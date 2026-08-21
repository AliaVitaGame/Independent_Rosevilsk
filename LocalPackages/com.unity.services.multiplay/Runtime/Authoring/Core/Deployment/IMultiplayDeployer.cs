using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Multiplay.Authoring.Core.Assets;
using Unity.Services.Multiplay.Authoring.Core.Model;
using Unity.Services.Multiplay.Authoring.Core.MultiplayApi;

namespace Unity.Services.Multiplay.Authoring.Core.Deployment
{
    /// <summary>
    /// Responsible to make available deployment functionality
    /// </summary>
    public interface IMultiplayDeployer
    {
        /// <summary>
        /// Initialize the MultiplayDeployer with an authenticated client
        /// </summary>
        /// <returns>Returns a Task representing the initialization</returns>
        Task InitAsync();

        /// <summary>
        /// Deploy the associated Multiplay Config items.
        /// Builds will be built and uploaded, Build Configurations and fleets will
        /// be created or updated according to the item.
        /// The item status and progress will be updated along the way.
        /// </summary>
        /// <param name="items">The items to deploy</param>
        /// <param name="token">A cancellation token</param>
        /// <returns>A Task that resolves once the deployment completes</returns>
        Task Deploy(IReadOnlyList<DeploymentItem> items, CancellationToken token = default);

        /// <summary>
        /// Build the binaries associated with the build items
        /// </summary>
        /// <param name="buildItems">The items to build</param>
        /// <param name="token">A cancellation token</param>
        /// <returns>
        /// A Task that resolves when the build is complete.
        /// The task embeds a tuple containing:
        /// <list type="bullet">
        /// <item><description>element1: successful builds</description></item>
        /// <item><description>element2: failed builds</description></item>
        /// </list>
        /// </returns>
        Task<(List<BuildItem>, List<BuildItem>)> BuildBinaries(
            IReadOnlyList<BuildItem> buildItems,
            CancellationToken token = default);

        /// <summary>
        /// Uploads the associated builds, and waits for them to be available.
        /// </summary>
        /// <param name="successfulBuilds">The builds to upload</param>
        /// <param name="token">A cancellation token</param>
        /// <returns>A Task embedding the result of the upload</returns>
        Task<UploadResult> UploadAndSyncBuilds(
            List<BuildItem> successfulBuilds,
            CancellationToken token = default);

        /// <summary>
        /// Creates or Updates the associated build configurations
        /// </summary>
        /// <param name="buildConfigurationItems">The build configuration</param>
        /// <param name="successfulUploads">The builds previously uploaded</param>
        /// <param name="token">A cancellation token</param>
        /// <returns>
        /// A task embedding a tuple containing:
        /// <list type="bullet">
        /// <item><description>element1: the successfully created build configs</description></item>
        /// <item><description>element2: the build configs whose deployment failed</description></item>
        /// </list>
        /// </returns>
        Task<(Dictionary<BuildConfigurationName, BuildConfigurationId>, List<BuildConfigurationItem>)> DeployBuildConfigs(
            IReadOnlyList<BuildConfigurationItem> buildConfigurationItems,
            Dictionary<BuildName, BuildId> successfulUploads,
            CancellationToken token);


        /// <summary>
        /// Creates or Updates the associated fleets
        /// </summary>
        /// <param name="items">The fleets to deploy</param>
        /// <param name="buildConfigIds">The build configurations. If the config IDs are known, they will not be searched remotely</param>
        /// <param name="token">A cancellation token</param>
        /// <returns>A task completing when the deployment completes</returns>
        Task DeployFleets(
            IReadOnlyList<FleetItem> items,
            Dictionary<BuildConfigurationName, BuildConfigurationId> buildConfigIds = null,
            CancellationToken token = default);

        /// <summary>
        /// Creates a test allocation for the associated Fleet
        /// </summary>
        /// <param name="fleetName">The fleet name</param>
        /// <param name="buildConfigurationName">The build configuration name</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task embedding the allocation information</returns>
        Task<AllocationInformation> CreateAndSyncTestAllocationAsync(
            FleetName fleetName,
            BuildConfigurationName buildConfigurationName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists existing test allocations for the associated fleet
        /// </summary>
        /// <param name="fleetId">The fleet Id</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task embedding the allocations information</returns>
        Task<List<AllocationInformation>> ListTestAllocations(FleetId fleetId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a request to process the removal of a test allocation
        /// </summary>
        /// <param name="fleetId">The fleet Id</param>
        /// <param name="allocationId">The allocation Id</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task completing when the allocations are removed</returns>
        Task RemoveTestAllocation(FleetId fleetId, Guid allocationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets regions that are available for fleet scaling options
        /// </summary>
        /// <returns>A task containing the regions data</returns>
        Task<Dictionary<string, Guid>> GetAvailableRegions();


        /// <summary>
        /// Deletes the Fleet
        /// </summary>
        /// <param name="fleetName">The fleet name</param>
        /// <returns>A tasks that completes when the fleet is deleted</returns>
        Task DeleteFleet(FleetId fleetName);

        /// <summary>
        /// Gets the information of the fleets
        /// </summary>
        /// <returns>A task embedding the fleets information</returns>
        public Task<IReadOnlyList<FleetInfo>> GetFleets();

        /// <summary>
        /// Represents the result of a build upload operation
        /// </summary>
        /// <param name="UploadResults">The individual upload result for the build item</param>
        /// <param name="FailedUploads">The Builds that failed to upload</param>
        /// <param name="SuccessfulSyncs">The builds that were successfully synced</param>
        /// <param name="FailedSyncs">The builds that failed to sync</param>
        public record UploadResult(
            Dictionary<BuildItem, BuildUploadResult> UploadResults,
            List<BuildItem> FailedUploads,
            Dictionary<BuildName, BuildId> SuccessfulSyncs,
            List<BuildItem> FailedSyncs);
    }
}
