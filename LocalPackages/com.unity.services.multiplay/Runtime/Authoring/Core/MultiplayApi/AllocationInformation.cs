using System;

namespace Unity.Services.Multiplay.Authoring.Core.MultiplayApi
{
    /// <summary>
    /// The details of a test allocation.
    /// See https://services.docs.unity.com/multiplay-config/v1/#tag/Allocations/operation/GetTestAllocation
    /// for details
    /// </summary>
    /// <param name="AllocationId">The ID of the allocation</param>
    /// <param name="FleetId">The ID of the fleet</param>
    /// <param name="RegionId">The ID of the region</param>
    /// <param name="BuildConfigurationId">The ID of the build configuratoin</param>
    /// <param name="ServerId">The ID of the server</param>
    /// <param name="MachineId">The ID of the associated machine</param>
    /// <param name="Ipv4Address">IPv4 of the allocated server</param>
    /// <param name="Ipv6Address">IPv6 of the allocated server</param>
    /// <param name="GamePort">The port of the allocated server on the machine</param>
    public record AllocationInformation(Guid AllocationId, Guid FleetId, Guid RegionId, long BuildConfigurationId,
        long ServerId, long MachineId, string Ipv4Address, string Ipv6Address, long GamePort);
}
