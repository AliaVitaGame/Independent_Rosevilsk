#if MULTIPLAY_API_AVAILABLE && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Services.Core.Editor.Environments;
using Unity.Services.Multiplay.Authoring.Core.Assets;
using Unity.Services.Multiplay.Authoring.Core.Deployment;
using Unity.Services.Multiplay.Authoring.Core.Model;
using Unity.Services.Multiplay.Authoring.Core.MultiplayApi;
using Unity.Services.Multiplay.Authoring.Editor;
using Unity.Services.Multiplayer.Editor.Shared.DependencyInversion;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;


namespace Unity.Multiplayer.PlayMode.Scenarios.Editor.Utils
{
    internal static class MultiplayUtils
    {
        [SerializeReference] static IScopedServiceProvider _ServiceProviderOverride;

        //<summary>
        //This method is used to recover the available fleet regions to which the fleet can be deployed.
        //</summary>
        public static async Task<List<string>> GetAvailableFleetRegions()
        {
            var provider = await CreateAndValidateServiceProviderScope();
            var deployer = provider.GetService<IMultiplayDeployer>();
            await deployer.InitAsync();
            var regions = await deployer.GetAvailableRegions();
            List<string> availableFleetRegions = new List<string>();
            foreach (KeyValuePair<string, Guid> region in regions)
            {
                availableFleetRegions.Add(region.Key);
            }
            return availableFleetRegions;
        }

        //<summary>
        //This method will list all the existing fleets available for this project and will shutdown all of them.
        //</summary>
        public static async Task ShutdownAllExistingFleets()
        {
            await ShutdownFleets(await GetFleetsDetails());
        }

        //<summary>
        //This method will list all the existing fleets deployed for this project.
        //</summary>
        public static async Task<List<FleetInfo>> GetFleetsDetails()
        {
            var provider = await CreateAndValidateServiceProviderScope();
            var deployer = provider.GetService<IMultiplayDeployer>();
            await deployer.InitAsync();
            return (List<FleetInfo>)await deployer.GetFleets();
        }

        //<summary>
        //This method will return all the allocation information for all the fleets deployed for this project.
        //[activeOnly] - If true, will return only the active allocations.
        //</summary>
        public static async Task<Dictionary<FleetId,List<AllocationInformation>>> GetAllTheAllocationsInformation(bool activeOnly = false)
        {
            List<FleetInfo> fleets = await GetFleetsDetails();
            Dictionary<FleetId,List<AllocationInformation>> allocations = new Dictionary<FleetId,List<AllocationInformation>>();
            foreach (var fleet in fleets)
            {
                List<AllocationInformation> allocationInformation = await GetAllocationsInformationForFleet(fleet);
                if(activeOnly)
                {
                    allocationInformation.RemoveAll(x => x.ServerId == 0);
                }
                if(allocationInformation.Count > 0)
                    allocations.Add(fleet.Id,allocationInformation);
            }
            return allocations;
        }

        //<summary>
        //This method will remove all the allocations for all the fleets deployed for this project.
        //</summary>
        public static async Task RemoveAllAllocationForAllFleets()
        {
            List<FleetInfo> fleets = await GetFleetsDetails();
            foreach (var fleet in fleets)
            {
                await RemoveAllAllocationsForFleet(fleet);
            }
        }
        //<summary>
        //This method will remove all the allocations for the given fleet.
        //</summary>
        public static async Task RemoveAllAllocationsForFleet(FleetInfo fleetsInfo)
        {
            List<AllocationInformation> allocations = await GetAllocationsInformationForFleet(fleetsInfo);
            var provider = await CreateAndValidateServiceProviderScope();
            var deployer = provider.GetService<IMultiplayDeployer>();
            await deployer.InitAsync();
            foreach (var allocation in allocations)
            {
                await deployer.RemoveTestAllocation(fleetsInfo.Id, allocation.AllocationId, CancellationToken.None);
            }
        }

        //<summary>
        //This method will return the allocation information for the given fleet.
        //</summary>
        public static async Task<List<AllocationInformation>> GetAllocationsInformationForFleet(FleetInfo fleetsInfo)
        {
            var provider = await CreateAndValidateServiceProviderScope();
            var deployer = provider.GetService<IMultiplayDeployer>();
            await deployer.InitAsync();
            return await deployer.ListTestAllocations(fleetsInfo.Id, CancellationToken.None);
        }

        //<summary>
        //This method will shutdown the server for the given id.
        //</summary>
        public static async Task<bool> ShutDownServer(long serverId, CancellationToken token = default) => await TriggerServerActionAsync(serverId, ServerAction.Stop, token);
        //<summary>
        //This method will start the server for the given id.
        //</summary>
        public static async Task<bool> StartServer(long serverId, CancellationToken token = default) => await TriggerServerActionAsync(serverId, ServerAction.Start, token);

        private static async Task<bool> TriggerServerActionAsync(long serverId, ServerAction action, CancellationToken token = default)
        {
            var provider = await CreateAndValidateServiceProviderScope();
            var servers = provider.GetService<IServersApi>();
            await servers.InitAsync();
            return await servers.TriggerServerActionAsync(serverId, action, token);
        }

