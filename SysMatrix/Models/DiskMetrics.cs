using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SysMatrix.Models
{
    /// <summary>
    /// Disk Space Metrics
    /// Alert if: Free space < 15% AND Free space < 5 GB
    /// </summary>
    public class DiskMetrics
    {
        public List<DiskInfo> Disks { get; set; }
        public bool AlertTriggered { get; set; }
        public string AlertMessage { get; set; }

        public DiskMetrics()
        {
            Disks = new List<DiskInfo>();
        }
    }

    public class DiskInfo
    {
        public string DriveName { get; set; }
        public double FreeSpacePercentage { get; set; }
        public double FreeMegabytes { get; set; }
        public double TotalSizeMegabytes { get; set; }
        public bool AlertTriggered { get; set; }
    }
}
