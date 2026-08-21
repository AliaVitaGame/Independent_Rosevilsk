using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Core.StateMachines
{
    public interface IPayloadedState<in TPayload> : IExitableState
    {
        UniTask EnterAsync(TPayload payload, CancellationToken cancellation = default);
    }
}
