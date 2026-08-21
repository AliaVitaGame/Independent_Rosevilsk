using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PoolSystem.Alternative
{
    public class PoolService : IPoolService
    {
        protected Dictionary<Type, object> Pools = new();
        protected Transform ContainersHolder;
        protected IPoolMonoFactory Factory;

        public PoolService(string containerName, IPoolMonoFactory factory)
        {
            ContainersHolder = new GameObject(containerName).transform;
            Factory = factory ?? new PoolMonoFactory();
        }

        public IPool<T> GetPool<T>(T poolObject) where T : MonoBehaviour
        {
            if (!Pools.TryGetValue(typeof(T), out var pool)) return null;
            var typedPool = pool as List<IPool<T>>;
            return typedPool?.Find(p => p.Prefab == poolObject);
        }

        public bool HasPool<T>(T poolObject) where T : MonoBehaviour
        {
            if (!Pools.TryGetValue(typeof(T), out var pool)) return false;
            var typedPool = pool as List<IPool<T>>;
            return typedPool?.Any(poolMono => poolMono.Prefab == poolObject) ?? false;
        }

        public virtual IPool<T> RegisterPool<T>(T prefab, int count, Transform container = null, bool autoExpand = true) where T : MonoBehaviour
        {
            if (prefab == null)
                throw new Exception("Prefab for pool cannot be null");

            container = CreateContainerIfNullAndReturnParent(container, prefab.name);
            var pool = Factory.Create(prefab, count, container, autoExpand);

            if (!Pools.TryGetValue(typeof(T), out var poolList))
            {
                poolList = new List<IPool<T>>();
                Pools[typeof(T)] = poolList;
            }

            var typedPoolList = poolList as List<IPool<T>>;
            typedPoolList?.Add(pool);

            return pool;
        }

        public IPool<T> GetOrRegisterPool<T>(T prefab, int count, Transform container = null, bool autoExpand = true) where T : MonoBehaviour
        {
            return HasPool(prefab) ? GetPool(prefab) : RegisterPool(prefab, count, container, autoExpand);
        }

        protected Transform CreateContainerIfNullAndReturnParent(Transform container, string containerName)
        {
            if (container == null)
                container = new GameObject(containerName).transform;
            container.parent = ContainersHolder;
            return container;
        }
    }
}
