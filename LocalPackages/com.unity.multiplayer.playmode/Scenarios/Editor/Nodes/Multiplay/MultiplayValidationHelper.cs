// TODO: MTT-8667 This should be moved to a proper assembly and remove these scripting defines.
#if MULTIPLAY_API_AVAILABLE && UNITY_EDITOR
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Nodes.Multiplay.Exceptions;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Utils;
using Unity.Services.Core.Editor.Environments;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor.Nodes.Multiplay
{
    internal class MultiplayValidationHelper : IMultiplayValidationHelper
    {
        internal struct ValidateRemoteResult
        {
            public string AuthToken;
            public string EnvironmentId;
            public string ProjectId;
        }

        public async Task<ValidateRemoteResult> ValidateRemoteAsync(CancellationToken cancellationToken)
        {
            var environmentValidation = await EnvironmentsApi.Instance.ValidateEnvironmentAsync();
            if (environmentValidation.Failed)
                throw new MultiplayEnvironmentException(environmentValidation.ErrorMessage);

            var activeEnv = EnvironmentsApi.Instance.ActiveEnvironmentId;
            if (activeEnv == null || string.IsNullOrEmpty(activeEnv.ToString()) || activeEnv == Guid.Empty)
            {
                throw new MultiplayEnvironmentException("No environment selected in Edit > Project Settings > Services > Environment.");
            }

            var token = await CloudProjectSettings.GetServiceTokenAsync(cancellationToken);
            if (string.IsNullOrEmpty(token))
            {
                throw new MultiplayUdashServiceTokenException("Failed to get service token");
            }

            return new ValidateRemoteResult()
            {
                AuthToken = token,
                EnvironmentId = activeEnv.ToString(),
                ProjectId = CloudProjectSettings.projectId
            };
        }

        public bool IsProjectLinkedToUnityCloud()
        {
            return !string.IsNullOrEmpty(CloudProjectSettings.projectId);
        }

        public async Task<Scenario.ValidationResult> ValidateMultiplayProviderStatusAsync(
            ValidateRemoteResult validateRemoteResult, CancellationToken cancellationToken)
        {
            var endpoint =
                $"https://services.unity.com/api/multiplay/providers/v4/projects/{validateRemoteResult.ProjectId}/environments/{validateRemoteResult.EnvironmentId}/providers";
            var accessToken = validateRemoteResult.AuthToken;

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync(endpoint, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                MultiplayUtils.LogMultiplaySetupErrors();
                return new Scenario.ValidationResult(false, "Multiplay Hosting needs to be enabled");
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var json = JsonUtility.FromJson<ProviderResponse>(responseBody);

            if (json?.data == null || json.data is not { Length: > 0 } || json.data[0].status != "PROVIDER_READY")
            {
                MultiplayUtils.LogMultiplaySetupErrors();
                return new Scenario.ValidationResult(false, "Multiplay Hosting needs to be enabled");
            }
            return new Scenario.ValidationResult(true, string.Empty);
        }
    }

    // Classes to handle the JSON response from the Udash Multiplay Provider API
    [Serializable]
    internal class ProviderResponse
    {
        public ProviderData[] data;
    }

    [Serializable]
    internal class ProviderData
    {
        public string id;
        public string status;
        public string type;
    }
}
#endif
