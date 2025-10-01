using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SysMatrix.Models;

namespace SysMatrix.Collector
{
    public class DiskIoCollector
    {
        private const double DISK_SEC_THRESHOLD_MS = 25.0; // 25 milliseconds
        private const double QUEUE_LENGTH_MULTIPLIER = 2.0;

        public async Task<DiskIoMetrics> CollectAsync()
        {
            return await Task.Run(() =>
            {
                var metrics = new DiskIoMetrics();
                int numberOfCores = Environment.ProcessorCount;

                try
                {
                    var category = new PerformanceCounterCategory("PhysicalDisk");
                    var instanceNames = category.GetInstanceNames()
                        .Where(name => !name.Equals("_Total", StringComparison.OrdinalIgnoreCase));

                    foreach (var instanceName in instanceNames)
                    {
                        try
                        {
                            var diskIoInfo = new DiskIoInfo
                            {
                                DiskName = instanceName
                            };

                            // Avg. Disk sec/Read (in seconds, convert to ms)
                            using (var readCounter = new PerformanceCounter("PhysicalDisk", "Avg. Disk sec/Read", instanceName))
                            {
                                readCounter.NextValue();
                                System.Threading.Thread.Sleep(100);
                                diskIoInfo.AvgDiskSecRead = Math.Round(readCounter.NextValue() * 1000, 2); // Convert to ms
                            }

                            // Avg. Disk sec/Write (in seconds, convert to ms)
                            using (var writeCounter = new PerformanceCounter("PhysicalDisk", "Avg. Disk sec/Write", instanceName))
                            {
                                writeCounter.NextValue();
                                System.Threading.Thread.Sleep(100);
                                diskIoInfo.AvgDiskSecWrite = Math.Round(writeCounter.NextValue() * 1000, 2); // Convert to ms
                            }

                            // Avg. Disk Queue Length
                            using (var queueCounter = new PerformanceCounter("PhysicalDisk", "Avg. Disk Queue Length", instanceName))
                            {
                                diskIoInfo.AvgDiskQueueLength = Math.Round(queueCounter.NextValue(), 2);
                            }

                            // Check alert conditions
                            double queueThreshold = QUEUE_LENGTH_MULTIPLIER * numberOfCores;
                            if ((diskIoInfo.AvgDiskSecRead > DISK_SEC_THRESHOLD_MS ||
                                 diskIoInfo.AvgDiskSecWrite > DISK_SEC_THRESHOLD_MS) &&
                                diskIoInfo.AvgDiskQueueLength > queueThreshold)
                            {
                                diskIoInfo.AlertTriggered = true;
                                metrics.AlertTriggered = true;
                            }

                            metrics.Disks.Add(diskIoInfo);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error reading disk {instanceName}: {ex.Message}");
                        }
                    }

                    if (metrics.AlertTriggered)
                    {
                        var alertedDisks = metrics.Disks.Where(d => d.AlertTriggered).Select(d => d.DiskName);
                        metrics.AlertMessage = $"Disk I/O Bottleneck Alert: High latency detected on disks: {string.Join(", ", alertedDisks)}. " +
                                              $"Avg Disk sec/Read or sec/Write > {DISK_SEC_THRESHOLD_MS} ms AND " +
                                              $"Avg Disk Queue Length > {QUEUE_LENGTH_MULTIPLIER} × cores";
                    }
                }
                catch (Exception ex)
                {
                    metrics.AlertMessage = $"Error collecting disk I/O metrics: {ex.Message}";
                }

                return metrics;
            });
        }
    }
}
