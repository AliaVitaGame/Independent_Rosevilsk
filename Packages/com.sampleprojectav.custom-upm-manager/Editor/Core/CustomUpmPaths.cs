using System.IO;

namespace SampleProjectAV.CustomUpmManager.Editor
{
    public static class CustomUpmPaths
    {
        public const string SettingsDirectory = "ProjectSettings/CustomUpmManager";
        public const string RegistryPath = SettingsDirectory + "/modules.json";
        public const string InstallStatePath = SettingsDirectory + "/install-state.json";
        public const string CacheRoot = "Library/CustomUpmManager";
        public const string DownloadCacheRoot = CacheRoot + "/Downloads";
        public const string GitCacheRoot = CacheRoot + "/Git";
        public const string PendingImportPath = CacheRoot + "/pending-unitypackage-import.json";

        public static void EnsureSettingsDirectory()
        {
            Directory.CreateDirectory(SettingsDirectory);
        }

        public static void EnsureCacheDirectories()
        {
            Directory.CreateDirectory(DownloadCacheRoot);
            Directory.CreateDirectory(GitCacheRoot);
        }
    }
}
