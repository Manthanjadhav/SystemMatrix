using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SysMatrix.Models
{
    /// <summary>
    /// Disk I/O Bottleneck Metrics
    /// Alert if: Avg Disk sec/Read or sec/Write > 25 ms AND Avg Disk Queue Length > 2 × number of cores/disks
    /// </summary>
    public class DiskIoMetrics
    {
        public List<DiskIoInfo> Disks { get; set; }
        public bool AlertTriggered { get; set; }
        public string AlertMessage { get; set; }

        public DiskIoMetrics()
        {
            Disks = new List<DiskIoInfo>();
        }
    }

    public class DiskIoInfo
    {
        public string DiskName { get; set; }
        public double AvgDiskSecRead { get; set; }
        public double AvgDiskSecWrite { get; set; }
        public double AvgDiskQueueLength { get; set; }
        public bool AlertTriggered { get; set; }
    }
}
