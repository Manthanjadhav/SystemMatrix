using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SysMatrix.Models
{
    /// <summary>
    /// Database Metrics (SQL Server)
    /// - DB Connections Alert if: User Connections > 85% of max AND Connection Failures > 5/min
    /// - DB Query Performance Alert if: Slow query count > baseline × 2 in 5 min AND Avg query execution time > threshold (e.g., 2 sec)
    /// - Transaction Log Health Alert if: Log file usage > 85% AND No log backup in > 30 min
    /// </summary>
    public class DatabaseMetrics
    {
        // Connection Metrics
        public int UserConnections { get; set; }
        public int MaxConnections { get; set; }
        public double ConnectionUsagePercentage { get; set; }
        public double ConnectionResetPerSec { get; set; }
        public double LoginsPerSec { get; set; }
        public int ConnectionFailuresPerMinute { get; set; }

        // Query Performance Metrics
        public double BatchRequestsPerSec { get; set; }
        public double SqlCompilationsPerSec { get; set; }
        public int SlowQueryCount { get; set; }
        public double AvgQueryDurationMs { get; set; }
        public double QueryDuration95thPercentileMs { get; set; }

        // Transaction Log Health
        public double LogFileUsedSizeKB { get; set; }
        public double LogFileTotalSizeKB { get; set; }
        public double LogFileUsagePercentage { get; set; }
        public DateTime? LastLogBackupTime { get; set; }
        public double MinutesSinceLastLogBackup { get; set; }

        // Alerts
        public bool ConnectionAlertTriggered { get; set; }
        public bool QueryPerformanceAlertTriggered { get; set; }
        public bool TransactionLogAlertTriggered { get; set; }
        public string AlertMessage { get; set; }
        public bool DatabaseAvailable { get; set; }
        public string ErrorMessage { get; set; }
    }
}
