using System;
using System.Collections.Generic;
using UnityEngine;

namespace PoolSystem.Alternative
{
    public class PoolMono<T> : IPool<T> where T : MonoBehaviour 
    {
        public T Prefab { get; }
        public bool AutoExpand { get; set; }
        public Transform Container { get; }
        
        protected List<T> Pool;

        public PoolMono(T prefab, int count, Transform container = null, bool autoExpand = true)
        {
            Prefab = prefab;
            Container = container;
            AutoExpand = autoExpand;
            CreatePool(count);
        }

        protected virtual void CreatePool(int count)
        {
            Pool = new List<T>();

            for (var i = 0; i < count; i++)
            {
                CreateObject();
            }
        }

        public virtual T CreateObject(bool isActivateByDefault = false)
        {
            var createdObjcet = UnityEngine.Object.Instantiate(Prefab, Container);
            createdObjcet.gameObject.SetActive(isActivateByDefault);
            Pool.Add(createdObjcet);
            return createdObjcet;
        }

        public virtual bool HasFreeElement(out T element, bool activeInHierarchy = true)
        {
            foreach (var mono in Pool)
            {
                if (!mono.gameObject.activeInHierarchy)
                {
                    element = mono;
                    mono.gameObject.SetActive(activeInHierarchy);
                    return true;
                }
            }
            element = null;
            return false;
        }

        public virtual T GetFreeElement(bool activeInHierarchy = true)
        {
            if (HasFreeElement(out var element, activeInHierarchy))
                return element;

            if (AutoExpand)
                return CreateObject(true);

            throw new Exception($"There is no free element of type <{typeof(T)}> in pool");
        }

        public virtual T GetFreeElement(Vector3 position, Quaternion rotation, bool activeInHierarchy = true)
        {
            var element = GetFreeElement(false);
            element.transform.SetPositionAndRotation(position, rotation);
            element.gameObject.SetActive(activeInHierarchy);
            return element;
        }
    }
}
