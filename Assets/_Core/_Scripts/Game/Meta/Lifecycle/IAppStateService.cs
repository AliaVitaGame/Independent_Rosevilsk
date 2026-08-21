using System;
using R3;

namespace Game.Meta.Lifecycle
{
    public interface IAppStateService
    {
        ReadOnlyReactiveProperty<ApplicationLifecycleState> CurrentState { get; }
        event Action Paused;
        event Action Resumed;
        event Action Quitting;
    }
}
