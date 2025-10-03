using System;
using System.Net.Http;
using System.Threading.Tasks;
using SysMatrix.Models;

namespace SysMatrix.Collector
{
    public class Ec2MetadataCollector
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        private string _cachedToken;
        private DateTime _tokenExpiration;
        private readonly object _tokenLock = new object();

        public async Task<Ec2Metadata> CollectAsync()
        {
            var metadata = new Ec2Metadata
            {
                CollectionTime = DateTime.UtcNow,
                InstanceBasic = new InstanceBasicInfo(),
                Placement = new PlacementInfo(),
                NetworkPrimary = new NetworkPrimaryInfo(),
                System = new SystemInfo()
            };

            try
            {
                // Instance Basic
                metadata.InstanceBasic.InstanceId = await GetMetadataAsync("instance-id");
                metadata.InstanceBasic.InstanceType = await GetMetadataAsync("instance-type"); 

                // Placement
                metadata.Placement.AvailabilityZone = await GetMetadataAsync("placement/availability-zone");
                metadata.Placement.Region = await GetMetadataAsync("placement/region");

                // Network Primary
                metadata.NetworkPrimary.LocalIpv4 = await GetMetadataAsync("local-ipv4");
                metadata.NetworkPrimary.PublicHostname = await GetMetadataAsync("public-hostname");
                metadata.NetworkPrimary.PublicIpv4 = await GetMetadataAsync("public-ipv4");

                // System
                metadata.System.Name = await GetMetadataAsync("tags/instance/Name");

                metadata.IsSuccess = true;
            }
            catch (Exception ex)
            {
                metadata.IsSuccess = false;
                metadata.ErrorMessage = $"Error collecting EC2 metadata: {ex.Message}";
            }

            return metadata;
        }

        private async Task<string> GetOrRefreshTokenAsync()
        {
            lock (_tokenLock)
            {
                if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiration.AddMinutes(-1))
                {
                    return _cachedToken;
                }
            }

            try
            {
                var tokenRequest = new HttpRequestMessage(HttpMethod.Put, $"{Constant.TOKEN_URL}");
                tokenRequest.Headers.Add("X-aws-ec2-metadata-token-ttl-seconds", "21600");

                var tokenResponse = await _httpClient.SendAsync(tokenRequest);
                if (tokenResponse.IsSuccessStatusCode)
                {
                    var token = await tokenResponse.Content.ReadAsStringAsync();

                    lock (_tokenLock)
                    {
                        _cachedToken = token;
                        _tokenExpiration = DateTime.UtcNow.AddSeconds(21600);
                    }

                    return token;
                }
            }
            catch { }

            return null;
        }

        private async Task<string> GetMetadataAsync(string path)
        {
            try
            {
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
    }
}