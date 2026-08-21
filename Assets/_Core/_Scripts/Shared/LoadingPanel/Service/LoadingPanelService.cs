using Cysharp.Threading.Tasks;

namespace Shared.UI.Loading
{
    public class LoadingPanelService : ILoadingPanelService
    {
        private readonly ILoadingPanelView _loadingPanelView;
        private bool _isVisible;

        public LoadingPanelService(ILoadingPanelView loadingPanelView)
        {
            _loadingPanelView = loadingPanelView;
        }

        public async UniTask ShowAsync()
        {
            if (_isVisible)
                return;

            await _loadingPanelView.Show();
            _isVisible = true;
        }

        public async UniTask HideAsync()
        {
            if (!_isVisible)
                return;

            await _loadingPanelView.Hide();
            _isVisible = false;
        }

        public void SetProgress(float value)
        {
            _loadingPanelView.SetProgress(value);
        }
    }
}
