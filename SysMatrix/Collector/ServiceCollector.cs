using System;
using System.Linq;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Threading.Tasks;
using SysMatrix.Models;

namespace SysMatrix.Collector
{
    public class ServiceCollector
    {
        private readonly ServiceMonitorConfig[] _servicesToMonitor = new[]
        {
            new ServiceMonitorConfig { ServiceName = "W3SVC", DisplayName = "World Wide Web Publishing Service", Port = 80 },
            new ServiceMonitorConfig { ServiceName = "W3SVC", DisplayName = "World Wide Web Publishing Service (HTTPS)", Port = 443 },
            new ServiceMonitorConfig { ServiceName = "MSSQLSERVER", DisplayName = "SQL Server (MSSQLSERVER)", Port = 1433 },
            new ServiceMonitorConfig { ServiceName = "SQLSERVERAGENT", DisplayName = "SQL Server Agent (MSSQLSERVER)", Port = null }
        };

        public async Task<ServiceMetrics> CollectAsync()
        {
            return await Task.Run(() =>
            {
                var metrics = new ServiceMetrics();

                try
                {
                    foreach (var config in _servicesToMonitor)
                    {
                        var serviceInfo = new ServiceInfo
                        {
                            ServiceName = config.ServiceName,
                            DisplayName = config.DisplayName,
                            MonitoredPort = config.Port
                        };

                        // Check service status
                        try
                        {
                            using (var service = new ServiceController(config.ServiceName))
                            {
                                serviceInfo.IsRunning = service.Status == ServiceControllerStatus.Running;
                                serviceInfo.Status = service.Status.ToString();
                            }
                        }
                        catch (InvalidOperationException)
                        {
                            // Service doesn't exist
                            serviceInfo.IsRunning = false;
                            serviceInfo.Status = "NotInstalled";
                        }
                        catch (Exception ex)
                        {
                            serviceInfo.Status = $"Error: {ex.Message}";
                        }

                        // Check port if specified
                        if (config.Port.HasValue)
                        {
                            serviceInfo.PortListening = CheckPortListening(config.Port.Value);

                            // Alert if service not running AND port not listening
                            if (!serviceInfo.IsRunning && serviceInfo.PortListening == false)
                            {
                                serviceInfo.AlertTriggered = true;
                                metrics.AlertTriggered = true;
                            }
                        }
                        else
                        {
                            // For services without ports, alert if just the service is not running
                            if (!serviceInfo.IsRunning && serviceInfo.Status != "NotInstalled")
                            {
                                serviceInfo.AlertTriggered = true;
                                metrics.AlertTriggered = true;
                            }
                        }

                        metrics.Services.Add(serviceInfo);
                    }

                    if (metrics.AlertTriggered)
                    {
                        var alertedServices = metrics.Services.Where(s => s.AlertTriggered)
                            .Select(s => $"{s.DisplayName} ({s.ServiceName})");

                        metrics.AlertMessage = $"Critical Service Availability Alert: The following services are not running " +
                                              $"and their corresponding ports are not responding: {string.Join(", ", alertedServices)}";
                    }
                }
                catch (Exception ex)
                {
                    metrics.AlertMessage = $"Error collecting service metrics: {ex.Message}";
                }

                return metrics;
            });
        }

        private bool CheckPortListening(int port)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var result = client.BeginConnect("localhost", port, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));

                    if (success)
                    {
                        client.EndConnect(result);
                        return true;
                    }
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private class ServiceMonitorConfig
        {
            public string ServiceName { get; set; }
            public string DisplayName { get; set; }
            public int? Port { get; set; }
        }
    }
}
