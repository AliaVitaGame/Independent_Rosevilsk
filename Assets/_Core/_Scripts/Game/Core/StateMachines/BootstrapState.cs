using System.Threading;
using Cysharp.Threading.Tasks;
using PrimeTween;
using Shared.UI.Loading;
using UnityEngine;

namespace Game.Core.StateMachines
{
    public sealed class BootstrapState : IState
    {
        private const float MinimumLoadingDuration = 1f;
        private const float MaximumLoadingDuration = 3f;

        private readonly ILoadingPanelService _loadingPanel;

        public BootstrapState(ILoadingPanelService loadingPanel)
        {
            _loadingPanel = loadingPanel;
        }

        public async UniTask EnterAsync(CancellationToken cancellation = default)
        {
            cancellation.ThrowIfCancellationRequested();

            var duration = Random.Range(MinimumLoadingDuration, MaximumLoadingDuration);
            var tween = Tween.Custom(
                _loadingPanel,
                startValue: 0f,
                endValue: 1f,
                duration: duration,
                ease: Ease.InOutSine,
                onValueChange: static (loadingPanel, progress) => loadingPanel.SetProgress(progress));

            try
            {
                while (tween.isAlive)
                {
                    cancellation.ThrowIfCancellationRequested();
                    await UniTask.Yield();
                }
            }
            catch
            {
                tween.Stop();
                throw;
            }
        }

        public void Exit() { }
    }
}
