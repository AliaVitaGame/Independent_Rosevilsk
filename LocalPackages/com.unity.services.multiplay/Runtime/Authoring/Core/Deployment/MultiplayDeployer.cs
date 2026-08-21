using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Multiplay.Authoring.Core.Assets;
using Unity.Services.Multiplay.Authoring.Core.Exceptions;
using Unity.Services.Multiplay.Authoring.Core.Model;
using Unity.Services.Multiplay.Authoring.Core.MultiplayApi;

namespace Unity.Services.Multiplay.Authoring.Core.Deployment
{
    /// <summary>
    /// Multiplay Deployer can deploy multiplay configuration items.
    /// </summary>
    class MultiplayDeployer : IMultiplayDeployer
    {
        IDeploymentFacade m_Deployment;

        /// <summary>
        /// Create an instance of MultiplayDeployer
        /// </summary>
        public MultiplayDeployer(IDeploymentFacade deployment)
        {
            m_Deployment = deployment;
        }

        /// <summary>
        /// Initialize the MultiplayDeployer with an authenticated client
        /// </summary>
        public async Task InitAsync()
        {
            await m_Deployment.InitAsync();
        }

        /// <summary>
        /// Deploy the associated Multiplay Config items.
        /// Builds will be built and uploaded, Build Configurations and fleets will
        /// be created or updated according to the item.
        /// The item status and progress will be updated along the way.
        /// </summary>
        public async Task Deploy(IReadOnlyList<DeploymentItem> items, CancellationToken token = default)
        {
            SetupStatuses(items);
            var(buildItems, buildConfigs, fleets) = GetValidItems(items);

            var(successfulBuilds, failedBuilds) = await BuildBinaries(buildItems, token);

            var uploadResult = await UploadAndSyncBuilds(successfulBuilds, token);


            buildConfigs = FilterBuildConfigs(buildConfigs, failedBuilds, uploadResult.FailedSyncs, uploadResult.FailedUploads);
            var(buildConfigIds, failedBuildConfigs)
                = await DeployBuildConfigs(buildConfigs, uploadResult.SuccessfulSyncs, token);

            fleets = FilterFleets(fleets, failedBuildConfigs);
            await DeployFleets(fleets, buildConfigIds, token);
        }

        /// <summary>
        /// Build the binaries associated with the build items
        /// </summary>
        public async Task<(List<BuildItem>, List<BuildItem>)> BuildBinaries(
            IReadOnlyList<BuildItem> buildItems,
            CancellationToken token = default)
        {
            return await CreateBuildBinaries(buildItems, token);
        }

        /// <summary>
        /// Uploads the associated builds, and waits for them to be available.
        /// </summary>
        public async Task<IMultiplayDeployer.UploadResult> UploadAndSyncBuilds(
            List<BuildItem> successfulBuilds,
            CancellationToken token = default)
        {
            var(uploadResults, failedUploads) = await UploadBuilds(successfulBuilds, token);

            var(successfulSyncs, failedSyncs) = await SyncBuilds(uploadResults, token);
            return new IMultiplayDeployer.UploadResult(uploadResults, failedUploads, successfulSyncs, failedSyncs);
        }

        async Task<(List<BuildItem>, List<BuildItem>)> CreateBuildBinaries(
            IReadOnlyList<BuildItem> buildItems,
            CancellationToken token)
        {
            var successfulBuilds = new List<BuildItem>();
            var failedBuilds = new List<BuildItem>();
            foreach (var buildItem in buildItems)
            {
                try
                {
                    // These steps have to happen sequentially
                    buildItem.Status = new DeploymentStatus("Building Binaries", messageSeverity: SeverityLevel.Info);
                    var success = await CreateBuildBinary(buildItem, token);
                    if (success)
                    {
                        buildItem.Status = new DeploymentStatus("Successfully Built Binaries", messageSeverity: SeverityLevel.Info);
                        successfulBuilds.Add(buildItem);
                        buildItem.Progress = 33.33f;
                    }
                    else
                    {
                        failedBuilds.Add(buildItem);
                    }
                }
                catch (Exception e)
                {
                    buildItem.Status = DeploymentStatus.FailedToDeploy;
                    buildItem.SetStatusDescription($"Unexpected build error: {e}");
                    failedBuilds.Add(buildItem);
                }
            }

            if (buildItems.Any())
            {
                await m_Deployment.WarnBuildTargetChanged(token);
            }

            return (successfulBuilds, failedBuilds);
        }

