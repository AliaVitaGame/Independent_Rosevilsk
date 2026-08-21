using Cysharp.Threading.Tasks;
using Game.Meta.Lifecycle;

namespace Game.Meta.Analytics
{
    public sealed class AnalyticsLifecycleReporter : IServicePreloader
    {
        private readonly IAppStateService _appStateService;
        private readonly IAnalyticsService _analyticsService;
        private bool _isInitialized;

        public AnalyticsLifecycleReporter(
            IAppStateService appStateService,
            IAnalyticsService analyticsService)
        {
            _appStateService = appStateService;
            _analyticsService = analyticsService;
        }

        public UniTask WarmUp()
        {
            if (_isInitialized) return UniTask.CompletedTask;

            _isInitialized = true;
            _appStateService.Paused += OnPaused;
            _appStateService.Quitting += OnQuitting;
            _analyticsService.Track(AnalyticsEventType.AppStarted);
            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            _appStateService.Paused -= OnPaused;
            _appStateService.Quitting -= OnQuitting;
        }

        private void OnPaused() => _analyticsService.Track(AnalyticsEventType.AppPaused);
        private void OnQuitting() => _analyticsService.Track(AnalyticsEventType.AppQuit);
    }
}
