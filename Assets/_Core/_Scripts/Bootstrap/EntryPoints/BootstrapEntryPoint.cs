using System;
using System.Threading;
using Game.Core.StateMachines;
using Game.Meta.Lifecycle;
using Shared.UI.Loading;
using UnityEngine;
using VContainer.Unity;

namespace Game.Shared
{
    public class BootstrapEntryPoint : IAsyncStartable, IDisposable
    {
        private readonly IServiceInitializer _serviceInitializer;
        private readonly IStateMachine _stateMachine;
        private readonly ILoadingPanelService _loadingPanel;

        public BootstrapEntryPoint(
            IServiceInitializer serviceInitializer,
            IStateMachine stateMachine,
            ILoadingPanelService loadingPanel)
        {
            _serviceInitializer = serviceInitializer;
            _stateMachine = stateMachine;
            _loadingPanel = loadingPanel;
        }

        public async Awaitable StartAsync(CancellationToken cancellation = default)
        {
            _loadingPanel.SetProgress(0f);
            await _loadingPanel.ShowAsync();
            await _serviceInitializer.InitializeAsync(cancellation);
            await _stateMachine.EnterAsync<BootstrapState>(cancellation);
            await _stateMachine.EnterAsync<MenuState>(cancellation);
        }

        public void Dispose() { }
    }
}
