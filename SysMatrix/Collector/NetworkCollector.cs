using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SysMatrix.Models;

namespace SysMatrix.Collector
{
    public class NetworkCollector
    {
        private const double ERROR_PERCENTAGE_THRESHOLD = 1.0; // 1%

        public async Task<NetworkMetrics> CollectAsync()
        {
            return await Task.Run(() =>
            {
                var metrics = new NetworkMetrics();

                try
                {
                    var category = new PerformanceCounterCategory("Network Interface");
                    var instanceNames = category.GetInstanceNames();

                    foreach (var instanceName in instanceNames)
                    {
                        try
                        {
                            var interfaceInfo = new NetworkInterfaceInfo
                            {
                                InterfaceName = instanceName
                            };

                            // Bytes Total/sec
                            using (var bytesCounter = new PerformanceCounter("Network Interface", "Bytes Total/sec", instanceName))
                            {
                                bytesCounter.NextValue();
                                System.Threading.Thread.Sleep(100);
                                interfaceInfo.BytesTotalPerSec = Math.Round(bytesCounter.NextValue(), 2);
                            }

                            // Packets Outbound Errors
                            using (var outboundErrorCounter = new PerformanceCounter("Network Interface", "Packets Outbound Errors", instanceName))
                            {
                                interfaceInfo.PacketsOutboundErrors = Math.Round(outboundErrorCounter.NextValue(), 2);
                            }

                            // Packets Received Errors
                            using (var receivedErrorCounter = new PerformanceCounter("Network Interface", "Packets Received Errors", instanceName))
                            {
                                interfaceInfo.PacketsReceivedErrors = Math.Round(receivedErrorCounter.NextValue(), 2);
                            }

                            // Calculate total packets (approximate from bytes)
                            // Packets/sec counters
                            using (var packetsSentCounter = new PerformanceCounter("Network Interface", "Packets Sent/sec", instanceName))
                            using (var packetsReceivedCounter = new PerformanceCounter("Network Interface", "Packets Received/sec", instanceName))
                            {
                                packetsSentCounter.NextValue();
                                packetsReceivedCounter.NextValue();
                                System.Threading.Thread.Sleep(100);
                                double packetsSent = packetsSentCounter.NextValue();
                                double packetsReceived = packetsReceivedCounter.NextValue();
                                interfaceInfo.TotalPackets = Math.Round(packetsSent + packetsReceived, 2);
                            }

                            // Calculate error percentage
                            if (interfaceInfo.TotalPackets > 0)
                            {
                                double totalErrors = interfaceInfo.PacketsOutboundErrors + interfaceInfo.PacketsReceivedErrors;
                                interfaceInfo.ErrorPercentage = Math.Round((totalErrors / interfaceInfo.TotalPackets) * 100, 4);

                                // Check alert conditions
                                if (interfaceInfo.ErrorPercentage > ERROR_PERCENTAGE_THRESHOLD)
                                {
                                    interfaceInfo.AlertTriggered = true;
                                    metrics.AlertTriggered = true;
                                }
                            }

                            metrics.Interfaces.Add(interfaceInfo);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error reading network interface {instanceName}: {ex.Message}");
                        }
                    }

                    if (metrics.AlertTriggered)
                    {
                        var alertedInterfaces = metrics.Interfaces.Where(i => i.AlertTriggered).Select(i => i.InterfaceName);
                        metrics.AlertMessage = $"Network Issues Alert: High packet error rate on interfaces: {string.Join(", ", alertedInterfaces)}. " +
                                              $"Packet errors > {ERROR_PERCENTAGE_THRESHOLD}% of total packets";
                    }
                }
                catch (Exception ex)
                {
                    metrics.AlertMessage = $"Error collecting network metrics: {ex.Message}";
                }

                return metrics;
            });
        }
    }
}
