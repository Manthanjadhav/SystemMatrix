using System;

namespace SysMatrix.Models
{
    public class Ec2Metadata
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime CollectionTime { get; set; }

        // Flattened - all properties in single object
        public string InstanceId { get; set; }
        public string InstanceType { get; set; }
        public string AvailabilityZone { get; set; }
        public string Region { get; set; }
        public string LocalIpv4 { get; set; }
        public string PublicHostname { get; set; }
        public string PublicIpv4 { get; set; }
        public string Name { get; set; }
    }
}