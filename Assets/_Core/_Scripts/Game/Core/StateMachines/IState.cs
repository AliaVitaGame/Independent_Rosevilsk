using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Core.StateMachines
{
    public interface IState : IExitableState
    {
        UniTask EnterAsync(CancellationToken cancellation = default);
    }
}
