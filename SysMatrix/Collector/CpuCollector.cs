using System; 
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics; 
using SysMatrix.Models;
using System.Linq;

namespace SysMatrix.Collector
{
    public class CpuCollector
    {
        private const double CPU_THRESHOLD = 90.0;
        private const double QUEUE_LENGTH_MULTIPLIER = 2.0;
        private readonly Queue<double> _cpuReadings = new Queue<double>();
        private readonly object _lockObject = new object();
        private const int SAMPLE_INTERVAL_MS = 10000;
        private const int MAX_SAMPLES = 30;
        /// <summary>
        /// Make _isInitialized false for mesuraing the cpu matrix for 5Min
        /// </summary>
        private bool _isInitialized = false;

        public async Task<CpuMetrics> CollectAsync()
        {
            var metrics = new CpuMetrics
            {
                NumberOfCores = Environment.ProcessorCount
            };

            try
            {
                // If not initialized, collect 30 samples over 5 minutes
                if (!_isInitialized)
                {
                    Console.WriteLine("First run: Collecting 5 minutes of CPU baseline data...");

                    for (int i = 0; i < MAX_SAMPLES; i++)
                    {
                        double cpuReading;
                        using (var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"))
                        {
                            cpuCounter.NextValue();
                            await Task.Delay(100); // Use async delay instead of Thread.Sleep
                            cpuReading = cpuCounter.NextValue();
                        }

                        lock (_lockObject)
                        {
                            _cpuReadings.Enqueue(cpuReading);
                        }

                        Console.WriteLine($"Sample {i + 1}/{MAX_SAMPLES} collected... : " + cpuReading);

                        if (i < MAX_SAMPLES - 1)
                        {
                            await Task.Delay(SAMPLE_INTERVAL_MS); // Wait 10 seconds
                        }
                    }

                    _isInitialized = true;
                    Console.WriteLine("Baseline collection complete.");
                }
                else
                {
                    // Subsequent calls: just collect one sample
                    double currentCpuUsage;
                    using (var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"))
                    {
                        cpuCounter.NextValue();
                        await Task.Delay(100);
                        currentCpuUsage = cpuCounter.NextValue();
                    }

                    lock (_lockObject)
                    {
                        _cpuReadings.Enqueue(currentCpuUsage);

                        while (_cpuReadings.Count > MAX_SAMPLES)
                        {
                            _cpuReadings.Dequeue();
                        }
                    }
                }

                // Calculate average
                lock (_lockObject)
                {
                    metrics.ProcessorTimePercentage = Math.Round(_cpuReadings.Average(), 2);
                }

                // Processor Queue Length
                using (var queueCounter = new PerformanceCounter("System", "Processor Queue Length"))
                {
                    metrics.ProcessorQueueLength = Math.Round(queueCounter.NextValue(), 2);
                }

                metrics.QueueLengthPerCore = Math.Round(metrics.ProcessorQueueLength / metrics.NumberOfCores, 2);

                // Check alert conditions
                if (metrics.ProcessorTimePercentage > CPU_THRESHOLD &&
                    metrics.QueueLengthPerCore > QUEUE_LENGTH_MULTIPLIER)
                {
                    metrics.AlertTriggered = true;
                    metrics.AlertMessage = $"CPU Pressure Alert: CPU usage (5-min avg) is {metrics.ProcessorTimePercentage}% " +
                                          $"(threshold: {CPU_THRESHOLD}%) and Queue Length per Core is " +
                                          $"{metrics.QueueLengthPerCore} (threshold: {QUEUE_LENGTH_MULTIPLIER})";
                }
            }
            catch (Exception ex)
            {
                metrics.AlertMessage = $"Error collecting CPU metrics: {ex.Message}";
            }

            return metrics;
        }
    }
}
