using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SysMatrix.Models;

namespace SysMatrix.Collector
{
    public class DiskCollector
    {
        private const double FREE_SPACE_PERCENTAGE_THRESHOLD = 15.0;
        private const double FREE_SPACE_GB_THRESHOLD = 5.0;

        public async Task<DiskMetrics> CollectAsync()
        {
            return await Task.Run(() =>
            {
                var metrics = new DiskMetrics();

                try
                {
                    var drives = DriveInfo.GetDrives()
                        .Where(d => d.IsReady && d.DriveType == DriveType.Fixed);

                    foreach (var drive in drives)
                    {
                        try
                        {
                            var diskInfo = new DiskInfo
                            {
                                DriveName = drive.Name,
                                FreeMegabytes = Math.Round(drive.AvailableFreeSpace / (1024.0 * 1024.0), 2),
                                TotalSizeMegabytes = Math.Round(drive.TotalSize / (1024.0 * 1024.0), 2)
                            };

                            diskInfo.FreeSpacePercentage = Math.Round(
                                (diskInfo.FreeMegabytes / diskInfo.TotalSizeMegabytes) * 100, 2);

                            // Check alert conditions
                            double freeSpaceGB = diskInfo.FreeMegabytes / 1024.0;
                            if (diskInfo.FreeSpacePercentage < FREE_SPACE_PERCENTAGE_THRESHOLD &&
                                freeSpaceGB < FREE_SPACE_GB_THRESHOLD)
                            {
                                diskInfo.AlertTriggered = true;
                                metrics.AlertTriggered = true;
                            }

                            metrics.Disks.Add(diskInfo);
                        }
                        catch (Exception ex)
                        {
                            // Skip drives that can't be read
                            Console.WriteLine($"Error reading drive {drive.Name}: {ex.Message}");
                        }
                    }

                    if (metrics.AlertTriggered)
                    {
                        var alertedDisks = metrics.Disks.Where(d => d.AlertTriggered).Select(d => d.DriveName);
                        metrics.AlertMessage = $"Disk Space Alert: Low disk space on drives: {string.Join(", ", alertedDisks)}. " +
                                              $"Free space < {FREE_SPACE_PERCENTAGE_THRESHOLD}% AND < {FREE_SPACE_GB_THRESHOLD} GB";
                    }
                }
                catch (Exception ex)
                {
                    metrics.AlertMessage = $"Error collecting disk metrics: {ex.Message}";
                }

                return metrics;
            });
        }
    }
}
