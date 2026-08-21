using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Bootstrap.SceneManagement
{
    public interface ISceneLoader
    {
        UniTask LoadAsync(SceneId sceneId, CancellationToken cancellation = default);
    }
}
