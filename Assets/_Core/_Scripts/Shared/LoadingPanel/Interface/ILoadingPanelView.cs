using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Shared.UI.Loading
{
    public interface ILoadingPanelView
    {
        GameObject LoadingPanel { get; }
        Image GetLoadingImage();
        
        UniTask Show();
        UniTask Hide();
        void SetProgress(float progress);
    }
}
