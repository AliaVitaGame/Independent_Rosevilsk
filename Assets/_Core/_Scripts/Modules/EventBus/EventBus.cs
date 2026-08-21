using System;
using System.Collections.Generic;

namespace OnlyFun.EventBus
{
    public class EventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _subscribers = new();

        public void Subscribe<T>(Action<T> callback)
        {
            var type = typeof(T);
            if (!_subscribers.ContainsKey(type))
                _subscribers[type] = new List<Delegate>();

            _subscribers[type].Add(callback);
        }

        public void Unsubscribe<T>(Action<T> callback)
        {
            var type = typeof(T);
            if (_subscribers.ContainsKey(type))
                _subscribers[type].Remove(callback);
        }

        public void Publish<T>(T evt)
        {
            var type = typeof(T);
            if (!_subscribers.ContainsKey(type)) return;

            var list = _subscribers[type].ToArray();
            foreach (var callback in list)
            {
                ((Action<T>)callback)?.Invoke(evt);
            }
        }
    }
}
