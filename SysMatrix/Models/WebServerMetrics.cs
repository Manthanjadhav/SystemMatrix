using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SysMatrix.Models
{
    /// <summary>
    /// Web Server Availability and Performance Metrics
    /// Availability Alert if: IIS Service (W3SVC) not running AND TCP port 80/443 not listening/responding OR Health probe (/health URL) fails 3 consecutive checks
    /// Performance Alert if: HTTP 5xx errors > 2% of requests AND Response time (95th percentile) > 2s for 3 samples
    /// </summary>
    public class WebServerMetrics
    {
        // Availability Metrics
        public bool W3SvcServiceRunning { get; set; }
        public bool Port80Listening { get; set; }
        public bool Port443Listening { get; set; }
        public bool HealthProbeSuccessful { get; set; }
        public int HealthProbeFailureCount { get; set; }
        public double HealthProbeResponseTimeMs { get; set; }

        // Performance Metrics
        public double TotalMethodRequestsPerSec { get; set; }
        public double CurrentConnections { get; set; }
        public double ConnectionAttemptsPerSec { get; set; }
        public int Error5xxCount { get; set; }
        public int TotalRequests { get; set; }
        public double Error5xxPercentage { get; set; }
        public double ResponseTime95thPercentileMs { get; set; }

        // Alerts
        public bool AvailabilityAlertTriggered { get; set; }
        public bool PerformanceAlertTriggered { get; set; }
        public string AlertMessage { get; set; }
    }
}
