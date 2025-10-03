using System;
using System.Collections.Generic;

namespace SysMatrix.Models
{
    public class Ec2Metadata
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime CollectionTime { get; set; }

        public InstanceBasicInfo InstanceBasic { get; set; }
        public AmiInfo Ami { get; set; }
        public PlacementInfo Placement { get; set; }
        public NetworkPrimaryInfo NetworkPrimary { get; set; }
        public BlockDeviceInfo BlockDevice { get; set; }
        public SecurityInfo Security { get; set; }
        public SystemInfo System { get; set; }
        public SpotInstanceInfo SpotInstance { get; set; }
        public EventsInfo Events { get; set; }
        public List<NetworkInterfaceDetail> NetworkInterfaces { get; set; }
        public DynamicDataInfo DynamicData { get; set; }
        public string UserData { get; set; }
    }

    public class InstanceBasicInfo
    {
        public string InstanceId { get; set; }
        public string InstanceType { get; set; }
        public string InstanceAction { get; set; }
        public string InstanceLifeCycle { get; set; }
    }

    public class AmiInfo
    {
        public string AmiId { get; set; }
        public string AmiLaunchIndex { get; set; }
        public string AmiManifestPath { get; set; }
    }

    public class PlacementInfo
    {
        public string AvailabilityZone { get; set; }
        public string AvailabilityZoneId { get; set; }
        public string Region { get; set; }
        public string PartitionNumber { get; set; }
    }

    public class NetworkPrimaryInfo
    {
        public string Hostname { get; set; }
        public string LocalHostname { get; set; }
        public string LocalIpv4 { get; set; }
        public string PublicHostname { get; set; }
        public string PublicIpv4 { get; set; }
        public string MacAddress { get; set; }
    }

    public class BlockDeviceInfo
    {
        public string MappingAmi { get; set; }
        public string MappingRoot { get; set; }
        public string MappingEbs { get; set; }
        public string MappingEphemeral { get; set; }
    }

    public class SecurityInfo
    {
        public string SecurityGroups { get; set; }
        public string IamInfo { get; set; }
        public string IamSecurityCredentials { get; set; }
    }

    public class SystemInfo
    {
        public string ProductCodes { get; set; }
        public string KernelId { get; set; }
        public string RamdiskId { get; set; }
        public string ReservationId { get; set; }
        public string MetricsVhostmd { get; set; }
        public string ServicesDomain { get; set; }
        public string ServicesPartition { get; set; }
        public string SystemData { get; set; }
        public string Tags { get; set; }
    }

    public class SpotInstanceInfo
    {
        public string TerminationTime { get; set; }
        public string InstanceAction { get; set; }
    }

    public class EventsInfo
    {
        public string MaintenanceHistory { get; set; }
        public string MaintenanceScheduled { get; set; }
        public string RecommendationsRebalance { get; set; }
    }

    public class NetworkInterfaceDetail
    {
        public string MacAddress { get; set; }
        public string DeviceNumber { get; set; }
        public string InterfaceId { get; set; }
        public string LocalHostname { get; set; }
        public string LocalIpv4s { get; set; }
        public string PublicHostname { get; set; }
        public string PublicIpv4s { get; set; }
        public string Ipv6s { get; set; }
        public string SecurityGroupIds { get; set; }
        public string SecurityGroups { get; set; }
        public string SubnetId { get; set; }
        public string SubnetIpv4CidrBlock { get; set; }
        public string SubnetIpv6CidrBlocks { get; set; }
        public string VpcId { get; set; }
        public string VpcIpv4CidrBlock { get; set; }
        public string VpcIpv4CidrBlocks { get; set; }
        public string VpcIpv6CidrBlocks { get; set; }
        public string OwnerId { get; set; }
    }

    public class DynamicDataInfo
    {
        public string InstanceIdentityDocument { get; set; }
        public string InstanceIdentitySignature { get; set; }
        public string InstanceIdentityPkcs7 { get; set; }
    }
}