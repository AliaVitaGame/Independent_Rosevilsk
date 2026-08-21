using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Bootstrap.SceneManagement;
using Game.Meta.Analytics;

namespace Game.Core.StateMachines
{
    public sealed class MenuState : IState
    {
        private readonly ISceneLoader _sceneLoader;
        private readonly IAnalyticsService _analyticsService;

        public MenuState(ISceneLoader sceneLoader, IAnalyticsService analyticsService)
        {
            _sceneLoader = sceneLoader;
            _analyticsService = analyticsService;
        }

        public async UniTask EnterAsync(CancellationToken cancellation = default)
        {
            await _sceneLoader.LoadAsync(SceneId.Menu, cancellation);
            _analyticsService.Track(AnalyticsEventType.SceneOpened, SceneId.Menu.ToString());
        }

        public void Exit() { }
    }
}
