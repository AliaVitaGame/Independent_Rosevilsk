using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

namespace Game.Core.StateMachines
{
    public sealed class StateMachine : IStateMachine
    {
        public ReadOnlyReactiveProperty<IExitableState> CurrentState => _currentState;

        private readonly Dictionary<Type, IExitableState> _states = new();
        private readonly ReactiveProperty<IExitableState> _currentState = new();

        public StateMachine(
            BootstrapState bootstrapState,
            MenuState menuState,
            LoadSceneState loadSceneState,
            PauseState pauseState,
            WinState winState,
            LoseState loseState)
        {
            AddState(bootstrapState);
            AddState(menuState);
            AddState(loadSceneState);
            AddState(pauseState);
            AddState(winState);
            AddState(loseState);
        }

        public async UniTask EnterAsync<TState>(CancellationToken cancellation = default) where TState : class, IState
        {
            var state = ChangeState<TState>();
            await state.EnterAsync(cancellation);
        }

        public async UniTask EnterAsync<TState, TPayload>(TPayload payload, CancellationToken cancellation = default)
            where TState : class, IPayloadedState<TPayload>
        {
            var state = ChangeState<TState>();
            await state.EnterAsync(payload, cancellation);
        }

        private void AddState(IExitableState state) => _states.Add(state.GetType(), state);

        private TState ChangeState<TState>() where TState : class, IExitableState
        {
            var state = (TState)_states[typeof(TState)];
            if (ReferenceEquals(_currentState.Value, state)) return state;

            _currentState.Value?.Exit();
            _currentState.Value = state;
            return state;
        }
    }
}
