using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Meta.Lifecycle
{
    public sealed class ServiceInitializer : IServiceInitializer
    {
        private const int MockInitializationDelayMilliseconds = 500;

        private readonly IEnumerable<IServicePreloader> _preloaders;

        public ServiceInitializer(IEnumerable<IServicePreloader> preloaders)
        {
            _preloaders = preloaders;
        }

        public async UniTask InitializeAsync(CancellationToken cancellation = default)
        {
            await UniTask.Delay(MockInitializationDelayMilliseconds, cancellationToken: cancellation);

            foreach (var preloader in _preloaders)
            {
                cancellation.ThrowIfCancellationRequested();
                await preloader.WarmUp();
            }
        }
    }
}
