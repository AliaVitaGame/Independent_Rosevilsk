using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Shared.Providers
{
    public class SettingsProvider : MonoBehaviour, ISettingsProvider
    {
        private Lazy<Dictionary<Type, object>> _lazyRegisteredProviders;
        private Dictionary<Type, MonoBehaviour[]> _cachedProviders = new();

        private void Awake()
        {
            _lazyRegisteredProviders = new Lazy<Dictionary<Type, object>>(RegisterProviders);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            CacheProviders();
        }
#endif

        public T GetSettings<T>() where T : class
        {
            var type = typeof(T);

            _lazyRegisteredProviders ??= new Lazy<Dictionary<Type, object>>(RegisterProviders);

            if (_lazyRegisteredProviders.Value.TryGetValue(type, out var provider))
            {
                return provider as T;
            }

            Debug.LogWarning($"Attempting to dynamically register provider for type {type.Name}.", this);
            provider = TryRegisterProvider(type);

            if (provider != null)
            {
                return provider as T;
            }

            throw new KeyNotFoundException($"No provider registered or found for type {type}");
        }

        private Dictionary<Type, object> RegisterProviders()
        {
            var dictionary = new Dictionary<Type, object>();
            CacheProviders();
            var providers = GetComponentsInChildren<MonoBehaviour>(true)
                .Where(mb => mb.GetType().GetInterfaces().Any());

            foreach (var provider in providers)
            {
                if (provider == null) continue;

                foreach (var interfaceType in provider.GetType().GetInterfaces())
                {
                    if (dictionary.TryAdd(interfaceType, provider))
                    {
                        Debug.Log($"Registered {interfaceType.Name} from {provider.GetType().Name}", this);
                    }
                }
            }

            return dictionary;
        }

        private object TryRegisterProvider(Type type)
        {
            if (!_cachedProviders.TryGetValue(type, out var candidates))
            {
                Debug.LogWarning($"No cached candidates found for type {type.Name}", this);
                return null;
            }

            foreach (var candidate in candidates)
            {
                if (candidate == null) continue;

                if (!type.IsInstanceOfType(candidate)) continue;

                if (_lazyRegisteredProviders.Value.TryAdd(type, candidate))
                {
                    Debug.Log($"Dynamically registered {type.Name} from {candidate.GetType().Name}", this);
                    return candidate;
                }

                Debug.LogWarning($"Provider for type {type.Name} is already registered.", this);
                return candidate;
            }

            Debug.LogWarning($"No matching provider found for type {type.Name}", this);
            return null;
        }

        private void CacheProviders()
        {
            _cachedProviders ??= new Dictionary<Type, MonoBehaviour[]>();
            _cachedProviders.Clear();
            var allComponents = GetComponentsInChildren<MonoBehaviour>(true);

            foreach (var component in allComponents)
            {
                if (component == null) continue;

                foreach (var interfaceType in component.GetType().GetInterfaces())
                {
                    if (!_cachedProviders.ContainsKey(interfaceType))
                    {
                        _cachedProviders[interfaceType] = new List<MonoBehaviour>().ToArray();
                    }

                    _cachedProviders[interfaceType] = _cachedProviders[interfaceType].Append(component).ToArray();
                }
            }
        }
    }
}
