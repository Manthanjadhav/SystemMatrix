using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using SysMatrix.Models;

namespace SysMatrix.Collector
{
    public class Ec2MetadataCollector
    { 
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) }; 

        // Cache token
        private string _cachedToken;
        private DateTime _tokenExpiration;
        private readonly object _tokenLock = new object();

        public async Task<Ec2Metadata> CollectAsync()
        {
            var metadata = new Ec2Metadata
            {
                CollectionTime = DateTime.UtcNow,
                InstanceBasic = new InstanceBasicInfo(),
                Ami = new AmiInfo(),
                Placement = new PlacementInfo(),
                NetworkPrimary = new NetworkPrimaryInfo(),
                BlockDevice = new BlockDeviceInfo(),
                Security = new SecurityInfo(),
                System = new SystemInfo(),
                SpotInstance = new SpotInstanceInfo(),
                Events = new EventsInfo(),
                DynamicData = new DynamicDataInfo(),
                NetworkInterfaces = new List<NetworkInterfaceDetail>()
            };

            try
            {
                await CollectInstanceBasicAsync(metadata.InstanceBasic);
                await CollectAmiInfoAsync(metadata.Ami);
                await CollectZoneInfoAsync(metadata.Placement);
                await CollectNetworkPrimaryAsync(metadata.NetworkPrimary);
                await CollectBlockDeviceAsync(metadata.BlockDevice);
                await CollectSecurityInfoAsync(metadata.Security);
                await CollectSystemInfoAsync(metadata.System);
                await CollectSpotInstanceAsync(metadata.SpotInstance);
                await CollectEventsAsync(metadata.Events);
                await CollectNetworkInterfacesAsync(metadata.NetworkInterfaces);
                await CollectDynamicDataAsync(metadata.DynamicData);
                metadata.UserData = await GetUserDataAsync();

                metadata.IsSuccess = true;
            }
            catch (Exception ex)
            {
                metadata.IsSuccess = false;
                metadata.ErrorMessage = $"Error collecting EC2 metadata: {ex.Message}";
            }

            return metadata;
        }

        // ... (all your Collect methods stay the same)
        private async Task<string> GetOrRefreshTokenAsync()
        {
            lock (_tokenLock)
            {
                // Return cached token if still valid
                if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiration.AddMinutes(-1))
                {
                    return _cachedToken;
                }
            }

            // Get new token
            try
            {
                var tokenRequest = new HttpRequestMessage(HttpMethod.Put, "http://169.254.169.254/latest/api/token");
                tokenRequest.Headers.Add("X-aws-ec2-metadata-token-ttl-seconds", "21600");

                var tokenResponse = await _httpClient.SendAsync(tokenRequest);
                if (tokenResponse.IsSuccessStatusCode)
                {
                    var token = await tokenResponse.Content.ReadAsStringAsync();

                    lock (_tokenLock)
                    {
                        _cachedToken = token;
                        _tokenExpiration = DateTime.UtcNow.AddSeconds(21600); // 6 hours
                    }

                    return token;
                }
            }
            catch { }

            return null;
        }

        private async Task CollectInstanceBasicAsync(InstanceBasicInfo info)
        {
            info.InstanceId = await GetMetadataAsync("instance-id");
            info.InstanceType = await GetMetadataAsync("instance-type");
            info.InstanceAction = await GetMetadataAsync("instance-action");
            info.InstanceLifeCycle = await GetMetadataAsync("instance-life-cycle");
        }

        private async Task CollectAmiInfoAsync(AmiInfo info)
        {
            info.AmiId = await GetMetadataAsync("ami-id");
            info.AmiLaunchIndex = await GetMetadataAsync("ami-launch-index");
            info.AmiManifestPath = await GetMetadataAsync("ami-manifest-path");
        }

        private async Task CollectZoneInfoAsync(PlacementInfo info)
        {
            info.AvailabilityZone = await GetMetadataAsync("placement/availability-zone");
            info.AvailabilityZoneId = await GetMetadataAsync("placement/availability-zone-id");
            info.Region = await GetMetadataAsync("placement/region");
            info.PartitionNumber = await GetMetadataAsync("placement/partition-number");
        }

        private async Task CollectNetworkPrimaryAsync(NetworkPrimaryInfo info)
        {
            info.Hostname = await GetMetadataAsync("hostname");
            info.LocalHostname = await GetMetadataAsync("local-hostname");
            info.LocalIpv4 = await GetMetadataAsync("local-ipv4");
            info.PublicHostname = await GetMetadataAsync("public-hostname");
            info.PublicIpv4 = await GetMetadataAsync("public-ipv4");
            info.MacAddress = await GetMetadataAsync("mac");
        }

        private async Task CollectBlockDeviceAsync(BlockDeviceInfo info)
        {
            info.MappingAmi = await GetMetadataAsync("block-device-mapping/ami");
            info.MappingRoot = await GetMetadataAsync("block-device-mapping/root");
            info.MappingEbs = await GetMetadataAsync("block-device-mapping/ebs0");
            info.MappingEphemeral = await GetMetadataAsync("block-device-mapping/ephemeral0");
        }

        private async Task CollectSecurityInfoAsync(SecurityInfo info)
        {
            info.SecurityGroups = await GetMetadataAsync("security-groups");
            info.IamInfo = await GetMetadataAsync("iam/info");
            info.IamSecurityCredentials = await GetMetadataAsync("iam/security-credentials/");
        }

        private async Task CollectSystemInfoAsync(SystemInfo info)
        {
            info.ProductCodes = await GetMetadataAsync("product-codes");
            info.KernelId = await GetMetadataAsync("kernel-id");
            info.RamdiskId = await GetMetadataAsync("ramdisk-id");
            info.ReservationId = await GetMetadataAsync("reservation-id");
            info.MetricsVhostmd = await GetMetadataAsync("metrics/vhostmd");
            info.ServicesDomain = await GetMetadataAsync("services/domain");
            info.ServicesPartition = await GetMetadataAsync("services/partition");
            info.SystemData = await GetMetadataAsync("system");
            info.Tags = await GetMetadataAsync("tags/instance");
        }

        private async Task CollectSpotInstanceAsync(SpotInstanceInfo info)
        {
            info.TerminationTime = await GetMetadataAsync("spot/termination-time");
            info.InstanceAction = await GetMetadataAsync("spot/instance-action");
        }

        private async Task CollectEventsAsync(EventsInfo info)
        {
            info.MaintenanceHistory = await GetMetadataAsync("events/maintenance/history");
            info.MaintenanceScheduled = await GetMetadataAsync("events/maintenance/scheduled");
            info.RecommendationsRebalance = await GetMetadataAsync("events/recommendations/rebalance");
        }

        private async Task CollectNetworkInterfacesAsync(List<NetworkInterfaceDetail> interfaces)
        {
            var macs = await GetMetadataAsync("network/interfaces/macs/");
            if (string.IsNullOrEmpty(macs) || macs == "N/A")
                return;

            var macList = macs.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var mac in macList)
            {
                var macClean = mac.TrimEnd('/');
                var netInterface = new NetworkInterfaceDetail
                {
                    MacAddress = macClean,
                    DeviceNumber = await GetMetadataAsync($"network/interfaces/macs/{mac}device-number"),
                    InterfaceId = await GetMetadataAsync($"network/interfaces/macs/{mac}interface-id"),
                    LocalHostname = await GetMetadataAsync($"network/interfaces/macs/{mac}local-hostname"),
                    LocalIpv4s = await GetMetadataAsync($"network/interfaces/macs/{mac}local-ipv4s"),
                    PublicHostname = await GetMetadataAsync($"network/interfaces/macs/{mac}public-hostname"),
                    PublicIpv4s = await GetMetadataAsync($"network/interfaces/macs/{mac}public-ipv4s"),
                    Ipv6s = await GetMetadataAsync($"network/interfaces/macs/{mac}ipv6s"),
                    SecurityGroupIds = await GetMetadataAsync($"network/interfaces/macs/{mac}security-group-ids"),
                    SecurityGroups = await GetMetadataAsync($"network/interfaces/macs/{mac}security-groups"),
                    SubnetId = await GetMetadataAsync($"network/interfaces/macs/{mac}subnet-id"),
                    SubnetIpv4CidrBlock = await GetMetadataAsync($"network/interfaces/macs/{mac}subnet-ipv4-cidr-block"),
                    SubnetIpv6CidrBlocks = await GetMetadataAsync($"network/interfaces/macs/{mac}subnet-ipv6-cidr-blocks"),
                    VpcId = await GetMetadataAsync($"network/interfaces/macs/{mac}vpc-id"),
                    VpcIpv4CidrBlock = await GetMetadataAsync($"network/interfaces/macs/{mac}vpc-ipv4-cidr-block"),
                    VpcIpv4CidrBlocks = await GetMetadataAsync($"network/interfaces/macs/{mac}vpc-ipv4-cidr-blocks"),
                    VpcIpv6CidrBlocks = await GetMetadataAsync($"network/interfaces/macs/{mac}vpc-ipv6-cidr-blocks"),
                    OwnerId = await GetMetadataAsync($"network/interfaces/macs/{mac}owner-id")
                };
                interfaces.Add(netInterface);
            }
        }

        private async Task CollectDynamicDataAsync(DynamicDataInfo info)
        {
            info.InstanceIdentityDocument = await GetDynamicAsync("instance-identity/document");
            info.InstanceIdentitySignature = await GetDynamicAsync("instance-identity/signature");
            info.InstanceIdentityPkcs7 = await GetDynamicAsync("instance-identity/pkcs7");
        }

        private async Task<string> GetMetadataAsync(string path)
        {
            try
            {
                // Get cached or fresh token (only 1 request for all metadata)
                var token = await GetOrRefreshTokenAsync();

                if (!string.IsNullOrEmpty(token))
                {
                    var metadataRequest = new HttpRequestMessage(HttpMethod.Get, $"{Constant.METADATA_URL}{path}");
                    metadataRequest.Headers.Add("X-aws-ec2-metadata-token", token);

                    var metadataResponse = await _httpClient.SendAsync(metadataRequest);
                    if (metadataResponse.IsSuccessStatusCode)
                    {
                        return (await metadataResponse.Content.ReadAsStringAsync()).Trim();
                    }
                }

                // Fallback to IMDSv1
                var response = await _httpClient.GetAsync($"{Constant.METADATA_URL}{path}");
                if (response.IsSuccessStatusCode)
                {
                    return (await response.Content.ReadAsStringAsync()).Trim();
                }

                return "N/A";
            }
            catch
            {
                return "N/A";
            }
        }

        private async Task<string> GetDynamicAsync(string path)
        {
            try
            {
                var token = await GetOrRefreshTokenAsync();

                if (!string.IsNullOrEmpty(token))
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, $"{Constant.DYNAMIC_URL}{path}");
                    request.Headers.Add("X-aws-ec2-metadata-token", token);

                    var response = await _httpClient.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        return (await response.Content.ReadAsStringAsync()).Trim();
                    }
                }

                var fallbackResponse = await _httpClient.GetAsync($"{Constant.DYNAMIC_URL}{path}");
                if (fallbackResponse.IsSuccessStatusCode)
                {
                    return (await fallbackResponse.Content.ReadAsStringAsync()).Trim();
                }

                return "N/A";
            }
            catch
            {
                return "N/A";
            }
        }

        private async Task<string> GetUserDataAsync()
        {
            try
            {
                var token = await GetOrRefreshTokenAsync();

                if (!string.IsNullOrEmpty(token))
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, "http://169.254.169.254/latest/user-data");
                    request.Headers.Add("X-aws-ec2-metadata-token", token);

                    var response = await _httpClient.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadAsStringAsync();
                    }
                }

                var fallbackResponse = await _httpClient.GetAsync("http://169.254.169.254/latest/user-data");
                if (fallbackResponse.IsSuccessStatusCode)
                {
                    return await fallbackResponse.Content.ReadAsStringAsync();
                }

                return "N/A";
            }
            catch
            {
                return "N/A";
            }
        }
    }
}