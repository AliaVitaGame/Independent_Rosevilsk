// WARNING: Auto generated code. Modifications will be lost!
using System.Threading.Tasks;

namespace Unity.Services.Multiplay.Authoring.Editor.Shared.Clients
{
    interface IAccessTokens
    {
        string GenesisAccessToken { get; }
        Task<string> GetServicesGatewayTokenAsync();
    }
}
