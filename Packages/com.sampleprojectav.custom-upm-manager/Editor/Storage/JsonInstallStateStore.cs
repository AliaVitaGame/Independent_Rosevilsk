using System.IO;
using UnityEngine;

namespace SampleProjectAV.CustomUpmManager.Editor
{
    public sealed class JsonInstallStateStore : IInstallStateStore
    {
        private readonly string path;

        public JsonInstallStateStore(string path)
        {
            this.path = path;
        }

        public CustomUpmInstallStateDatabase Load()
        {
            if (!File.Exists(path))
                return new CustomUpmInstallStateDatabase();

            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
                return new CustomUpmInstallStateDatabase();

            var database = JsonUtility.FromJson<CustomUpmInstallStateDatabase>(json) ?? new CustomUpmInstallStateDatabase();
            database.EnsureLists();
            return database;
        }

        public void Save(CustomUpmInstallStateDatabase database)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            database = database ?? new CustomUpmInstallStateDatabase();
            database.EnsureLists();
            File.WriteAllText(path, JsonUtility.ToJson(database, true));
        }
    }
}
