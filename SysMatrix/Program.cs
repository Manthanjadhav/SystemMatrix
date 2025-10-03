using System;
using System.Threading.Tasks;
using Newtonsoft.Json; 
using SysMatrix.Helpers;

namespace SysMatrix
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var instanceData = InstanceInfoHelper.GetCurrentInstanceMetadata();
                // Call the helper method to collect all monitoring data
                //var monitoringData = await MonitoringHelper.CollectAllMonitoringDataAsync();

                //string jsonOutput = JsonConvert.SerializeObject(monitoringData, Formatting.Indented);

                //Console.WriteLine(jsonOutput);

                //// Optionally save to file
                //string fileName = $"monitoring_data_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                //System.IO.File.WriteAllText(fileName, jsonOutput);
                //Console.WriteLine($"\nData saved to: {fileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
