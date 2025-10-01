using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; 
using System.Diagnostics;
using System.Threading.Tasks;
using SysMatrix.Models;
namespace SysMatrix.Collector
{
    public class CpuCollector
    {
        private const double CPU_THRESHOLD = 90.0;
        private const double QUEUE_LENGTH_MULTIPLIER = 2.0;

        public async Task<CpuMetrics> CollectAsync()
        {
            return await Task.Run(() =>
            {
                var metrics = new CpuMetrics
                {
                    NumberOfCores = Environment.ProcessorCount
                };

                try
                {
                    // Processor Time
                    using (var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"))
                    {
                        cpuCounter.NextValue(); // First call returns 0
                        System.Threading.Thread.Sleep(100); // Small delay for accurate reading
                        metrics.ProcessorTimePercentage = Math.Round(cpuCounter.NextValue(), 2);
                    }

                    // Processor Queue Length
                    using (var queueCounter = new PerformanceCounter("System", "Processor Queue Length"))
                    {
                        metrics.ProcessorQueueLength = Math.Round(queueCounter.NextValue(), 2);
                    }

                    // Calculate queue length per core
                    metrics.QueueLengthPerCore = Math.Round(metrics.ProcessorQueueLength / metrics.NumberOfCores, 2);

                    // Check alert conditions
                    if (metrics.ProcessorTimePercentage > CPU_THRESHOLD &&
                        metrics.QueueLengthPerCore > QUEUE_LENGTH_MULTIPLIER)
                    {
                        metrics.AlertTriggered = true;
                        metrics.AlertMessage = $"CPU Pressure Alert: CPU usage is {metrics.ProcessorTimePercentage}% " +
                                              $"(threshold: {CPU_THRESHOLD}%) and Queue Length per Core is " +
                                              $"{metrics.QueueLengthPerCore} (threshold: {QUEUE_LENGTH_MULTIPLIER})";
                    }
                }
                catch (Exception ex)
                {
                    metrics.AlertMessage = $"Error collecting CPU metrics: {ex.Message}";
                }

                return metrics;
            });
        }
    }
}
