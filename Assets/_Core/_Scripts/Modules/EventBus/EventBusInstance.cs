using UnityEngine;

namespace OnlyFun.EventBus
{
    public class EventBusInstance : MonoBehaviour
    {
        public static EventBus EventBus { get; private set; }

        private void Awake()
        {
            if (EventBus == null)
            {
                EventBus = new EventBus();
                return;
            }
            Destroy(gameObject);
        }
    }
}