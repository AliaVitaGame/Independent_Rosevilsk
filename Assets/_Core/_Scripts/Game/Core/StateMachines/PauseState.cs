using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Core.StateMachines
{
    public sealed class PauseState : IState
    {
        public UniTask EnterAsync(CancellationToken cancellation = default)
        {
            cancellation.ThrowIfCancellationRequested();
            return UniTask.CompletedTask;
        }

        public void Exit() { }
    }
}
