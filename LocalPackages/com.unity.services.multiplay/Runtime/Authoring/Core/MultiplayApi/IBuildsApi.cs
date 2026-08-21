using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Multiplay.Authoring.Core.Assets;
using Unity.Services.Multiplay.Authoring.Core.Builds;

namespace Unity.Services.Multiplay.Authoring.Core.MultiplayApi
{
    interface IBuildsApi : IInitializable
    {
        Task<(BuildId, CloudBucketId)?> FindByName(string name, CancellationToken cancellationToken = default);
        Task<(BuildId, CloudBucketId)> Create(string name, MultiplayConfig.BuildDefinition definition, CancellationToken cancellationToken = default);
        Task CreateVersion(BuildId id, CloudBucketId bucket, CancellationToken cancellationToken = default);
        Task<bool> IsSynced(BuildId id, CancellationToken cancellationToken = default);
    }
}
