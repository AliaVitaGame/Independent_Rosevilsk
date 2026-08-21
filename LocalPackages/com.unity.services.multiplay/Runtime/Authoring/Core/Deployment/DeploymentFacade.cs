using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Multiplay.Authoring.Core.Assets;
using Unity.Services.Multiplay.Authoring.Core.Builds;
using Unity.Services.Multiplay.Authoring.Core.Exceptions;
using Unity.Services.Multiplay.Authoring.Core.Model;
using Unity.Services.Multiplay.Authoring.Core.MultiplayApi;
using Unity.Services.Multiplay.Authoring.Core.Threading;
using ILogger = Unity.Services.Multiplay.Authoring.Core.Logging.ILogger;

namespace Unity.Services.Multiplay.Authoring.Core.Deployment
{
    class DeploymentFacade : IDeploymentFacade
    {
        readonly IMultiplayBuildAuthoring m_Builder;
        readonly IBuildFileManagement m_FileManagement;
        readonly ICloudStorage m_CloudStorage;
        readonly IBuildsApi m_BuildsApi;
        readonly IBuildConfigApi m_ConfigApi;
        readonly IFleetApi m_FleetApi;
        readonly IAllocationApi m_AllocationApi;
        readonly IDispatchToMainThread m_Dispatcher;
        readonly ILogger m_Logger;

        public DeploymentFacade(
            IMultiplayBuildAuthoring builder,
            IBuildFileManagement fileManagement,
            ICloudStorage cloudStorage,
            IBuildsApi buildsApi,
            IBuildConfigApi buildConfigApi,
            IFleetApi fleetApi,
            IAllocationApi allocationApi,
            IDispatchToMainThread dispatcher,
            ILogger logger)
        {
            m_Builder = builder;
            m_FileManagement = fileManagement;
            m_CloudStorage = cloudStorage;
            m_BuildsApi = buildsApi;
            m_ConfigApi = buildConfigApi;
            m_FleetApi = fleetApi;
            m_AllocationApi = allocationApi;
            m_Dispatcher = dispatcher;
            m_Logger = logger;
        }

        public Task BuildBinaryAsync(BuildItem buildItem, CancellationToken cancellationToken = default)
        {
            var directory = buildItem.Definition.BuildPath ?? m_FileManagement.DefaultBuildPath(buildItem.OriginalName.ToString());

            var res = m_Builder.BuildMultiplayServer(directory, buildItem.Definition.ExecutableName);

            return Task.CompletedTask;
        }

        public Task WarnBuildTargetChanged(CancellationToken cancellationToken = default)
        {
            m_Builder.WarnBuildTargetChanged();
            return Task.CompletedTask;
        }

