using System;

namespace SysMatrix.Models
{
    public class Ec2Metadata
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime CollectionTime { get; set; }

        public InstanceBasicInfo InstanceBasic { get; set; }
        public PlacementInfo Placement { get; set; }
        public NetworkPrimaryInfo NetworkPrimary { get; set; }
        public SystemInfo System { get; set; }
    }

    public class InstanceBasicInfo
    {
        public string InstanceId { get; set; }
        public string InstanceType { get; set; } 
    }

    public class PlacementInfo
    {
        public string AvailabilityZone { get; set; }
        public string Region { get; set; }
    }

    public class NetworkPrimaryInfo
    {
        public string LocalIpv4 { get; set; }
        public string PublicHostname { get; set; }
        public string PublicIpv4 { get; set; }
    }

    public class SystemInfo
    {
        public string Name { get; set; }
    }
}