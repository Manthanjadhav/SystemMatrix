using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SysMatrix.Models
{
    /// <summary>
    /// CPU Pressure Metrics
    /// Alert if: CPU usage > 90% (avg over 5 min) AND Processor Queue Length > 2 per core
    /// </summary>
    public class CpuMetrics
    {
        public double ProcessorTimePercentage { get; set; }
        public double ProcessorQueueLength { get; set; }
        public int NumberOfCores { get; set; }
        public double QueueLengthPerCore { get; set; }
        public bool AlertTriggered { get; set; }
        public string AlertMessage { get; set; }
    }
}
