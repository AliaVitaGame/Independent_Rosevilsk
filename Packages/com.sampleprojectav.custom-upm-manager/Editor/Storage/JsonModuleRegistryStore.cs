using System.IO;
using UnityEngine;

namespace SampleProjectAV.CustomUpmManager.Editor
{
    public sealed class JsonModuleRegistryStore : IModuleRegistryStore
    {
        private readonly string path;

        public JsonModuleRegistryStore(string path)
        {
            this.path = path;
        }

        public CustomUpmRegistry Load()
        {
            if (!File.Exists(path))
                return new CustomUpmRegistry();

            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
                return new CustomUpmRegistry();

            var registry = JsonUtility.FromJson<CustomUpmRegistry>(json) ?? new CustomUpmRegistry();
            registry.EnsureLists();
            return registry;
        }

        public void Save(CustomUpmRegistry registry)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            registry = registry ?? new CustomUpmRegistry();
            registry.EnsureLists();
            File.WriteAllText(path, JsonUtility.ToJson(registry, true));
        }
    }
}
