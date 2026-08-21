using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Bootstrap.SceneManagement;
using Game.Meta.Analytics;

namespace Game.Core.StateMachines
{
    public sealed class LoadSceneState : IPayloadedState<SceneId>
    {
        private readonly ISceneLoader _sceneLoader;
        private readonly IAnalyticsService _analyticsService;

        public LoadSceneState(ISceneLoader sceneLoader, IAnalyticsService analyticsService)
        {
            _sceneLoader = sceneLoader;
            _analyticsService = analyticsService;
        }

        public async UniTask EnterAsync(SceneId sceneId, CancellationToken cancellation = default)
        {
            await _sceneLoader.LoadAsync(sceneId, cancellation);
            _analyticsService.Track(AnalyticsEventType.SceneOpened, sceneId.ToString());
        }

        public void Exit() { }
    }
}
