using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
using SysMatrix.Models;

namespace SysMatrix.Collector
{
    public class DatabaseCollector
    {
        private string connectionString = "Server=localhost;Database=master;Integrated Security=true;Connection Timeout=5;";

        public async Task<DatabaseMetrics> CollectAsync()
        {
            return await Task.Run(() =>
            {
                var metrics = new DatabaseMetrics();

                try
                {
                    // First check if SQL Server is available
                    using (var connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        metrics.DatabaseAvailable = true;

                        // Collect connection metrics
                        CollectConnectionMetrics(connection, metrics);

                        // Collect query performance metrics
                        CollectQueryPerformanceMetrics(connection, metrics);

                        // Collect transaction log health metrics
                        CollectTransactionLogMetrics(connection, metrics);

                        // Try to collect performance counters
                        CollectSqlServerPerformanceCounters(metrics);

                        // Check alerts
                        CheckAlerts(metrics);
                    }
                }
                catch (SqlException ex)
                {
                    metrics.DatabaseAvailable = false;
                    metrics.ErrorMessage = $"SQL Server connection error: {ex.Message}";
                }
                catch (Exception ex)
                {
                    metrics.DatabaseAvailable = false;
                    metrics.ErrorMessage = $"Error collecting database metrics: {ex.Message}";
                }

                return metrics;
            });
        }

        private void CollectConnectionMetrics(SqlConnection connection, DatabaseMetrics metrics)
        {
            try
            {
                // Get user connections and max connections
                string query = @"
                    SELECT 
                        (SELECT CAST(value_in_use AS INT) FROM sys.configurations WHERE name = 'user connections') AS MaxConnections,
                        (SELECT COUNT(*) FROM sys.dm_exec_sessions WHERE is_user_process = 1) AS UserConnections";

                using (var command = new SqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        metrics.MaxConnections = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                        metrics.UserConnections = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);

                        if (metrics.MaxConnections == 0)
                            metrics.MaxConnections = 32767; // Default max when set to 0

                        metrics.ConnectionUsagePercentage = Math.Round(
                            (metrics.UserConnections / (double)metrics.MaxConnections) * 100, 2);
                    }
                }