        async Task<(Dictionary<BuildItem, BuildUploadResult>, List<BuildItem>)> UploadBuilds(
            List<BuildItem> successfulBuilds,
            CancellationToken token)
        {
            var uploadResults = new Dictionary<BuildItem, BuildUploadResult>();
            var failedUploads = new List<BuildItem>();
            foreach (var buildItem in successfulBuilds)
            {
                try
                {
                    var uploadResult = await m_Deployment.UploadBuildAsync(buildItem, token);
                    uploadResults.Add(buildItem, uploadResult);
                    buildItem.Progress = 66f;
                }
                catch (MultiplayAuthoringException e)
                {
                    var status = new DeploymentStatus(
                        $"Failed to Upload build: {e.Message}.",
                        "Please make sure you have enable game server hosting in the dashboard.",
                        SeverityLevel.Error);
                    buildItem.Status = status;
                    failedUploads.Add(buildItem);
                }
                catch (Exception e)
                {
                    buildItem.Status = DeploymentStatus.FailedToDeploy;
                    buildItem.SetStatusDescription($"Failed to Upload build: {e}");
                    failedUploads.Add(buildItem);
                }
            }

            return (uploadResults, failedUploads);
        }

        async Task<(Dictionary<BuildName, BuildId>, List<BuildItem>)> SyncBuilds(
            Dictionary<BuildItem, BuildUploadResult> uploadResults,
            CancellationToken token)
        {
            var successSyncs = new Dictionary<BuildName, BuildId>();
            var failedSyncs = new List<BuildItem>();
            foreach (var(buildItem, uploadResult) in uploadResults)
            {
                try
                {
                    bool createNewVersion = uploadResult.Changes != 0;
                    var syncComplete = await m_Deployment.SyncBuildAsync(
                        createNewVersion,
                        buildItem,
                        uploadResult.BuildId,
                        uploadResult.CloudBucketId,
                        token);

                    if (!syncComplete)
                    {
                        buildItem.Status = DeploymentStatus.FailedToDeploy;
                        buildItem.SetStatusDescription($"Failed to synchronize Build in the allocated time.");
                        failedSyncs.Add(buildItem);
                        continue;
                    }

                    successSyncs.Add(buildItem.OriginalName, uploadResult.BuildId);
                    buildItem.Progress = 100f;
                    buildItem.Status = new DeploymentStatus("Deployed", messageSeverity: SeverityLevel.Success);
                }
                catch (Exception e)
                {
                    buildItem.Status = DeploymentStatus.FailedToDeploy;
                    buildItem.SetStatusDescription($"Failed to synchronize new build: {e}");
                    failedSyncs.Add(buildItem);
                }
            }

            return (successSyncs, failedSyncs);
        }

        /// <summary>
        /// Creates or Updates the associated build configurations
        /// </summary>
        public async Task<(Dictionary<BuildConfigurationName, BuildConfigurationId>, List<BuildConfigurationItem>)> DeployBuildConfigs(
            IReadOnlyList<BuildConfigurationItem> buildConfigurationItems,
            Dictionary<BuildName, BuildId> successfulUploads,
            CancellationToken token)
        {
            var buildConfigIds = new Dictionary<BuildConfigurationName, BuildConfigurationId>();
            var failedBuildConfigs = new List<BuildConfigurationItem>();
            foreach (var buildConfig in buildConfigurationItems)
            {
                if (!successfulUploads.ContainsKey(buildConfig.Definition.Build))
                {
                    // Build was previously deployed, we're just updating the config
                    try
                    {
                        var buildId = await m_Deployment.FindBuildAsync(buildConfig.Definition.Build, token);
                        successfulUploads.Add(buildConfig.Definition.Build, buildId);
                        buildConfig.Progress = 50f;
                    }
                    catch (BuildNotFoundException e)
                    {
                        buildConfig.Status = DeploymentStatus.FailedToDeploy;
                        buildConfig.SetStatusDescription(e.Message);
                        failedBuildConfigs.Add(buildConfig);
                        continue;
                    }
                    catch (Exception e)
                    {
                        buildConfig.Status = DeploymentStatus.FailedToDeploy;
                        buildConfig.SetStatusDescription(e.ToString());
                        failedBuildConfigs.Add(buildConfig);
                        continue;
                    }
                }

                try
                {
                    var id = await m_Deployment.DeployBuildConfigAsync(
                        buildConfig.OriginalName,
                        successfulUploads[buildConfig.Definition.Build],
                        buildConfig.Definition,
                        token);
                    buildConfigIds.Add(buildConfig.OriginalName, id);
                    buildConfig.Progress = 100f;
                    buildConfig.Status = new DeploymentStatus("Deployed", messageSeverity: SeverityLevel.Success);
                }
                catch (Exception e)
                {
                    buildConfig.Status = DeploymentStatus.FailedToDeploy;
                    buildConfig.SetStatusDescription(e.ToString());
                    failedBuildConfigs.Add(buildConfig);
                }
            }

            return (buildConfigIds, failedBuildConfigs);
        }

