using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Threading.Tasks;
using SysMatrix.Models;


namespace SysMatrix.Collector
{
    public class WebServerCollector
    {
        private const double ERROR_5XX_PERCENTAGE_THRESHOLD = 2.0; // 2%
        private const double RESPONSE_TIME_THRESHOLD_MS = 2000.0; // 2 seconds
        private const int HEALTH_PROBE_FAILURE_THRESHOLD = 3;

        public async Task<WebServerMetrics> CollectAsync()
        {
            return await Task.Run(() =>
            {
                var metrics = new WebServerMetrics();

                try
                {
                    // Check W3SVC service status
                    metrics.W3SvcServiceRunning = CheckServiceStatus("W3SVC");

                    // Check port availability
                    metrics.Port80Listening = CheckPortListening(80);
                    metrics.Port443Listening = CheckPortListening(443);

                    // Health probe check (simplified - checking localhost)
                    var healthProbeResult = CheckHealthProbe("http://localhost/health");
                    metrics.HealthProbeSuccessful = healthProbeResult.Item1;
                    metrics.HealthProbeResponseTimeMs = healthProbeResult.Item2;
                    metrics.HealthProbeFailureCount = healthProbeResult.Item1 ? 0 : 1;

                    // Collect IIS performance counters (if available)
                    if (metrics.W3SvcServiceRunning)
                    {
                        CollectIISPerformanceMetrics(metrics);
                    }

                    // Check availability alert
                    if (!metrics.W3SvcServiceRunning && !metrics.Port80Listening && !metrics.Port443Listening)
                    {
                        metrics.AvailabilityAlertTriggered = true;
                        metrics.AlertMessage = "Web Server Availability Alert: W3SVC service not running and ports 80/443 not listening. ";
                    }
                    else if (!metrics.HealthProbeSuccessful && metrics.HealthProbeFailureCount >= HEALTH_PROBE_FAILURE_THRESHOLD)
                    {
                        metrics.AvailabilityAlertTriggered = true;
                        metrics.AlertMessage += "Health probe failed 3 consecutive times. ";
                    }

                    // Check performance alert
                    if (metrics.Error5xxPercentage > ERROR_5XX_PERCENTAGE_THRESHOLD &&
                        metrics.ResponseTime95thPercentileMs > RESPONSE_TIME_THRESHOLD_MS)
                    {
                        metrics.PerformanceAlertTriggered = true;
                        metrics.AlertMessage += $"Web Server Performance Alert: 5xx errors at {metrics.Error5xxPercentage}% " +
                                               $"(threshold: {ERROR_5XX_PERCENTAGE_THRESHOLD}%) and response time at " +
                                               $"{metrics.ResponseTime95thPercentileMs}ms (threshold: {RESPONSE_TIME_THRESHOLD_MS}ms)";
                    }
                }
                catch (Exception ex)
                {
                    metrics.AlertMessage = $"Error collecting web server metrics: {ex.Message}";
                }

                return metrics;
            });
        }

        private bool CheckServiceStatus(string serviceName)
        {
            try
            {
                using (var service = new ServiceController(serviceName))
                {
                    return service.Status == ServiceControllerStatus.Running;
                }
            }
            catch
            {
                return false;
            }
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

        private Tuple<bool, double> CheckHealthProbe(string url)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var request = WebRequest.Create(url);
                request.Timeout = 5000; // 5 second timeout

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    stopwatch.Stop();
                    bool success = response.StatusCode == HttpStatusCode.OK;
                    return new Tuple<bool, double>(success, Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2));
                }
            }
            catch
            {
                return new Tuple<bool, double>(false, 0);
            }
        }

        private void CollectIISPerformanceMetrics(WebServerMetrics metrics)
        {
            try
            {
                // Total Method Requests/sec
                using (var requestsCounter = new PerformanceCounter("Web Service", "Total Method Requests/sec", "_Total"))
                {
                    requestsCounter.NextValue();
                    System.Threading.Thread.Sleep(100);
                    metrics.TotalMethodRequestsPerSec = Math.Round(requestsCounter.NextValue(), 2);
                }

                // Current Connections
                using (var connectionsCounter = new PerformanceCounter("Web Service", "Current Connections", "_Total"))
                {
                    metrics.CurrentConnections = Math.Round(connectionsCounter.NextValue(), 2);
                }

                // Connection Attempts/sec
                using (var attemptsCounter = new PerformanceCounter("Web Service", "Connection Attempts/sec", "_Total"))
                {
                    attemptsCounter.NextValue();
                    System.Threading.Thread.Sleep(100);
                    metrics.ConnectionAttemptsPerSec = Math.Round(attemptsCounter.NextValue(), 2);
                }

                // Calculate error percentage (simplified - actual implementation would need IIS logs)
                metrics.TotalRequests = 1000; // Placeholder
                metrics.Error5xxCount = 10; // Placeholder - would come from IIS logs
                if (metrics.TotalRequests > 0)
                {
                    metrics.Error5xxPercentage = Math.Round((metrics.Error5xxCount / (double)metrics.TotalRequests) * 100, 2);
                }

                // Response time (would typically come from synthetic monitoring)
                metrics.ResponseTime95thPercentileMs = metrics.HealthProbeResponseTimeMs;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error collecting IIS performance metrics: {ex.Message}");
            }
        }
    }
}
