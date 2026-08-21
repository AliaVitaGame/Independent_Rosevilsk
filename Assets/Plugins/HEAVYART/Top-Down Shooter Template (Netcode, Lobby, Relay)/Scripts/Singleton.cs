using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        protected Singleton() { }
        private static T instance;

        public static T Instance
        {
            get
            {
                if (instance == null)
                    instance = FindFirstObjectByType<T>();

                return instance;
            }
        }

    }
}
