using System;
using System.Collections.Generic;
using R3;

namespace Game.Systems.FSM
{
    public class Fsm
    {
        public ReadOnlyReactiveProperty<IState> CurrentState => CurrentStateInternal;

        protected readonly ReactiveProperty<IState> CurrentStateInternal;
        protected readonly Dictionary<Type, IState> StatesInternal = new();

        public Fsm()
        {
            CurrentStateInternal = new ReactiveProperty<IState>();
        }

        public void AddState(IState state)
        {
            StatesInternal.Add(state.GetType(), state);
        }

        public void SetState<T>() where T : IState
        {
            var type = typeof(T);

            if (CurrentStateInternal.Value != null && CurrentStateInternal.Value.GetType() == type)
                return;
                
            if (StatesInternal.TryGetValue(type, out var newState))
                ChangeState(newState);
        }

        public void Update()
        {
            CurrentStateInternal.Value?.Update();
        }

        protected virtual void ChangeState(IState newState)
        {
            CurrentStateInternal.Value?.Exit();
            CurrentStateInternal.Value = newState;
            CurrentStateInternal.Value.Enter();
        }
    }
}
