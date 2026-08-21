using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

namespace Game.Core.StateMachines
{
    public interface IStateMachine
    {
        ReadOnlyReactiveProperty<IExitableState> CurrentState { get; }
        UniTask EnterAsync<TState>(CancellationToken cancellation = default) where TState : class, IState;
        UniTask EnterAsync<TState, TPayload>(TPayload payload, CancellationToken cancellation = default)
            where TState : class, IPayloadedState<TPayload>;
    }
}