                // Estimate connection failures (would need more sophisticated tracking)
                metrics.ConnectionFailuresPerMinute = 0; // Placeholder
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error collecting connection metrics: {ex.Message}");
            }
        }

        private void CollectQueryPerformanceMetrics(SqlConnection connection, DatabaseMetrics metrics)
        {
            try
            {
                // Get slow query count and average duration
                string query = @"
                    SELECT 
                        COUNT(*) AS SlowQueryCount,
                        AVG(total_elapsed_time / execution_count / 1000.0) AS AvgDurationMs
                    FROM sys.dm_exec_query_stats
                    WHERE (total_elapsed_time / execution_count / 1000.0) > 2000"; // > 2 seconds

                using (var command = new SqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        metrics.SlowQueryCount = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                        metrics.AvgQueryDurationMs = reader.IsDBNull(1) ? 0 : Math.Round(reader.GetDouble(1), 2);
                    }
                }

                // Get 95th percentile (simplified approximation)
                metrics.QueryDuration95thPercentileMs = metrics.AvgQueryDurationMs * 1.5; // Approximation
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error collecting query performance metrics: {ex.Message}");
            }
        }

        private void CollectTransactionLogMetrics(SqlConnection connection, DatabaseMetrics metrics)
        {
            try
            {
                // Get transaction log usage
                string query = @"
                    SELECT TOP 1
                        CAST(SUM(CAST(FILEPROPERTY(name, 'SpaceUsed') AS BIGINT)) * 8.0 / 1024 AS DECIMAL(18,2)) AS LogUsedSizeKB,
                        CAST(SUM(size) * 8.0 / 1024 AS DECIMAL(18,2)) AS LogTotalSizeKB
                    FROM sys.database_files
                    WHERE type_desc = 'LOG'";

                using (var command = new SqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        metrics.LogFileUsedSizeKB = reader.IsDBNull(0) ? 0 : (double)reader.GetDecimal(0);
                        metrics.LogFileTotalSizeKB = reader.IsDBNull(1) ? 0 : (double)reader.GetDecimal(1);

                        if (metrics.LogFileTotalSizeKB > 0)
                        {
                            metrics.LogFileUsagePercentage = Math.Round(
                                (metrics.LogFileUsedSizeKB / metrics.LogFileTotalSizeKB) * 100, 2);
                        }
                    }
                }

                // Get last log backup time
                string backupQuery = @"
                    SELECT TOP 1 backup_finish_date
                    FROM msdb.dbo.backupset
                    WHERE database_name = DB_NAME() AND type = 'L'
                    ORDER BY backup_finish_date DESC";

                using (var command = new SqlCommand(backupQuery, connection))
                {
                    var result = command.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        metrics.LastLogBackupTime = (DateTime)result;
                        metrics.MinutesSinceLastLogBackup = Math.Round(
                            (DateTime.Now - metrics.LastLogBackupTime.Value).TotalMinutes, 2);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error collecting transaction log metrics: {ex.Message}");
            }
        }

        private void CollectSqlServerPerformanceCounters(DatabaseMetrics metrics)
        {
            try
            {
                // Try to get SQL Server performance counters
                var instanceName = GetSqlServerInstanceName();

                if (!string.IsNullOrEmpty(instanceName))
                {
                    // Batch Requests/sec
                    using (var batchCounter = new PerformanceCounter($"SQLServer:SQL Statistics", "Batch Requests/sec", null))
                    {
                        batchCounter.NextValue();
                        System.Threading.Thread.Sleep(100);
                        metrics.BatchRequestsPerSec = Math.Round(batchCounter.NextValue(), 2);
                    }

                    // SQL Compilations/sec
                    using (var compilationsCounter = new PerformanceCounter($"SQLServer:SQL Statistics", "SQL Compilations/sec", null))
                    {
                        compilationsCounter.NextValue();
                        System.Threading.Thread.Sleep(100);
                        metrics.SqlCompilationsPerSec = Math.Round(compilationsCounter.NextValue(), 2);
                    }

                    // Logins/sec
                    using (var loginsCounter = new PerformanceCounter($"SQLServer:SQL Statistics", "Logins/sec", null))
                    {
                        loginsCounter.NextValue();
                        System.Threading.Thread.Sleep(100);
                        metrics.LoginsPerSec = Math.Round(loginsCounter.NextValue(), 2);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error collecting SQL Server performance counters: {ex.Message}");
            }
        }

        private string GetSqlServerInstanceName()
        {
            try
            {
                var category = new PerformanceCounterCategory("SQLServer:SQL Statistics");
                return "SQLServer";
            }
            catch
            {
                // Try MSSQLSERVER or other instances
                try
                {
                    var category = new PerformanceCounterCategory("MSSQLSERVER:SQL Statistics");
                    return "MSSQLSERVER";
                }
                catch
                {
                    return null;
                }
            }
        }

        private void CheckAlerts(DatabaseMetrics metrics)
        {
            // Connection alert
            if (metrics.ConnectionUsagePercentage > Constant.CONNECTION_USAGE_THRESHOLD &&
                metrics.ConnectionFailuresPerMinute > Constant.CONNECTION_FAILURES_THRESHOLD)
            {
                metrics.ConnectionAlertTriggered = true;
                metrics.AlertMessage = $"Database Connection Alert: Connection usage at {metrics.ConnectionUsagePercentage}% " +
                                      $"(threshold: {Constant.CONNECTION_USAGE_THRESHOLD}%) and {metrics.ConnectionFailuresPerMinute} " +
                                      $"failures/min (threshold: {Constant.CONNECTION_FAILURES_THRESHOLD}). ";
            }

            // Query performance alert
            if (metrics.AvgQueryDurationMs > Constant.AVG_QUERY_DURATION_THRESHOLD_MS)
            {
                metrics.QueryPerformanceAlertTriggered = true;
                metrics.AlertMessage += $"Database Query Performance Alert: Average query duration at {metrics.AvgQueryDurationMs}ms " +
                                       $"(threshold: {Constant.AVG_QUERY_DURATION_THRESHOLD_MS}ms). ";
            }

            // Transaction log alert
            if (metrics.LogFileUsagePercentage > Constant.LOG_FILE_USAGE_THRESHOLD &&
                metrics.MinutesSinceLastLogBackup > Constant.LOG_BACKUP_THRESHOLD_MINUTES)
            {
                metrics.TransactionLogAlertTriggered = true;
                metrics.AlertMessage += $"Transaction Log Alert: Log file usage at {metrics.LogFileUsagePercentage}% " +
                                       $"(threshold: {Constant.LOG_FILE_USAGE_THRESHOLD}%) and {metrics.MinutesSinceLastLogBackup} minutes " +
                                       $"since last backup (threshold: {Constant.LOG_BACKUP_THRESHOLD_MINUTES} minutes)";
            }
        }
    }
}
