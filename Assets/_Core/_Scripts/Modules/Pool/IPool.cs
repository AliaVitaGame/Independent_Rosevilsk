using UnityEngine;

namespace PoolSystem.Alternative
{
    public interface IPool<T> where T : MonoBehaviour
    {
        public T Prefab { get; }
        public bool AutoExpand { get; set; }
        public Transform Container { get; }
        
        public T CreateObject(bool isActivateByDefault = false);
        public bool HasFreeElement(out T element, bool activeInHierarchy = true);
        public T GetFreeElement(bool activeInHierarchy = true);
        public T GetFreeElement(Vector3 position, Quaternion rotation, bool activeInHierarchy = true);
    }
}