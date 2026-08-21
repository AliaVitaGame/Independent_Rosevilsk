using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.Services.Multiplay.Authoring.Editor.MultiplayApis
{
    interface IApiAuthenticator
    {
        Task<(ApiConfig, string, IDictionary<string, string>)> Authenticate();
    }

    record ApiConfig(Guid ProjectId, Guid EnvironmentId) : IMultiplayApiConfig
    {
        internal static ApiConfig Empty => new ApiConfig(Guid.Empty, Guid.Empty);
    }
}
