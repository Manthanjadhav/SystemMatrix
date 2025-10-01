using System;
using System.Threading.Tasks;
using System.Diagnostics; 
using SysMatrix.Models;

namespace SysMatrix.Collector
{
    public class MemoryCollector
    {


        public async Task<MemoryMetrics> CollectAsync()
        {
            return await Task.Run(() =>
            {
                var metrics = new MemoryMetrics();

                try
                {
                    // Available Memory in MBytes
                    using (var availableMemCounter = new PerformanceCounter("Memory", "Available MBytes"))
                    {
                        metrics.AvailableMBytes = Math.Round(availableMemCounter.NextValue(), 2);
                    }

                    // Pages per second
                    using (var pagesCounter = new PerformanceCounter("Memory", "Pages/sec"))
                    {
                        pagesCounter.NextValue(); // First call
                        System.Threading.Thread.Sleep(100);
                        metrics.PagesPerSec = Math.Round(pagesCounter.NextValue(), 2);
                    }

                    // Get total physical memory
                    var computerInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();
                    metrics.TotalMemoryMBytes = Math.Round(computerInfo.TotalPhysicalMemory / (1024.0 * 1024.0), 2);

                    // Calculate percentage
                    metrics.AvailableMemoryPercentage = Math.Round(
                        (metrics.AvailableMBytes / metrics.TotalMemoryMBytes) * 100, 2);

                    // Check alert conditions
                    if (metrics.AvailableMemoryPercentage < Constant.AVAILABLE_MEMORY_THRESHOLD &&
                        metrics.PagesPerSec > Constant.PAGES_PER_SEC_THRESHOLD)
                    {
                        metrics.AlertTriggered = true;
                        metrics.AlertMessage = $"Memory Pressure Alert: Available memory is {metrics.AvailableMemoryPercentage}% " +
                                              $"(threshold: {Constant.AVAILABLE_MEMORY_THRESHOLD}%) and Pages/sec is {metrics.PagesPerSec} " +
                                              $"(threshold: {Constant.PAGES_PER_SEC_THRESHOLD})";
                    }
                }
                catch (Exception ex)
                {
                    metrics.AlertMessage = $"Error collecting memory metrics: {ex.Message}";
                }

                return metrics;
            });
        }
    }
}
