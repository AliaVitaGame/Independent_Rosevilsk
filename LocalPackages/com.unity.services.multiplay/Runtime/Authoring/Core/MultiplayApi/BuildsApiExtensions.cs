using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Multiplay.Authoring.Core.Assets;
using Unity.Services.Multiplay.Authoring.Core.Builds;

namespace Unity.Services.Multiplay.Authoring.Core.MultiplayApi
{
    static class BuildsApiExtensions
    {
        public static async Task<(BuildId, CloudBucketId)> FindOrCreate(this IBuildsApi api, string name, MultiplayConfig.BuildDefinition definition, CancellationToken cancellationToken = default)
        {
            var existing = await api.FindByName(name, cancellationToken);
            if (existing != null)
            {
                return existing.Value;
            }

            return await api.Create(name, definition, cancellationToken);
        }

        public static async Task<bool> WaitForSync(
            this IBuildsApi api,
            BuildId build,
            Action<float> onProgress = null,
            CancellationToken cancellationToken = default,
            ITaskDelay delayer = null)
        {
            const int maxIterations = 40;
            const int baseWaitTimeMilliseconds = 500;

            // cant use ??= due to the unity formatter
            delayer = delayer ?? new DefaultTaskDelay();

            var iterations = 1;

            while (!await api.IsSynced(build, cancellationToken))
            {
                var waitTime = baseWaitTimeMilliseconds * iterations;
                if (++iterations >= maxIterations)
                {
                    return false;
                }

                await delayer.Delay(waitTime, cancellationToken);
                onProgress?.Invoke(100f * iterations / maxIterations);
            }

            onProgress?.Invoke(100f);
            return true;
        }
    }
}
