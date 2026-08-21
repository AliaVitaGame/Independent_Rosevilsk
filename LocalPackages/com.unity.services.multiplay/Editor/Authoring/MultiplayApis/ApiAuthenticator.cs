using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Core.Editor.Environments;
using Unity.Services.Multiplay.Authoring.Editor.Shared.Clients;
using UnityEditor;

using ICoreAccessTokens = Unity.Services.Core.Editor.IAccessTokens;

namespace Unity.Services.Multiplay.Authoring.Editor.MultiplayApis
{
    class ApiAuthenticator : IApiAuthenticator
    {
        const string k_StgUrl = "https://staging.services.unity.com";

        readonly ICoreAccessTokens m_AccessTokens;
        readonly IEnvironmentsApi m_EnvironmentsApi;

        public ApiAuthenticator(ICoreAccessTokens accessTokens, IEnvironmentsApi environmentsApi)
        {
            m_AccessTokens = accessTokens;
            m_EnvironmentsApi = environmentsApi;
        }

        public async Task<(ApiConfig, string, IDictionary<string, string>)> Authenticate()
        {
            if (m_EnvironmentsApi.ActiveEnvironmentId == null)
            {
                throw new EnvironmentNotFoundException("Authentication failed. Please make sure the environment in Project Settings > Environments has been set.");
            }

            var config = new ApiConfig(
                Guid.Parse(CloudProjectSettings.projectId),
                m_EnvironmentsApi.ActiveEnvironmentId.Value
            );

            var gatewayToken = await m_AccessTokens.GetServicesGatewayTokenAsync();

            var adminApiHeaders = new AdminApiHeaders<ApiAuthenticator>(gatewayToken);
            var headers = adminApiHeaders.ToDictionary();
            return (config, CloudEnvironmentConfigProvider.IsStaging() ? k_StgUrl : null, headers);
        }
    }
}
