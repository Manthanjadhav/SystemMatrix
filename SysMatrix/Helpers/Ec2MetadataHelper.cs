using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SysMatrix.Collector;
using SysMatrix.Models;

namespace SysMatrix.Helpers
{
    public class Ec2MetadataHelper
    {
        private readonly Ec2MetadataCollector _collector;

        public Ec2MetadataHelper()
        {
            _collector = new Ec2MetadataCollector();
        }

        public async Task<string> CollectAndReturnJsonAsync()
        {
            try
            {
                Console.WriteLine("========================================");
                Console.WriteLine("   EC2 Metadata Collection Started");
                Console.WriteLine("========================================\n");

                // Call collector to gather all data
                Console.WriteLine("Collecting EC2 metadata from instance...\n");
                var metadata = await _collector.CollectAsync();

                if (metadata.IsSuccess)
                {
                    // Convert to JSON
                    var jsonSettings = new JsonSerializerSettings
                    {
                        Formatting = Formatting.Indented,
                        NullValueHandling = NullValueHandling.Include
                    };

                    string json = JsonConvert.SerializeObject(metadata, jsonSettings);

                    // Print to console
                    Console.WriteLine("EC2 METADATA (JSON):");
                    Console.WriteLine("========================================");
                    Console.WriteLine(json);
                    Console.WriteLine("========================================\n");

                    // Save to file
                    string filename = $"ec2-metadata-{DateTime.Now:yyyyMMdd-HHmmss}.json";
                    System.IO.File.WriteAllText(filename, json);
                    Console.WriteLine($"✓ Data saved to: {filename}\n"); 

                    return json;
                }
                else
                {
                    string errorMessage = $"ERROR: {metadata.ErrorMessage}";
                    Console.WriteLine(errorMessage);
                    return errorMessage;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"FATAL ERROR: {ex.Message}";
                Console.WriteLine(errorMessage);
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return errorMessage;
            }
        } 
    }
}