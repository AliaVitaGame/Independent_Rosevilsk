using System;
using System.Collections.Generic;
using Unity.Services.Core.Editor;
using Unity.Services.Core.Editor.Environments;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Multiplay.Authoring.Core;
using Unity.Services.Multiplay.Authoring.Core.Assets;
using Unity.Services.Multiplay.Authoring.Core.Builds;
using Unity.Services.Multiplay.Authoring.Core.Deployment;
using Unity.Services.Multiplay.Authoring.Core.Logging;
using Unity.Services.Multiplay.Authoring.Core.MultiplayApi;
using Unity.Services.Multiplay.Authoring.Core.Threading;
using Unity.Services.Multiplay.Authoring.Editor.AdminApis.Ccd.Scheduler;
using Unity.Services.Multiplay.Authoring.Editor.Analytics;
using Unity.Services.Multiplay.Authoring.Editor.Assets;
using Unity.Services.Multiplay.Authoring.Editor.Builds;
using Unity.Services.Multiplay.Authoring.Editor.CloudContent;
using Unity.Services.Multiplay.Authoring.Editor.Deployment;
using Unity.Services.Multiplay.Authoring.Editor.Logging;
using Unity.Services.Multiplay.Authoring.Editor.MultiplayApis;
using Unity.Services.Multiplay.Authoring.Editor.Shared.Analytics;
using Unity.Services.Multiplay.Authoring.Editor.Shared.Assets;
using Unity.Services.Multiplay.Authoring.Editor.Shared.Chrono;
using Unity.Services.Multiplay.Authoring.Editor.Shared.DependencyInversion;
using UnityEditor;
using static Unity.Services.Multiplay.Authoring.Editor.Shared.DependencyInversion.Factories;
using IAccessTokens = Unity.Services.Core.Editor.IAccessTokens;

namespace Unity.Services.Multiplay.Authoring.Editor
{
    /// <summary>
    /// Provides access to the different services used for the Admin APIs and Deployment capabilities
    /// </summary>
    public static class MultiplayAuthoringServices
    {
        /// <summary>
        /// The service provider
        /// </summary>
        public static IScopedServiceProvider Provider { get; }

        static MultiplayAuthoringServices()
        {
            var collection = new ServiceCollection(true);
            Register(collection);
            Provider = collection.Build();
        }

        [InitializeOnLoadMethod]
        static void RegisterDeploymentProvider()
        {
            Deployments.Instance.DeploymentProviders.Add(Provider.GetService<DeploymentProvider>());
        }

        internal static void Register(ServiceCollection collection)
        {
            collection.Register(Default<IFileReader, FileReader>);
            collection.Register(Default<IBuildFileManagement, BuildFileManagement>);
            collection.Register(Default<IBinaryBuilder, BinaryBuilder>);
            collection.Register(Default<IMultiplayBuildAuthoring, MultiplayBuildAuthoring>);
            collection.Register(Default<IMultiplayConfigValidator, MultiplayConfigValidator>);

            collection.Register(Default<IAccessTokens, AccessTokens>);
            collection.Register(Default<ICurrentTime, CurrentTime>);

            collection.Register(Default<IAssetAnalytics, AssetAnalytics>);
            collection.Register(Default<IBuildsAnalytics, BuildsAnalytics>);
            collection.Register(Default<IDeployAnalytics, DeployAnalytics>);
            collection.Register(Default<ICommonAnalytics, CommonAnalytics>);
            collection.Register(Default<IAnalyticsUtils, AnalyticsUtils>);
#if UNITY_2023_2_OR_NEWER
            collection.Register(Default<ICommonAnalyticProvider, CommonAnalyticProvider>);
#endif

            collection.RegisterSingleton(Default<ObservableAssets<MultiplayConfigAsset>, MultiplayConfigObservableAssets>);

            collection.Register(Default<IDispatchToMainThread, DispatcherToMainThread>);
            collection.Register(Default<ITaskDelay, DefaultTaskDelay>);
            collection.Register(Default<IFleetApi, FleetApi>);
            collection.Register(Default<ILogsApi, LogsApi>);
            collection.Register(Default<IAllocationApi, AllocationApi>);
            collection.Register(Default<IBuildsApi, BuildsApi>);
            collection.Register(Default<IServersApi, ServersApi>);
            collection.Register(Default<IBuildConfigApi, BuildConfigApi>);
            collection.Register(Default<IMultiplayDeployer, MultiplayDeployer>);
            collection.Register(Default<IApiAuthenticator, ApiAuthenticator>);
            collection.Register(Default<ICloudStorage, CcdCloudStorage>);
            collection.Register(Default<ICcdAnalytics, DummyCcdAnalytics>);
            collection.Register(Default<IDeploymentFacade, DeploymentFacade>);

            RegisterAdminBoilerplate(collection);

            collection.RegisterSingleton<IItemStore>(_ => AssetPersistenceStore.Instance);
            collection.RegisterSingleton(_ => EnvironmentsApi.Instance);

            collection.RegisterSingleton<DeploymentProvider>(sp =>
            {
                var provider = new MultiplayDeploymentProvider(
                    new MultiplayConfigObservableAssets(new AssetAnalytics(sp.GetService<IAnalyticsUtils>())),
                    new DeployCommand(sp.GetService<IDeployAnalytics>()),
                    sp.GetService<IItemStore>()
                );
                provider.Commands.Add(new BuildCommand());
                provider.Commands.Add(new UploadCommand());
                return provider;
            });

            collection.Register(Default<ILogger, Logger>);
            collection.RegisterStartupSingleton(Default<FleetStatusTracker>);
        }

