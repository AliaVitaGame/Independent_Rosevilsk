using System.Threading.Tasks;
using Unity.Services.Multiplay.Authoring.Core.Assets;

namespace Unity.Services.Multiplay.Authoring.Core.MultiplayApi
{
    static class BuildConfigApiExtensions
    {
        public static async Task<BuildConfigurationId> FindOrCreate(this IBuildConfigApi api, string name, BuildId buildId, MultiplayConfig.BuildConfigurationDefinition definition)
        {
            var existing = await api.FindByName(name);
            if (existing != null)
            {
                return existing.Value;
            }

            return await api.Create(name, buildId, definition);
        }
    }
}
