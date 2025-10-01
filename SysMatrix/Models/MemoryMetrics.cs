using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SysMatrix.Models
{
    /// <summary>
    /// Memory Pressure Metrics
    /// Alert if: Available Memory < 10% of total AND Pages/sec > 2000 (or > baseline × 2)
    /// </summary>
    public class MemoryMetrics
    {
        public double AvailableMBytes { get; set; }
        public double TotalMemoryMBytes { get; set; }
        public double AvailableMemoryPercentage { get; set; }
        public double PagesPerSec { get; set; }
        public bool AlertTriggered { get; set; }
        public string AlertMessage { get; set; }
    }
}
