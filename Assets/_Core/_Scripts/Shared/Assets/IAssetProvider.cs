using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Shared.Assets
{
    public interface IAssetProvider
    {
        UniTask<T> LoadAsync<T>(string address) where T : Object;
    }
}
