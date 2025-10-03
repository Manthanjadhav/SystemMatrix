using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SysMatrix.Models
{
    public static class Constant
    {
        public const double DISK_SEC_THRESHOLD_MS = 25.0; // 25 milliseconds
        public const double QUEUE_LENGTH_MULTIPLIER = 2.0;
        public const double CONNECTION_USAGE_THRESHOLD = 85.0; // 85%
        public const int CONNECTION_FAILURES_THRESHOLD = 5; // 5 per minute
        public const double AVG_QUERY_DURATION_THRESHOLD_MS = 2000.0; // 2 seconds
        public const double LOG_FILE_USAGE_THRESHOLD = 85.0; // 85%
        public const double LOG_BACKUP_THRESHOLD_MINUTES = 30.0;
        public const double AVAILABLE_MEMORY_THRESHOLD = 10.0; // 10%
        public const double PAGES_PER_SEC_THRESHOLD = 2000.0;
        public const double ERROR_PERCENTAGE_THRESHOLD = 1.0; // 1%
        public const double ERROR_5XX_PERCENTAGE_THRESHOLD = 2.0; // 2%
        public const double RESPONSE_TIME_THRESHOLD_MS = 2000.0; // 2 seconds
        public const int HEALTH_PROBE_FAILURE_THRESHOLD = 3;
        public static string METADATA_URL = ConfigurationManager.AppSettings["METADATA_URL"]!= null ? ConfigurationManager.AppSettings["METADATA_URL"].ToString() : "URL";
        public static string DYNAMIC_URL = ConfigurationManager.AppSettings["DYNAMIC_URL"] != null ? ConfigurationManager.AppSettings["DYNAMIC_URL"].ToString() : "URL";
    }
}
