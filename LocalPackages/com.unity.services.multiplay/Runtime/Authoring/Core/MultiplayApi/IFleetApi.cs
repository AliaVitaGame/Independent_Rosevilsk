using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Multiplay.Authoring.Core.Assets;

namespace Unity.Services.Multiplay.Authoring.Core.MultiplayApi
{
    interface IFleetApi : IInitializable
    {
        Task<FleetInfo> FindByName(string name, CancellationToken cancellationToken = default);
        Task<FleetInfo> Create(string name,
            IList<BuildConfigurationId> buildConfigurations,
            MultiplayConfig.FleetDefinition definition,
            CancellationToken cancellationToken = default);
        Task Update(
            FleetId id,
            string name,
            IList<BuildConfigurationId> buildConfigurations,
            MultiplayConfig.FleetDefinition definition,
            Guid osID,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<FleetInfo>> List(CancellationToken cancellationToken = default);

        Task<Dictionary<string, Guid>> GetRegions();

        Task DeleteFleet(FleetId fleetId);
    }
}
