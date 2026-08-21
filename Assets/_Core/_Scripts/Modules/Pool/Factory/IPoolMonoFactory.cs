using UnityEngine;

namespace PoolSystem.Alternative
{
    public interface IPoolMonoFactory
    {
        IPool<T> Create<T>(T prefab, int count, Transform container = null, bool autoExpand = true) where T : MonoBehaviour;

    }   

    public class PoolMonoFactory : IPoolMonoFactory
    {
        public IPool<T> Create<T>(T prefab, int count, Transform container = null, bool autoExpand = true) where T : MonoBehaviour
        {
            return new PoolMono<T>(prefab, count, container, autoExpand);
        }
    }
}