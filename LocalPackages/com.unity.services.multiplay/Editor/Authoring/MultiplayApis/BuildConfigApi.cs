using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Multiplay.Authoring.Core.Assets;
using Unity.Services.Multiplay.Authoring.Core.MultiplayApi;
using Unity.Services.Multiplay.Authoring.Editor.AdminApis.BuildConfigs.Apis.BuildConfigurations;
using Unity.Services.Multiplay.Authoring.Editor.AdminApis.BuildConfigs.BuildConfigurations;
using Unity.Services.Multiplay.Authoring.Editor.AdminApis.BuildConfigs.Models;

namespace Unity.Services.Multiplay.Authoring.Editor.MultiplayApis
{
    class BuildConfigApi : IBuildConfigApi
    {
        IMultiplayApiConfig m_ApiConfig;
        readonly IBuildConfigurationsApiClient m_Client;
        readonly IApiAuthenticator m_ApiInit;

        public BuildConfigApi(IBuildConfigurationsApiClient client, IApiAuthenticator apiInit)
        {
            m_Client = client;
            m_ApiInit = apiInit;
            m_ApiConfig = ApiConfig.Empty;
        }

        public async Task InitAsync()
        {
            var(config, basePath, headers) = await m_ApiInit.Authenticate();
            ((BuildConfigurationsApiClient)m_Client).Configuration = new AdminApis.BuildConfigs.Configuration(
                basePath,
                null,
                null,
                headers);
            m_ApiConfig = config;
        }

        public async Task<BuildConfigurationId?> FindByName(string name, CancellationToken cancellationToken = default)
        {
            var request = new ListBuildConfigurationsRequest(m_ApiConfig.ProjectId, m_ApiConfig.EnvironmentId, partialFilter: name);
            var response = await TryCatchRequestAsync(request, async(req) => {
                return await m_Client.ListBuildConfigurationsAsync(req);
            });
            var result = response.Result.FirstOrDefault(
                b => b.Name == name);

            if (result == null)
                return null;

            return new BuildConfigurationId { Id = result.Id };
        }

        public async Task<BuildConfigurationId> Create(string name, BuildId buildId, MultiplayConfig.BuildConfigurationDefinition definition, CancellationToken cancellationToken = default)
        {
            var request = new CreateBuildConfigurationRequest(m_ApiConfig.ProjectId, m_ApiConfig.EnvironmentId,
                new BuildConfigurationCreateRequest(
                    name,
                    buildId.Id,
                    definition.QueryType.ToString().ToLower(),
                    definition.BinaryPath,
                    definition.CommandLine,
                    definition.Cores,
                    definition.SpeedMhz,
                    definition.MemoryMiB,
                    configuration: definition.Variables.Select(kv => new ConfigurationPair(kv.Key, kv.Value)).ToList()
                )
            );
            var response = await TryCatchRequestAsync(request, async(req) => {
                return await m_Client.CreateBuildConfigurationAsync(request);
            });
            return new BuildConfigurationId { Id = response.Result.Id };
        }

        public async Task Update(BuildConfigurationId id, string name, BuildId buildId, MultiplayConfig.BuildConfigurationDefinition definition, CancellationToken cancellationToken = default)
        {
            var request = new UpdateBuildConfigurationRequest(
                m_ApiConfig.ProjectId,
                m_ApiConfig.EnvironmentId,
                id.Id,
                new BuildConfigurationUpdateRequest(
                    name,
                    buildId.Id,
                    definition.Variables.Select(kv => new ConfigurationPair1(0, kv.Key, kv.Value)).ToList(),
                    definition.QueryType.ToString().ToLower(),
                    definition.BinaryPath,
                    definition.CommandLine,
                    definition.Cores,
                    definition.SpeedMhz,
                    definition.MemoryMiB
                )
            );
            await TryCatchRequestAsync(request, async(req) => {
                return await m_Client.UpdateBuildConfigurationAsync(req);
            });
        }

        public async Task Clear()
        {
            var request = new ListBuildConfigurationsRequest(m_ApiConfig.ProjectId, m_ApiConfig.EnvironmentId);
            var response = await TryCatchRequestAsync(request, async(req) => {
                return await m_Client.ListBuildConfigurationsAsync(req);
            });
            foreach (var build in response.Result)
            {
                try
                {
                    var delete = new DeleteBuildConfigurationRequest(m_ApiConfig.ProjectId, m_ApiConfig.EnvironmentId, build.Id, dryRun: false);
                    await m_Client.DeleteBuildConfigurationAsync(delete);
                }
                catch (Exception)
                {
                    // Ignored because some build configs can not be removed
                }
            }
        }

        async Task<AdminApis.BuildConfigs.Response<TResponse>> TryCatchRequestAsync<TRequest, TResponse>(TRequest request, Func<TRequest, Task<AdminApis.BuildConfigs.Response<TResponse>>> func)
        {
            AdminApis.BuildConfigs.Response<TResponse> response;
            try
            {
                response = await func(request);
            }
            catch (AdminApis.BuildConfigs.Http.HttpException<ListBuildConfigurations400Response> ex)
            {
                throw new MultiplayAuthoringException((int)ex.Response.StatusCode, ex.ActualError.Detail, ex);
            }
            catch (AdminApis.BuildConfigs.Http.HttpException<ListBuildConfigurations401Response> ex)
            {
                throw new MultiplayAuthoringException((int)ex.Response.StatusCode, ex.ActualError.Detail, ex);
            }
            catch (AdminApis.BuildConfigs.Http.HttpException<ListBuildConfigurations403Response> ex)
            {
                throw new MultiplayAuthoringException((int)ex.Response.StatusCode, ex.ActualError.Detail, ex);
            }
            catch (AdminApis.BuildConfigs.Http.HttpException<ListBuildConfigurations404Response> ex)
            {
                throw new MultiplayAuthoringException((int)ex.Response.StatusCode, ex.ActualError.Detail, ex);
            }
            catch (AdminApis.BuildConfigs.Http.HttpException<ListBuildConfigurations429Response> ex)
            {
                throw new MultiplayAuthoringException((int)ex.Response.StatusCode, ex.ActualError.Detail, ex);
            }
            catch (AdminApis.BuildConfigs.Http.HttpException<ListBuildConfigurations500Response> ex)
            {
                throw new MultiplayAuthoringException((int)ex.Response.StatusCode, "Internal Server Error", ex);
            }
            return response;
        }
    }
}