        public async Task<BuildUploadResult> UploadBuildAsync(BuildItem buildItem, CancellationToken cancellationToken = default)
        {
            m_Logger.LogVerbose($"[{nameof(DeploymentFacade)}] Finding build '{buildItem.OriginalName.ToString()}'...");
            var(buildId, bucketId) = await m_BuildsApi.FindOrCreate(buildItem.OriginalName.ToString(), buildItem.Definition, cancellationToken);

            m_Logger.LogVerbose($"[{nameof(DeploymentFacade)}] Getting Files to upload...");
            var directory = buildItem.Definition.BuildPath ?? m_FileManagement.DefaultBuildPath(buildItem.OriginalName.ToString());
            var buildEntries = m_FileManagement.GetBuildEntriesAtPath(directory);

            int updatedItems = 0;
            int totalItems = buildEntries.Count;
            Action<BuildEntry> onUpdated = entry =>
            {
                // No need for concurrency management, since these actions will be queued
                // on the main thread message pump
                m_Dispatcher.DispatchAction(() =>
                {
                    try
                    {
                        updatedItems++;
                        float partialProgress = 100f * updatedItems / totalItems;
                        buildItem.Progress = 33f + partialProgress / 3;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                });
            };

            m_Logger.LogVerbose($"[{nameof(DeploymentFacade)}] Uploading Files...");
            var changes = await m_CloudStorage.UploadBuildEntries(bucketId, buildEntries, onUpdated, cancellationToken);

            m_Logger.LogVerbose($"[{nameof(DeploymentFacade)}] Completed Upload Build");
            return new BuildUploadResult(buildItem, buildId, bucketId, changes);
        }

        public async Task<bool> SyncBuildAsync(
            bool createNewVersion,
            BuildItem build,
            BuildId buildId,
            CloudBucketId bucketId,
            CancellationToken cancellationToken = default)
        {
            if (createNewVersion)
            {
                m_Logger.LogVerbose($"[{nameof(DeploymentFacade)}] Creating Build Version...");
                build.SetStatusDescription("Creating Version");
                await m_BuildsApi.CreateVersion(buildId, bucketId, cancellationToken);
            }

            build.Progress += 16f;

            m_Logger.LogVerbose($"[{nameof(DeploymentFacade)}] Syncing Build...");
            build.SetStatusDescription("Syncing build");

            var onProgress = GetOnProgressCallback(build);
            var success = await m_BuildsApi.WaitForSync(buildId, onProgress, cancellationToken);
            if (!success)
            {
                build.Status = DeploymentStatus.FailedToDeploy;
                build.SetStatusDetail("Failed to sync build in the allocated time");
            }
            return success;
        }

        internal Action<float> GetOnProgressCallback(BuildItem build)
        {
            float baseProgress = build.Progress;
            float leftProgress = 100 - baseProgress;
            Action<float> onProgress = syncProgress =>
            {
                m_Dispatcher.DispatchAction(() =>
                {
                    m_Logger.LogVerbose($"[{nameof(DeploymentFacade)}] Syncing Build ({syncProgress}%)...");
                    var partialProgress = syncProgress / 100f * leftProgress;
                    build.Progress = baseProgress + partialProgress;
                });
            };
            return onProgress;
        }

        public async Task<BuildId> FindBuildAsync(BuildName name, CancellationToken cancellationToken = default)
        {
            var info = await m_BuildsApi.FindByName(name.ToString(), cancellationToken);
            return info?.Item1 ?? throw new BuildNotFoundException(name.ToString());
        }

        public async Task<BuildConfigurationId> DeployBuildConfigAsync(BuildConfigurationName name, BuildId buildId, MultiplayConfig.BuildConfigurationDefinition definition, CancellationToken cancellationToken = default)
        {
            m_Logger.LogVerbose($"[{nameof(DeploymentFacade)}] Deploying BuildConfig '{name}'...");
            var configId = await m_ConfigApi.FindByName(name.ToString(), cancellationToken);
            if (configId == null)
            {
                return await m_ConfigApi.Create(name.ToString(), buildId, definition, cancellationToken);
            }

            await m_ConfigApi.Update(configId.Value, name.ToString(), buildId, definition, cancellationToken);
            return configId.Value;
        }

        public async Task<BuildConfigurationId> FindBuildConfigAsync(BuildConfigurationName name, CancellationToken cancellationToken = default)
        {
            var info = await m_ConfigApi.FindByName(name.ToString(), cancellationToken);
            return info ?? throw new Exception("BuildConfig not found");
        }

        public async Task<FleetId> DeployFleetAsync(FleetName name, IList<BuildConfigurationId> buildConfigs, MultiplayConfig.FleetDefinition definition, CancellationToken cancellationToken = default)
        {
            var fleetInfo = await m_FleetApi.FindByName(name.ToString(), cancellationToken);
            if (fleetInfo == null)
            {
                var fleet = await m_FleetApi.Create(name.ToString(), buildConfigs, definition, cancellationToken);
                return fleet.Id;
            }

            await m_FleetApi.Update(fleetInfo.Id, name.ToString(), buildConfigs, definition, fleetInfo.OsId, cancellationToken);
            return fleetInfo.Id;
        }

        public async Task<AllocationInformation> CreateAndSyncTestAllocationAsync(FleetName fleetName, BuildConfigurationName buildConfigurationName, CancellationToken cancellationToken = default)
        {
            var fleetInfo = await m_FleetApi.FindByName(fleetName.ToString(), cancellationToken);
            if (fleetInfo == null)
            {
                throw new ArgumentException($"Fleet {fleetName.ToString()} not found");
            }
            var buildConfigurationId = await FindBuildConfigAsync(buildConfigurationName, cancellationToken);
            var allocationResult = await m_AllocationApi.CreateTestAllocation(fleetInfo.Id, fleetInfo.Regions.First().RegionId, buildConfigurationId.Id, cancellationToken);

            var allocationInformation = await m_AllocationApi.WaitForAllocation(fleetInfo.Id, allocationResult.AllocationId, cancellationToken);
            return allocationInformation;
        }

        public Task<List<AllocationInformation>> ListTestAllocations(FleetId fleetId, CancellationToken cancellationToken = default)
        {
            return m_AllocationApi.ListTestAllocations(fleetId, cancellationToken);
        }

        public Task RemoveTestAllocation(FleetId fleetId, Guid allocationId, CancellationToken token)
        {
            return m_AllocationApi.RemoveTestAllocation(fleetId, allocationId, token);
        }

        public Task<Dictionary<string, Guid>> GetAvailableRegions()
        {
            return m_FleetApi.GetRegions();
        }

        public Task<IReadOnlyList<FleetInfo>> GetFleets()
        {
            return m_FleetApi.List();
        }

        public Task DeleteFleet(FleetId fleetId)
        {
            return m_FleetApi.DeleteFleet(fleetId);
        }

        public async Task InitAsync()
        {
            await m_CloudStorage.InitAsync();
            await m_BuildsApi.InitAsync();
            await m_ConfigApi.InitAsync();
            await m_FleetApi.InitAsync();
            await m_AllocationApi.InitAsync();
        }
    }
}