        public async Task DeployFleets(
            IReadOnlyList<FleetItem> items,
            Dictionary<BuildConfigurationName, BuildConfigurationId> buildConfigIds = null,
            CancellationToken token = default)
        {
            buildConfigIds ??= new Dictionary<BuildConfigurationName, BuildConfigurationId>();
            foreach (var fleet in items)
            {
                try
                {
                    var ids = new List<BuildConfigurationId>();
                    foreach (var configuration in fleet.Definition.BuildConfigurations)
                    {
                        if (!buildConfigIds.ContainsKey(configuration))
                        {
                            buildConfigIds.Add(
                                configuration,
                                await m_Deployment.FindBuildConfigAsync(configuration, token));
                        }
                    }

                    ids = fleet.Definition.BuildConfigurations.Select(c => buildConfigIds[c]).ToList();

                    await m_Deployment.DeployFleetAsync(
                        fleet.OriginalName,
                        ids,
                        fleet.Definition,
                        token);
                    fleet.Progress = 100f;
                    fleet.Status = new DeploymentStatus("Deployed", messageSeverity: SeverityLevel.Success);
                }
                catch (Exception e)
                {
                    fleet.Status = DeploymentStatus.FailedToDeploy;
                    fleet.SetStatusDetail($"Failed to deploy: {e.Message}");
                }
            }
        }

        (IReadOnlyList<BuildItem>, IReadOnlyList<BuildConfigurationItem>, IReadOnlyList<FleetItem>) GetValidItems(
            IReadOnlyList<IDeploymentItem> items)
        {
            var builds = items.OfType<BuildItem>().ToList();
            var buildConfigs = items.OfType<BuildConfigurationItem>().ToList();
            var fleets = items.OfType<FleetItem>().ToList();

            var validBuilds = DuplicateResourceValidation.FilterDuplicateResources(builds, out var duplicateBuilds);
            var validBuildConfigs = DuplicateResourceValidation.FilterDuplicateResources(buildConfigs, out var duplicateBuildConfigs);
            var validFleets = DuplicateResourceValidation.FilterDuplicateResources(fleets, out var duplicateFleets);

            UpdateDuplicateResourceStatus(duplicateBuilds);
            UpdateDuplicateResourceStatus(duplicateBuildConfigs);
            UpdateDuplicateResourceStatus(duplicateFleets);

            return (validBuilds, validBuildConfigs, validFleets);
        }

        void SetupStatuses(IReadOnlyList<IDeploymentItem> items)
        {
            foreach (var fleet in items.OfType<FleetItem>())
            {
                fleet.Progress = 0f;
                fleet.Status = new DeploymentStatus("Waiting for Builds", messageSeverity: SeverityLevel.Info);
            }

            foreach (var buildConfigs in items.OfType<BuildConfigurationItem>())
            {
                buildConfigs.Progress = 0f;
                buildConfigs.Status = new DeploymentStatus("Waiting for Builds", messageSeverity: SeverityLevel.Info);
            }

            foreach (var builds in items.OfType<BuildItem>())
            {
                builds.Progress = 0f;
                builds.Status = new DeploymentStatus("Waiting for other Builds", messageSeverity: SeverityLevel.Info);
            }
        }

        void UpdateDuplicateResourceStatus(
            IReadOnlyList<IGrouping<string, DeploymentItem>> duplicateGroups)
        {
            foreach (var group in duplicateGroups)
            {
                foreach (var item in group)
                {
                    var(shortMes, message) = DuplicateResourceValidation.GetDuplicateResourceErrorMessages(item, group.ToList());
                    item.Status = DeploymentStatus.FailedToDeploy;
                    item.SetStatusDetail(message);
                }
            }
        }

