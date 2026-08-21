using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Multiplay.Authoring.Core.Assets;
using Unity.Services.Multiplay.Authoring.Core.Model;
using Unity.Services.Multiplay.Authoring.Editor.Assets;
using UnityEditor;
using UnityEngine;

namespace Unity.Services.Multiplay.Authoring.Editor.Deployment
{
    class AssetPersistenceStore : ScriptableObject, ISerializationCallbackReceiver, IItemStore
    {
        [NonSerialized]
        Dictionary<(GUID, IResourceName), object> m_StoredObjects = new();
        List<SerializedReference> m_References = new();

        // ScriptableSingleton can not be unit tested as you cant make instances.
        // This pattern lets us have a global instance while being able to make test copies.
        public static AssetPersistenceStore Instance
        {
            get
            {
                if (InstanceHolder.instance.store == null)
                {
                    InstanceHolder.instance.store = CreateInstance<AssetPersistenceStore>();
                }
                return InstanceHolder.instance.store;
            }
        }

        public BuildItem GetOrCreate(MultiplayConfigAsset config, BuildName resourceName)
        {
            return GetOrCreate<BuildItem>(config, resourceName);
        }

        public BuildConfigurationItem GetOrCreate(MultiplayConfigAsset config, BuildConfigurationName resourceName)
        {
            return GetOrCreate<BuildConfigurationItem>(config, resourceName);
        }

        public FleetItem GetOrCreate(MultiplayConfigAsset config, FleetName resourceName)
        {
            return GetOrCreate<FleetItem>(config, resourceName);
        }

        public IEnumerable<DeploymentItem> ItemsForConfig(MultiplayConfigAsset config)
        {
            var guid = AssetDatabase.GUIDFromAssetPath(config.Path);
            return m_StoredObjects
                .Where(kv => kv.Key.Item1 == guid)
                .Select(kv => kv.Value)
                .Cast<DeploymentItem>();
        }

        public IReadOnlyList<DeploymentItem> GetAllItems()
        {
            return m_StoredObjects
                .Select(kv => kv.Value)
                .Cast<DeploymentItem>()
                .ToList();
        }

        public void RemoveConfig(MultiplayConfigAsset config)
        {
            var guid = AssetDatabase.GUIDFromAssetPath(config.Path);
            var toRemove = m_StoredObjects.Keys.Where(k => k.Item1 == guid).ToList();
            foreach (var key in toRemove)
            {
                m_StoredObjects.Remove(key);
            }
        }

        public void OnBeforeSerialize()
        {
            m_References.Clear();

            foreach (var((guid, resourceName), value) in m_StoredObjects)
            {
                var r = new SerializedReference();

                r.configGuid = guid.ToString();
                r.name = resourceName.ToString();

                switch (value)
                {
                    case BuildItem buildItem:
                        r.kind = ReferenceKind.BuildItem;
                        r.buildItem = buildItem;
                        break;
                    case BuildConfigurationItem configItem:
                        r.kind = ReferenceKind.BuildConfigurationItem;
                        r.buildConfigurationItem = configItem;
                        break;
                    case FleetItem fleetItem:
                        r.kind = ReferenceKind.FleetItem;
                        r.fleetItem = fleetItem;
                        break;
                }

                m_References.Add(r);
            }
        }

        public void OnAfterDeserialize()
        {
            m_StoredObjects.Clear();

            foreach (var reference in m_References)
            {
                if (GUID.TryParse(reference.configGuid, out var assetGuid))
                {
                    switch (reference.kind)
                    {
                        case ReferenceKind.BuildItem:
                            m_StoredObjects.Add((assetGuid, new BuildName(){ Name = reference.name }), reference.buildItem);
                            break;
                        case ReferenceKind.BuildConfigurationItem:
                            m_StoredObjects.Add((assetGuid, new BuildConfigurationName(){ Name = reference.name }), reference.buildConfigurationItem);
                            break;
                        case ReferenceKind.FleetItem:
                            m_StoredObjects.Add((assetGuid, new FleetName(){ Name = reference.name }), reference.fleetItem);
                            break;
                    }
                }
            }
        }

        T GetOrCreate<T>(MultiplayConfigAsset config, IResourceName resourceName) where T : new()
        {
            var guid = AssetDatabase.GUIDFromAssetPath(config.Path);
            var key = (guid, resourceName);
            if (!m_StoredObjects.ContainsKey(key))
            {
                m_StoredObjects[key] = new T();
            }
            return (T)m_StoredObjects[key];
        }

        class InstanceHolder : ScriptableSingleton<InstanceHolder>
        {
            public AssetPersistenceStore store;
        }


        [Serializable]
        struct SerializedReference
        {
            public string configGuid;
            public string name;

            public ReferenceKind kind;

            public BuildItem buildItem;
            public BuildConfigurationItem buildConfigurationItem;
            public FleetItem fleetItem;
        }

        [Serializable]
        enum ReferenceKind
        {
            BuildItem,
            BuildConfigurationItem,
            FleetItem
        }
    }
}
