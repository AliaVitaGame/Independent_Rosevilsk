using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Meta.Lifecycle
{
    public interface IServiceInitializer
    {
        UniTask InitializeAsync(CancellationToken cancellation = default);
    }
}
