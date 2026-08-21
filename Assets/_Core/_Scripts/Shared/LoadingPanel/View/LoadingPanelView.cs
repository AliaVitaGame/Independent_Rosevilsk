using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Shared.UI.Loading
{
    public class LoadingPanelView : MonoBehaviour, ILoadingPanelView
    {
        [SerializeField] private Image loadingImage;
        [SerializeField] private GameObject loadingPanel;

        public GameObject LoadingPanel => loadingPanel;
        public Image GetLoadingImage() => loadingImage;

        private void Awake()
        {
            loadingPanel.SetActive(false);
        }

        public UniTask Show()
        {
            loadingPanel.SetActive(true);
            return UniTask.CompletedTask;
        }

        public UniTask Hide()
        {
            loadingPanel.SetActive(false);
            return UniTask.CompletedTask;
        }

        public void SetProgress(float progress)
        {
            loadingImage.fillAmount = Mathf.Clamp01(progress);
        }
    }
}
