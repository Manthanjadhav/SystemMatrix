using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SysMatrix.Models
{
    /// <summary>
    /// Root data model containing all monitoring metrics
    /// </summary>
    public class MonitoringData
    {
        public DateTime CollectionTimestamp { get; set; }
        public CpuMetrics CpuMetrics { get; set; }
        public MemoryMetrics MemoryMetrics { get; set; }
        public DiskMetrics DiskMetrics { get; set; }
        public DiskIoMetrics DiskIoMetrics { get; set; }
        public NetworkMetrics NetworkMetrics { get; set; }
        public WebServerMetrics WebServerMetrics { get; set; }
        public DatabaseMetrics DatabaseMetrics { get; set; }
        public ServiceMetrics ServiceMetrics { get; set; }
        public string ErrorMessage { get; set; }
    }
}
