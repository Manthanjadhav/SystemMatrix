using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SysMatrix.Models
{
    /// <summary>
    /// Network Issues Metrics
    /// Alert if: Packets Outbound Errors + Received Errors > 1% of total packets AND Throughput drops > 50% from baseline (over 5 min)
    /// </summary>
    public class NetworkMetrics
    {
        public List<NetworkInterfaceInfo> Interfaces { get; set; }
        public bool AlertTriggered { get; set; }
        public string AlertMessage { get; set; }

        public NetworkMetrics()
        {
            Interfaces = new List<NetworkInterfaceInfo>();
        }
    }

    public class NetworkInterfaceInfo
    {
        public string InterfaceName { get; set; }
        public double BytesTotalPerSec { get; set; }
        public double PacketsOutboundErrors { get; set; }
        public double PacketsReceivedErrors { get; set; }
        public double TotalPackets { get; set; }
        public double ErrorPercentage { get; set; }
        public bool AlertTriggered { get; set; }
    }
}
