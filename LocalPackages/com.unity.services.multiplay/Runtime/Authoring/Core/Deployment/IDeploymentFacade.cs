using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Multiplay.Authoring.Core.Assets;
using Unity.Services.Multiplay.Authoring.Core.Builds;
using Unity.Services.Multiplay.Authoring.Core.Model;
using Unity.Services.Multiplay.Authoring.Core.MultiplayApi;

namespace Unity.Services.Multiplay.Authoring.Core.Deployment
{
    interface IDeploymentFacade
    {
        Task InitAsync();
        Task BuildBinaryAsync(BuildItem buildItem, CancellationToken cancellationToken = default);
        Task WarnBuildTargetChanged(CancellationToken cancellationToken = default);
        Task<BuildUploadResult> UploadBuildAsync(BuildItem buildItem, CancellationToken cancellationToken = default);
        Task<bool> SyncBuildAsync(bool createNewVersion, BuildItem buildItem, BuildId buildId, CloudBucketId bucketId, CancellationToken cancellationToken = default);
        Task<BuildId> FindBuildAsync(BuildName name, CancellationToken cancellationToken = default);
        Task<BuildConfigurationId> DeployBuildConfigAsync(BuildConfigurationName name, BuildId buildId, MultiplayConfig.BuildConfigurationDefinition definition, CancellationToken cancellationToken = default);
        Task<BuildConfigurationId> FindBuildConfigAsync(BuildConfigurationName name, CancellationToken cancellationToken = default);
        Task<FleetId> DeployFleetAsync(FleetName name, IList<BuildConfigurationId> buildConfigs, MultiplayConfig.FleetDefinition definition, CancellationToken cancellationToken = default);
        Task<AllocationInformation> CreateAndSyncTestAllocationAsync(FleetName fleetName, BuildConfigurationName buildConfigurationName, CancellationToken cancellationToken = default);
        Task<List<AllocationInformation>> ListTestAllocations(FleetId fleetId, CancellationToken cancellationToken = default);
        Task RemoveTestAllocation(FleetId fleetId, Guid allocationId, CancellationToken cancellationToken = default);
        Task<Dictionary<string, Guid>> GetAvailableRegions();
        Task<IReadOnlyList<FleetInfo>> GetFleets();
        Task DeleteFleet(FleetId fleetId);
    }
}