        public Task<AllocationInformation> CreateAndSyncTestAllocationAsync(
            FleetName fleetName,
            BuildConfigurationName buildConfigurationName,
            CancellationToken cancellationToken = default)
        {
            return m_Deployment.CreateAndSyncTestAllocationAsync(fleetName, buildConfigurationName, cancellationToken);
        }

        public Task<List<AllocationInformation>> ListTestAllocations(FleetId fleetId, CancellationToken cancellationToken = default)
        {
            return m_Deployment.ListTestAllocations(fleetId, cancellationToken);
        }

        public Task RemoveTestAllocation(FleetId fleetId, Guid allocationId, CancellationToken cancellationToken = default)
        {
            return m_Deployment.RemoveTestAllocation(fleetId, allocationId, cancellationToken);
        }

        public Task<Dictionary<string, Guid>> GetAvailableRegions()
        {
            return m_Deployment.GetAvailableRegions();
        }

        public Task DeleteFleet(FleetId fleetId)
        {
            return m_Deployment.DeleteFleet(fleetId);
        }

        public Task<IReadOnlyList<FleetInfo>> GetFleets()
        {
            return m_Deployment.GetFleets();
        }

        List<BuildConfigurationItem> FilterBuildConfigs(
            IReadOnlyList<BuildConfigurationItem> buildConfigs,
            List<BuildItem> failedBuilds,
            List<BuildItem> failedSyncs,
            List<BuildItem> failedUploads)
        {
            var deployableConfigs = new List<BuildConfigurationItem>();
            foreach (var buildConfig in buildConfigs)
            {
                var buildName = buildConfig.Definition.Build;
                if (failedBuilds.Any(b => b.OriginalName == buildName))
                {
                    buildConfig.Status = DeploymentStatus.FailedToDeploy;
                    buildConfig.SetStatusDescription($"Underlying build '{buildName}' failed to build, skipping BuildConfig deployment.");
                    continue;
                }
                if (failedUploads.Any(b => b.OriginalName == buildName))
                {
                    buildConfig.Status = DeploymentStatus.FailedToDeploy;
                    buildConfig.SetStatusDescription($"Underlying build '{buildName}' failed to upload, skipping BuildConfig deployment.");
                    continue;
                }
                if (failedSyncs.Any(b => b.OriginalName == buildName))
                {
                    buildConfig.Status = DeploymentStatus.FailedToDeploy;
                    buildConfig.SetStatusDescription($"Underlying build '{buildName}' failed to sync, skipping BuildConfig deployment.");
                    continue;
                }

                deployableConfigs.Add(buildConfig);
            }

            return deployableConfigs;
        }

        List<FleetItem> FilterFleets(
            IReadOnlyList<FleetItem> fleets,
            List<BuildConfigurationItem> failedBuildConfigs)
        {
            var deployableFleets = new List<FleetItem>();
            var failedBuildConfigNames = failedBuildConfigs
                .Select(bc => bc.OriginalName)
                .ToList();
            foreach (var fleetItem in fleets)
            {
                var intersection = fleetItem.Definition
                    .BuildConfigurations
                    .Where(bc => failedBuildConfigNames.Any(name => name.Equals(bc)))
                    .ToList();

                if (intersection.Count > 0)
                {
                    string failedBcNames = string.Join(", ", intersection);
                    fleetItem.Status = DeploymentStatus.FailedToDeploy;
                    fleetItem.SetStatusDescription($"Failed to deploy because some underlying build configurations failed to deploy in this process: {failedBcNames}");
                    continue;
                }
                deployableFleets.Add(fleetItem);
            }

            return deployableFleets;
        }

        async Task<bool> CreateBuildBinary(BuildItem buildItem,
            CancellationToken cancellationToken)
        {
            try
            {
                buildItem.Progress = 0;
                await m_Deployment.BuildBinaryAsync(buildItem, cancellationToken);
            }
            catch (BuildFailedException e)
            {
                buildItem.Status = DeploymentStatus.FailedToDeploy;
                buildItem.SetStatusDetail(e.Message);
                return false;
            }

            return true;
        }
    }
}
