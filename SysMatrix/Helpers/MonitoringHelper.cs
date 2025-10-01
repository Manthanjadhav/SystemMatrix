using System;
using System.Threading.Tasks;
using SysMatrix.Collector;
using SysMatrix.Models;

namespace SysMatrix.Helpers
{
    /// <summary>
    /// Helper class for monitoring data collection
    /// </summary>
    public static class MonitoringHelper
    {
        /// <summary>
        /// Helper method to collect all monitoring data from all collectors
        /// This is the main collection method called from Program.Main()
        /// </summary>
        public static async Task<MonitoringData> CollectAllMonitoringDataAsync()
        {
            var monitoringData = new MonitoringData
            {
                CollectionTimestamp = DateTime.Now
            };

            try
            {
                // Create all collectors
                var cpuCollector = new CpuCollector();
                var memoryCollector = new MemoryCollector();
                var diskCollector = new DiskCollector();
                var diskIoCollector = new DiskIoCollector();
                var networkCollector = new NetworkCollector();
                var webServerCollector = new WebServerCollector();
                var databaseCollector = new DatabaseCollector();
                var serviceCollector = new ServiceCollector();

                // Collect all data asynchronously
                var cpuTask = cpuCollector.CollectAsync();
                var memoryTask = memoryCollector.CollectAsync();
                var diskTask = diskCollector.CollectAsync();
                var diskIoTask = diskIoCollector.CollectAsync();
                var networkTask = networkCollector.CollectAsync();
                var webServerTask = webServerCollector.CollectAsync();
                var databaseTask = databaseCollector.CollectAsync();
                var serviceTask = serviceCollector.CollectAsync();

                // Wait for all tasks to complete
                await Task.WhenAll(cpuTask, memoryTask, diskTask, diskIoTask,
                                   networkTask, webServerTask, databaseTask, serviceTask);

                // Assign results
                monitoringData.CpuMetrics = cpuTask.Result;
                monitoringData.MemoryMetrics = memoryTask.Result;
                monitoringData.DiskMetrics = diskTask.Result;
                monitoringData.DiskIoMetrics = diskIoTask.Result;
                monitoringData.NetworkMetrics = networkTask.Result;
                monitoringData.WebServerMetrics = webServerTask.Result;
                monitoringData.DatabaseMetrics = databaseTask.Result;
                monitoringData.ServiceMetrics = serviceTask.Result;
            }
            catch (Exception ex)
            {
                monitoringData.ErrorMessage = $"Error collecting data: {ex.Message}";
            }

            return monitoringData;
        }
    }
}
