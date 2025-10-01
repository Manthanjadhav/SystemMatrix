using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SysMatrix.Models
{
    /// <summary>
    /// Critical Service Availability Metrics
    /// Alert if: Service (IIS, SQL Server) not running AND Corresponding port not listening/responding
    /// </summary>
    public class ServiceMetrics
    {
        public List<ServiceInfo> Services { get; set; }
        public bool AlertTriggered { get; set; }
        public string AlertMessage { get; set; }

        public ServiceMetrics()
        {
            Services = new List<ServiceInfo>();
        }
    }

    public class ServiceInfo
    {
        public string ServiceName { get; set; }
        public string DisplayName { get; set; }
        public bool IsRunning { get; set; }
        public string Status { get; set; }
        public int? MonitoredPort { get; set; }
        public bool? PortListening { get; set; }
        public bool AlertTriggered { get; set; }
    }
}
