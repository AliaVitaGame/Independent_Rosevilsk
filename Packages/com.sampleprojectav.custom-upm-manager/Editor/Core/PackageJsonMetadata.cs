using System;
using UnityEngine;

namespace SampleProjectAV.CustomUpmManager.Editor
{
    public sealed class PackageJsonMetadata
    {
        public static readonly PackageJsonMetadata Empty = new PackageJsonMetadata(null, null, null);

        public PackageJsonMetadata(string displayName, string packageName, string version)
        {
            DisplayName = displayName;
            PackageName = packageName;
            Version = version;
        }

        public string DisplayName { get; }
        public string PackageName { get; }
        public string Version { get; }

        public bool HasAnyValue =>
            !string.IsNullOrWhiteSpace(DisplayName)
            || !string.IsNullOrWhiteSpace(PackageName)
            || !string.IsNullOrWhiteSpace(Version);
    }

    public static class PackageJsonMetadataParser
    {
        public static PackageJsonMetadata Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return PackageJsonMetadata.Empty;

            var dto = JsonUtility.FromJson<PackageJsonDto>(json);
            if (dto == null)
                return PackageJsonMetadata.Empty;

            var packageName = TrimToNull(dto.name);
            var displayName = TrimToNull(dto.displayName) ?? packageName;
            var version = TrimToNull(dto.version);

            return new PackageJsonMetadata(displayName, packageName, version);
        }

        private static string TrimToNull(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value.Trim();
        }

        [Serializable]
        private sealed class PackageJsonDto
        {
            public string name;
            public string displayName;
            public string version;
        }
    }
}
