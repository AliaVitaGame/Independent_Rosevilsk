using System.Threading;
using Cysharp.Threading.Tasks;
using Shared.UI.Loading;
using UnityEngine.SceneManagement;

namespace Game.Bootstrap.SceneManagement
{
    public class SceneLoaderService : ISceneLoader
    {
        private readonly ISceneCatalog _sceneCatalog;
        private readonly ILoadingPanelService _loadingPanel;

        public SceneLoaderService(
            ISceneCatalog sceneCatalog,
            ILoadingPanelService loadingPanel)
        {
            _sceneCatalog = sceneCatalog;
            _loadingPanel = loadingPanel;
        }

        public async UniTask LoadAsync(SceneId sceneId, CancellationToken cancellation = default)
        {
            _loadingPanel.SetProgress(0f);
            await _loadingPanel.ShowAsync();

            try
            {
                cancellation.ThrowIfCancellationRequested();

                var operation = SceneManager.LoadSceneAsync(_sceneCatalog.GetSceneName(sceneId));
                while (!operation.isDone)
                {
                    _loadingPanel.SetProgress(operation.progress / 0.9f);
                    await UniTask.Yield();
                }

                _loadingPanel.SetProgress(1f);
            }
            finally
            {
                await _loadingPanel.HideAsync();
            }
        }
    }
}
