using Cysharp.Threading.Tasks;

namespace Game.Meta.Lifecycle
{
    public interface IServicePreloader
    {
        UniTask WarmUp();
    }
}
