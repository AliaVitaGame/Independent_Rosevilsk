using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Services.Multiplay.Authoring.Core.MultiplayApi
{
    interface IAllocationApi : IInitializable
    {
        Task<AllocationResult> CreateTestAllocation(FleetId fleetId, Guid regionId, long buildConfigurationId, CancellationToken cancellationToken = default);
        Task<AllocationInformation> GetTestAllocation(FleetId fleetId, Guid allocationId, CancellationToken cancellationToken = default);
        Task<List<AllocationInformation>> ListTestAllocations(
            FleetId fleetId,
            CancellationToken cancellationToken = default);

        Task RemoveTestAllocation(
            FleetId fleetId,
            Guid allocationId,
            CancellationToken cancellationToken = default);
    }
}