        static void RegisterAdminBoilerplate(ServiceCollection collection)
        {
            collection.Register(
                Default<AdminApis.Allocations.Apis.Allocations.IAllocationsApiClient,
                        AdminApis.Allocations.Apis.Allocations.AllocationsApiClient>);
            collection.Register(
                Default<AdminApis.Builds.Apis.Builds.IBuildsApiClient, AdminApis.Builds.Apis.Builds.BuildsApiClient>);
            collection.Register(
                Default<AdminApis.BuildConfigs.Apis.BuildConfigurations.IBuildConfigurationsApiClient,
                        AdminApis.BuildConfigs.Apis.BuildConfigurations.BuildConfigurationsApiClient>);
            collection.Register(
                Default<AdminApis.Fleets.Apis.Fleets.IFleetsApiClient, AdminApis.Fleets.Apis.Fleets.FleetsApiClient>);
            collection.Register(
                Default<AdminApis.Servers.Apis.Servers.IServersApiClient, AdminApis.Servers.Apis.Servers.ServersApiClient>);
            collection.Register(Default<AdminApis.Logs.Apis.Logs.ILogsApiClient, AdminApis.Logs.Apis.Logs.LogsApiClient>);

            collection.Register(
                Default<AdminApis.Ccd.Apis.Entries.IEntriesApiClient, AdminApis.Ccd.Apis.Entries.EntriesApiClient>);
            collection.Register(
                Default<AdminApis.Ccd.Apis.Buckets.IBucketsApiClient, AdminApis.Ccd.Apis.Buckets.BucketsApiClient>);

            collection.Register(_ => new AdminApis.Allocations.Configuration("", null, null, new Dictionary<string, string>()));
            collection.Register(_ => new AdminApis.Builds.Configuration("", null, null, new Dictionary<string, string>()));
            collection.Register(_ => new AdminApis.BuildConfigs.Configuration("", null, null, new Dictionary<string, string>()));
            collection.Register(_ => new AdminApis.Fleets.Configuration("", null, null, new Dictionary<string, string>()));
            collection.Register(_ => new AdminApis.Servers.Configuration("", null, null, new Dictionary<string, string>()));
            collection.Register(_ => new AdminApis.Logs.Configuration("", null, null, new Dictionary<string, string>()));
            collection.Register(_ => new AdminApis.Ccd.Configuration("", null, null, new Dictionary<string, string>()));

            collection.Register(Default<System.Net.Http.HttpClient>);
            collection.Register(Default<AdminApis.Allocations.Http.IHttpClient, AdminApis.Allocations.Http.HttpClient>);
            collection.Register(Default<AdminApis.Builds.Http.IHttpClient, AdminApis.Builds.Http.HttpClient>);
            collection.Register(Default<AdminApis.Fleets.Http.IHttpClient, AdminApis.Fleets.Http.HttpClient>);
            collection.Register(Default<AdminApis.Ccd.Http.IHttpClient, AdminApis.Ccd.Http.HttpClient>);
            collection.Register(Default<AdminApis.Logs.Http.IHttpClient, AdminApis.Logs.Http.HttpClient>);
            collection.Register(Default<AdminApis.Servers.Http.IHttpClient, AdminApis.Servers.Http.HttpClient>);
            collection.Register(Default<AdminApis.BuildConfigs.Http.IHttpClient, AdminApis.BuildConfigs.Http.HttpClient>);
        }

        /// <summary>
        /// Get the service specified by the type T
        /// </summary>
        /// <param name="provider">Provider in which to find service</param>
        /// <typeparam name="T">Type to get</typeparam>
        /// <returns>The instance of the specified service if registered. Throws otherwise</returns>
        public static T GetService<T>(this IServiceProvider provider)
        {
            return (T)provider.GetService(typeof(T));
        }
    }
}
