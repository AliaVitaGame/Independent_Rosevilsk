using Cysharp.Threading.Tasks;

namespace Shared.UI.Loading
{
    public interface ILoadingPanelService
    {
        UniTask ShowAsync();
        UniTask HideAsync();
        void SetProgress(float value);
    }
}