        //<summary>
        //This method will shutdown all the fleets provided in the list.
        //</summary>
        public static async Task<List<FleetItem>> ShutdownFleets(List<FleetInfo> fleetsInfo)
        {
            var fleetItems = new List<FleetItem>();
            foreach (var fleetInfo in fleetsInfo)
            {
                Dictionary<string, MultiplayConfig.ScalingDefinition> regions =  new Dictionary<string, MultiplayConfig.ScalingDefinition>();
                foreach (var region in fleetInfo.Regions)
                {
                    regions.Add(region.Name, new MultiplayConfig.ScalingDefinition{MaxServers = 0, MinAvailable = 0, Online = false});
                }
                List<BuildConfigurationName> buildConfigurationNames =  new List<BuildConfigurationName>();
                foreach (var BuildConfigInfos in fleetInfo.BuildConfigInfos)
                {
                    buildConfigurationNames.Add(new BuildConfigurationName() {Name = BuildConfigInfos.Name});
                }
                var fleetItem = new FleetItem
                {
                    OriginalName = new FleetName {Name = fleetInfo.FleetName},
                    Name = fleetInfo.FleetName,
                    Definition = new MultiplayConfig.FleetDefinition
                    {
                        BuildConfigurations = buildConfigurationNames,
                        Regions = regions
                    }
                };
                fleetItems.Add(fleetItem);
            }
            var provider = await CreateAndValidateServiceProviderScope();
            var deployer = provider.GetService<IMultiplayDeployer>();
            await deployer.InitAsync();
            await deployer.Deploy(fleetItems, CancellationToken.None);
            return fleetItems;
        }
        public static IScopedServiceProvider CreateServiceProviderScope(bool isForCI = false) => _ServiceProviderOverride != null ? _ServiceProviderOverride.CreateScope() : MultiplayAuthoringServices.Provider.CreateScope();

        //<summary>
        //This method will create the service provider for the Unity Multiplayer Game services.
        // [isForCI] - If true, will create a MOC service provider for our CI environment.
        //</summary>
        public static async Task<IScopedServiceProvider> CreateAndValidateServiceProviderScope(bool isForCI = false)
        {
            var provider = CreateServiceProviderScope(isForCI);
            try
            {
                await ValidateEnvironment(provider);
                return provider;
            }
            catch (Exception)
            {
                provider.Dispose();
                throw;
            }
        }

        private static async Task ValidateEnvironment(IScopedServiceProvider provider)
        {
            var environments = provider.GetService<IEnvironmentsApi>();
            await environments.RefreshAsync();
            var environmentValidation = await environments.ValidateEnvironmentAsync();
            if (environmentValidation.Failed)
                throw new InvalidOperationException(environmentValidation.ErrorMessage);
        }

        public static void LogCloudSetupErrors()
        {
            Debug.LogError(
                "Play Mode Scenario: To use a Multiplay Hosting remote server, this project needs to be connected to Unity Cloud. " +
                "You can set this up in <a href=\"OpenProjectServices\">Edit > Project Settings > Services</a>.");
            LogEnvironmentSetupErrors();
            // if cloud project ID is not set, we cannot proceed with a Udash project URL, so this log will not have a link to Udash project page
            LogMultiplaySetupErrors(isProjectDashboardLinkAvailable:false);
        }

        public static void LogEnvironmentSetupErrors()
        {
            Debug.LogError(
                "Play Mode Scenario: After connecting your project to the Unity Cloud, an environment needs to be selected. " +
                "You can set this up in <a href=\"OpenEnvironmentSettings\">Edit > Project Settings > Services > Environment</a>.");
        }

        public static void LogMultiplaySetupErrors(bool isProjectDashboardLinkAvailable = true)
        {
            if (isProjectDashboardLinkAvailable)
            {
                var organizationKey = CloudProjectSettings.organizationKey;
                var projectId = CloudProjectSettings.projectId;
                var dashboardUrl = $"https://cloud.unity.com/home/organizations/{organizationKey}/projects/{projectId}";
                Debug.LogError(
                    "Play Mode Scenario: To use a Multiplay Hosting remote instance, you must have Multiplay enabled on your project dashboard. " +
                    $"Please check your <a href=\"{dashboardUrl}\">project dashboard</a> for more details.");
            }
            else
            {
                Debug.LogError(
                    "Play Mode Scenario: To use a Multiplay Hosting remote instance, you must have Multiplay enabled on your project dashboard. " +
                    "Please check your project dashboard for more details.");
            }
        }

        public static void LogServiceTokenErrors()
        {
            const string unityCloudServiceStatusUrl = "https://status.unity.com/";
            Debug.LogError(
                $"Unexpected error: failed to get service token. Please try again later. For more information, " +
                $"check the <a href=\"{unityCloudServiceStatusUrl}\">Unity Cloud Service Status</a>.");
        }

        // This class handles the hyperlink clicks in the Project Settings window for Multiplay Settings
        [InitializeOnLoad]
        internal static class MultiplayProjectSettingsHyperlinkHandler
        {
            static MultiplayProjectSettingsHyperlinkHandler()
            {
                EditorGUI.hyperLinkClicked += OnHyperlinkClicked;
            }

            private static void OnHyperlinkClicked(EditorWindow window, HyperLinkClickedEventArgs args)
            {
                if (!args.hyperLinkData.TryGetValue("href", out var link)) return;
                switch (link)
                {
                    case "OpenProjectServices":
                        SettingsService.OpenProjectSettings("Project/Services");
                        break;
                    case "OpenEnvironmentSettings":
                        SettingsService.OpenProjectSettings("Project/Services/Environments");
                        break;
                }
            }
        }
    }
}
#endif
