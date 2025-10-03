using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.Runtime; 
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace SysMatrix.Helpers
{
    public class InstanceInfo
    {
        public string InstanceId { get; set; } = string.Empty;
        public string InstanceName { get; set; } = string.Empty;
        public string PrivateIP { get; set; } = string.Empty;
        public string PublicIP { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
    }

    public class InstanceInfoHelper : IDisposable
    {
        private readonly ConcurrentDictionary<string, List<InstanceInfo>> _instanceCache;
        private readonly ConcurrentDictionary<string, AmazonEC2Client> _ec2Clients;
        private readonly object _lockObject = new object();
        private bool _disposed = false;
        private readonly BasicAWSCredentials _basicAWSCredentials;

        public InstanceInfoHelper(string accessKey, string secretKey)
        {
            _instanceCache = new ConcurrentDictionary<string, List<InstanceInfo>>();
            _basicAWSCredentials = new BasicAWSCredentials(accessKey, secretKey);
            _ec2Clients = new ConcurrentDictionary<string, AmazonEC2Client>();
        }

        public Task<List<InstanceInfo>> EnumerateInstancesAsync(string region)
        {
            if (string.IsNullOrWhiteSpace(region))
            {
                //Helper.Tracer.Trace(TraceLevel.Error, $"Region cannot be null or empty {region}");
                return Task.FromResult(new List<InstanceInfo>());
            }

            // Check if region is already cached
            if (_instanceCache.TryGetValue(region, out var cachedInstances))
            {
                return Task.FromResult(cachedInstances);
            }

            // Use lock to prevent multiple simultaneous enumerations of the same region
            lock (_lockObject)
            {
                // Double-check after acquiring lock
                if (_instanceCache.TryGetValue(region, out cachedInstances))
                {
                    return Task.FromResult(cachedInstances);
                }

                // Enumerate instances for the region
                var instances = EnumerateInstancesInternal(region).Result;
                _instanceCache.TryAdd(region, instances);
                return Task.FromResult(instances);
            }
        }

        public List<InstanceInfo> EnumerateInstances(string region)
        {
            return EnumerateInstancesAsync(region).Result;
        }

        public async Task<InstanceInfo> FindByPrivateIPAsync(string privateIP, string region)
        {
            var instances = await EnumerateInstancesAsync(region);
            return instances.FirstOrDefault(i => i.PrivateIP == privateIP);
        }

        public InstanceInfo FindByPrivateIP(string privateIP)
        {
            if (string.IsNullOrWhiteSpace(privateIP))
                throw new ArgumentException("Private IP cannot be null or empty", nameof(privateIP));

            foreach (var regionInstances in _instanceCache.Values)
            {
                var instance = regionInstances.FirstOrDefault(i => i.PrivateIP == privateIP);
                if (instance != null)
                    return instance;
            }
            return null;
        }

        public InstanceInfo FindByPublicIP(string publicIP)
        {
            if (string.IsNullOrWhiteSpace(publicIP))
                throw new ArgumentException("Public IP cannot be null or empty", nameof(publicIP));

            foreach (var regionInstances in _instanceCache.Values)
            {
                var instance = regionInstances.FirstOrDefault(i => i.PublicIP == publicIP);
                if (instance != null)
                    return instance;
            }
            return null;
        }

        public InstanceInfo FindByIPAddress(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                throw new ArgumentException("IP Address cannot be null or empty", nameof(ipAddress));

            foreach (var regionInstances in _instanceCache.Values)
            {
                var instance = regionInstances.FirstOrDefault(i => (i.PublicIP == ipAddress) || (i.PrivateIP == ipAddress));
                if (instance != null)
                    return instance;
            }
            return null;
        }

        public List<InstanceInfo> FindBy(Func<InstanceInfo, bool> predicate, string region = null)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            var results = new List<InstanceInfo>();

            if (!string.IsNullOrWhiteSpace(region))
            {
                // Search in specific region
                if (_instanceCache.TryGetValue(region, out var regionInstances))
                {
                    results.AddRange(regionInstances.Where(predicate));
                }
            }
            else
            {
                // Search across all cached regions
                foreach (var regionInstances in _instanceCache.Values)
                {
                    results.AddRange(regionInstances.Where(predicate));
                }
            }

            return results;
        }

        public InstanceInfo FindFirstBy(Func<InstanceInfo, bool> predicate, string region = null)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            if (!string.IsNullOrWhiteSpace(region))
            {
                // Search in specific region
                if (_instanceCache.TryGetValue(region, out var regionInstances))
                {
                    return regionInstances.FirstOrDefault(predicate);
                }
            }
            else
            {
                // Search across all cached regions
                foreach (var regionInstances in _instanceCache.Values)
                {
                    var instance = regionInstances.FirstOrDefault(predicate);
                    if (instance != null)
                        return instance;
                }
            }

            return null;
        }

        public List<InstanceInfo> GetCachedInstances(string region)
        {
            return _instanceCache.TryGetValue(region, out var instances) ? instances : null;
        }

        public Dictionary<string, List<InstanceInfo>> GetAllCachedInstances()
        {
            return _instanceCache.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public bool ClearRegionCache(string region)
        {
            return _instanceCache.TryRemove(region, out _);
        }

        public void ClearAllCache()
        {
            _instanceCache.Clear();
        }

        public List<string> GetCachedRegions()
        {
            return _instanceCache.Keys.ToList();
        }

        private async Task<List<InstanceInfo>> EnumerateInstancesInternal(string region)
        {
            var instances = new List<InstanceInfo>();

            try
            {
                var ec2Client = GetOrCreateEC2Client(region);
                var request = new DescribeInstancesRequest();

                do
                {
                    var response = await ec2Client.DescribeInstancesAsync(request);

                    if (response != null && response.Reservations != null)
                    {
                        foreach (var reservation in response.Reservations)
                        {
                            foreach (var instance in reservation.Instances)
                            {
                                // Skip terminated instances
                                if (instance.State.Name == InstanceStateName.Terminated)
                                    continue;

                                var instanceInfo = new InstanceInfo
                                {
                                    InstanceId = instance.InstanceId ?? string.Empty,
                                    InstanceName = GetInstanceName(instance),
                                    PrivateIP = instance.PrivateIpAddress ?? string.Empty,
                                    PublicIP = instance.PublicIpAddress ?? string.Empty,
                                    Region = region
                                };
                                // Console.WriteLine($"Got EC2 Instance --> InstanceId: {instanceInfo.InstanceId}, InstanceName: {instanceInfo.InstanceName}, PrivateIP:{instanceInfo.PrivateIP}, PublicIP: {instanceInfo.PublicIP}, Region: {instanceInfo.Region}");
                                instances.Add(instanceInfo);
                            }
                        }

                        request.NextToken = response.NextToken;
                    }
                    else
                    {
                        break;
                    }
                }
                while (!string.IsNullOrEmpty(request.NextToken));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error enumerating instances in region {region}");
               // Helper.Tracer.Trace(TraceLevel.Error, $"Error enumerating instances in region {region}");
            }

            return instances;
        }

        private AmazonEC2Client GetOrCreateEC2Client(string region)
        {
            return _ec2Clients.GetOrAdd(region, r => new AmazonEC2Client(_basicAWSCredentials, Amazon.RegionEndpoint.GetBySystemName(r)));
        }

        private string GetInstanceName(Instance instance)
        {
            var nameTag = instance.Tags?.FirstOrDefault(t => t.Key.Equals("Name", StringComparison.OrdinalIgnoreCase));
            return nameTag?.Value ?? string.Empty;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // Dispose all EC2 clients
                foreach (var client in _ec2Clients.Values)
                {
                    client?.Dispose();
                }
                _ec2Clients.Clear();
                _disposed = true;
            }
        }

        #region EC2 Instance Metadata Methods

        public class InstanceMetadata
        {
            [Required]
            public  string InstanceId { get; set; }
            [Required]
            public  string Region { get; set; }
            [Required]
            public  string AvailabilityZone { get; set; }
            [Required]
            public  string InstanceType { get; set; }
        }

        public static async Task<InstanceMetadata> GetCurrentInstanceMetadata()
        {
            try
            {
                //await FileLogger.LogInfoAsync("🔍 Retrieving EC2 instance metadata");

                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(10);

                    // Get instance ID
                    string instanceId = await GetMetadataValueAsync(httpClient, "instance-id");
                    if (string.IsNullOrEmpty(instanceId))
                    {
                        throw new InvalidOperationException("Failed to retrieve instance ID from metadata service");
                    }

                    // Get availability zone
                    string availabilityZone = await GetMetadataValueAsync(httpClient, "placement/availability-zone");
                    if (string.IsNullOrEmpty(availabilityZone))
                    {
                        throw new InvalidOperationException("Failed to retrieve availability zone from metadata service");
                    }

                    // Get instance type (optional, for logging)
                    string instanceType = await GetMetadataValueAsync(httpClient, "instance-type");

                    // Extract region from availability zone (remove last character)
                    string region = availabilityZone.Substring(0, availabilityZone.Length - 1);

                    //await FileLogger.LogInfoAsync($"📋 Instance Metadata - ID: {instanceId}, AZ: {availabilityZone}, Type: {instanceType}");

                    return new InstanceMetadata
                    {
                        InstanceId = instanceId,
                        Region = region,
                        AvailabilityZone = availabilityZone,
                        InstanceType = instanceType
                    };
                }
            }
            catch (Exception ex)
            {
                //await FileLogger.LogInfoAsync($"❌ Failed to retrieve instance metadata: {ex.Message}");
                throw new InvalidOperationException("Could not retrieve EC2 instance metadata. Ensure this application is running on an EC2 instance.", ex);
            }
        }

        private static async Task<string> GetMetadataValueAsync(HttpClient httpClient, string path)
        {
            try
            {
                // EC2 Instance Metadata Service v2 (IMDSv2) - more secure
                // First, get token
                var tokenRequest = new HttpRequestMessage(HttpMethod.Put, "http://169.254.169.254/latest/api/token");
                tokenRequest.Headers.Add("X-aws-ec2-metadata-token-ttl-seconds", "21600"); // 6 hours

                var tokenResponse = await httpClient.SendAsync(tokenRequest);
                if (!tokenResponse.IsSuccessStatusCode)
                {
                    // Fallback to IMDSv1 if v2 is not available
                    //await FileLogger.LogInfoAsync("⚠️  IMDSv2 not available, falling back to IMDSv1");
                    return await GetMetadataValueV1Async(httpClient, path);
                }

                string token = await tokenResponse.Content.ReadAsStringAsync();

                // Use token to get metadata
                var metadataRequest = new HttpRequestMessage(HttpMethod.Get, $"http://169.254.169.254/latest/meta-data/{path}");
                metadataRequest.Headers.Add("X-aws-ec2-metadata-token", token);

                var metadataResponse = await httpClient.SendAsync(metadataRequest);
                if (metadataResponse.IsSuccessStatusCode)
                {
                    return (await metadataResponse.Content.ReadAsStringAsync()).Trim();
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                //await FileLogger.LogInfoAsync($"⚠️  Error getting metadata for {path}: {ex.Message}");
                return string.Empty;
            }
        }

        private static async Task<string> GetMetadataValueV1Async(HttpClient httpClient, string path)
        {
            try
            {
                var response = await httpClient.GetAsync($"http://169.254.169.254/latest/meta-data/{path}");
                if (response.IsSuccessStatusCode)
                {
                    return (await response.Content.ReadAsStringAsync()).Trim();
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                //await FileLogger.LogInfoAsync($"⚠️  Error getting metadata (v1) for {path}: {ex.Message}");
                return string.Empty;
            }
        }

        #endregion
    }
}