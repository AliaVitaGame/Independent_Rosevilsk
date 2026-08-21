using System;
using System.Collections.Generic;
using UnityEngine;

namespace SampleProjectAV.CustomUpmManager.Editor
{
    public enum CustomUpmSourceKind
    {
        GitUpmPackage = 0,
        UnityPackageUrl = 1,
        GitRepositoryUnityPackages = 2,
        AssetStoreUrl = 3
    }

    [Serializable]
    public sealed class CustomUpmVersion
    {
        [SerializeField] private string name;
        [SerializeField] private string revision;
        [SerializeField] private bool isTag;
        [SerializeField] private string sourceUrl;
        [SerializeField] private string unityPackagePath;

        public CustomUpmVersion()
        {
        }

        public CustomUpmVersion(string name, string revision)
        {
            Name = name;
            Revision = revision;
        }

        public string Name
        {
            get => name;
            set => name = value;
        }

        public string Revision
        {
            get => revision;
            set => revision = value;
        }

        public bool IsTag
        {
            get => isTag;
            set => isTag = value;
        }

        public string SourceUrl
        {
            get => sourceUrl;
            set => sourceUrl = value;
        }

        public string UnityPackagePath
        {
            get => unityPackagePath;
            set => unityPackagePath = value;
        }
    }

    [Serializable]
    public sealed class CustomUpmModule
    {
        public const string UncategorizedCategory = "Uncategorized";

        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private string category = UncategorizedCategory;
        [SerializeField] private CustomUpmSourceKind sourceKind;
        [SerializeField] private string sourceUrl;
        [SerializeField] private string packageName;
        [SerializeField] private string unityPackagePath;
        [SerializeField] private string documentationUrl;
        [SerializeField] private string defaultVersion = "1.0.0";
        [SerializeField] private List<CustomUpmVersion> versions = new List<CustomUpmVersion>();

        public string Id
        {
            get => id;
            set => id = value;
        }

        public string DisplayName
        {
            get => displayName;
            set => displayName = value;
        }

        public string Category
        {
            get => string.IsNullOrWhiteSpace(category) ? UncategorizedCategory : category;
            set => category = string.IsNullOrWhiteSpace(value) ? UncategorizedCategory : value.Trim();
        }

        public CustomUpmSourceKind SourceKind
        {
            get => sourceKind;
            set => sourceKind = value;
        }

        public string SourceUrl
        {
            get => sourceUrl;
            set => sourceUrl = value;
        }

        public string PackageName
        {
            get => packageName;
            set => packageName = value;
        }

        public string UnityPackagePath
        {
            get => unityPackagePath;
            set => unityPackagePath = value;
        }

        public string DocumentationUrl
        {
            get => documentationUrl;
            set => documentationUrl = value;
        }

        public string DefaultVersion
        {
            get => defaultVersion;
            set => defaultVersion = value;
        }

        public List<CustomUpmVersion> Versions
        {
            get
            {
                EnsureLists();
                return versions;
            }
        }

        public void SetVersions(IEnumerable<CustomUpmVersion> newVersions)
        {
            versions = newVersions == null
                ? new List<CustomUpmVersion>()
                : new List<CustomUpmVersion>(newVersions);
        }

        public void EnsureLists()
        {
            if (versions == null)
                versions = new List<CustomUpmVersion>();
        }
    }

    [Serializable]
    public sealed class CustomUpmRegistry
    {
        [SerializeField] private List<CustomUpmModule> modules = new List<CustomUpmModule>();

        public List<CustomUpmModule> Modules
        {
            get
            {
                EnsureLists();
                return modules;
            }
        }

        public void EnsureLists()
        {
            if (modules == null)
                modules = new List<CustomUpmModule>();

            foreach (var module in modules)
                module?.EnsureLists();
        }
    }

    [Serializable]
    public sealed class CustomUpmInstallState
    {
        [SerializeField] private string moduleId;
        [SerializeField] private string installedVersion;
        [SerializeField] private string installedRevision;
        [SerializeField] private string packageName;
        [SerializeField] private CustomUpmSourceKind sourceKind;
        [SerializeField] private List<string> importedAssetPaths = new List<string>();

        public string ModuleId
        {
            get => moduleId;
            set => moduleId = value;
        }

        public string InstalledVersion
        {
            get => installedVersion;
            set => installedVersion = value;
        }

        public string InstalledRevision
        {
            get => installedRevision;
            set => installedRevision = value;
        }

        public string PackageName
        {
            get => packageName;
            set => packageName = value;
        }

        public CustomUpmSourceKind SourceKind
        {
            get => sourceKind;
            set => sourceKind = value;
        }

        public List<string> ImportedAssetPaths
        {
            get
            {
                EnsureLists();
                return importedAssetPaths;
            }
        }

        public void SetImportedAssetPaths(IEnumerable<string> paths)
        {
            importedAssetPaths = paths == null ? new List<string>() : new List<string>(paths);
        }

        public void EnsureLists()
        {
            if (importedAssetPaths == null)
                importedAssetPaths = new List<string>();
        }
    }

    [Serializable]
    public sealed class CustomUpmInstallStateDatabase
    {
        [SerializeField] private List<CustomUpmInstallState> modules = new List<CustomUpmInstallState>();

        public List<CustomUpmInstallState> Modules
        {
            get
            {
                EnsureLists();
                return modules;
            }
        }

        public bool TryGet(string moduleId, out CustomUpmInstallState state)
        {
            EnsureLists();

            foreach (var module in modules)
            {
                if (module != null && string.Equals(module.ModuleId, moduleId, StringComparison.Ordinal))
                {
                    state = module;
                    return true;
                }
            }

            state = null;
            return false;
        }

        public void Upsert(CustomUpmInstallState state)
        {
            if (state == null || string.IsNullOrEmpty(state.ModuleId))
                return;

            EnsureLists();

            for (var i = 0; i < modules.Count; i++)
            {
                if (modules[i] != null && string.Equals(modules[i].ModuleId, state.ModuleId, StringComparison.Ordinal))
                {
                    modules[i] = state;
                    return;
                }
            }

            modules.Add(state);
        }

        public void Remove(string moduleId)
        {
            EnsureLists();
            modules.RemoveAll(module => module != null && string.Equals(module.ModuleId, moduleId, StringComparison.Ordinal));
        }

        public void EnsureLists()
        {
            if (modules == null)
                modules = new List<CustomUpmInstallState>();

            foreach (var module in modules)
                module?.EnsureLists();
        }
    }
}
