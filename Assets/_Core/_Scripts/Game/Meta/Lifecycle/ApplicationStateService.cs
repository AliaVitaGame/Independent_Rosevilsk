using R3;
using UnityEngine;

namespace Game.Meta.Lifecycle
{
    public sealed class ApplicationStateService : MonoBehaviour, IAppStateService
    {
        public ReadOnlyReactiveProperty<ApplicationLifecycleState> CurrentState => _currentState;

        private readonly ReactiveProperty<ApplicationLifecycleState> _currentState = new(ApplicationLifecycleState.Running);

        public event System.Action Paused;
        public event System.Action Resumed;
        public event System.Action Quitting;

        private void OnApplicationPause(bool isPaused)
        {
            _currentState.Value = isPaused ? ApplicationLifecycleState.Paused : ApplicationLifecycleState.Running;

            if (isPaused)
                Paused?.Invoke();
            else
                Resumed?.Invoke();
        }

        private void OnApplicationQuit()
        {
            _currentState.Value = ApplicationLifecycleState.Quitting;
            Quitting?.Invoke();
        }
    }
}
