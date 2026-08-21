using UnityEngine;

namespace PoolSystem.Alternative
{
    public interface IPoolService
    {
        IPool<T> GetPool<T>(T poolObject) where T : MonoBehaviour;
        bool HasPool<T>(T poolObject) where T : MonoBehaviour;
        IPool<T> RegisterPool<T>(T prefab, int count, Transform container = null, bool autoExpand = true) where T : MonoBehaviour;
        IPool<T> GetOrRegisterPool<T>(T prefab, int count, Transform container = null, bool autoExpand = true) where T : MonoBehaviour;
    }
}
