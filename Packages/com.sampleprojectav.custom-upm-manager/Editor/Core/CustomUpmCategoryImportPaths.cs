using System;
using System.Collections.Generic;
using System.Linq;

namespace SampleProjectAV.CustomUpmManager.Editor
{
    public static class CustomUpmCategoryImportPaths
    {
        public const string AssetsRoot = "Assets";
        public const string VfxFolder = "Assets/_Core/_Content/VFX";
        public const string ModelsFolder = "Assets/_Core/_Content/Models";
        public const string UiFolder = "Assets/_Core/_Content/Arts/UI";

        public static bool TryGetImportFolder(string category, out string folder)
        {
            switch ((category ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "vfx":
                    folder = VfxFolder;
                    return true;
                case "models":
                    folder = ModelsFolder;
                    return true;
                case "ui":
                    folder = UiFolder;
                    return true;
                default:
                    folder = null;
                    return false;
            }
        }

        public static string RemapAssetPath(string assetPath, string destinationFolder)
        {
            var normalized = NormalizeAssetPath(assetPath);
            var destination = NormalizeAssetPath(destinationFolder);
            if (string.IsNullOrEmpty(normalized) || string.IsNullOrEmpty(destination))
                return normalized;

            if (IsUnderFolder(normalized, destination))
                return normalized;

            if (!normalized.StartsWith(AssetsRoot + "/", StringComparison.OrdinalIgnoreCase))
                return normalized;

            return destination + normalized.Substring(AssetsRoot.Length);
        }

        public static IReadOnlyList<string> GetMoveRoots(IEnumerable<string> importedPaths)
        {
            var imported = new HashSet<string>(
                (importedPaths ?? Array.Empty<string>())
                    .Select(NormalizeAssetPath)
                    .Where(path => !string.IsNullOrEmpty(path)),
                StringComparer.OrdinalIgnoreCase);

            return imported
                .Where(path =>
                {
                    var parent = GetParentPath(path);
                    return string.IsNullOrEmpty(parent)
                           || string.Equals(parent, AssetsRoot, StringComparison.OrdinalIgnoreCase)
                           || !imported.Contains(parent);
                })
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static IReadOnlyList<AssetMove> BuildMoves(IEnumerable<string> importedPaths, string destinationFolder)
        {
            var destination = NormalizeAssetPath(destinationFolder);
            if (string.IsNullOrEmpty(destination))
                return Array.Empty<AssetMove>();

            return GetMoveRoots(importedPaths)
                .Select(root => new AssetMove(root, RemapAssetPath(root, destination)))
                .Where(move => !string.Equals(move.From, move.To, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public static IReadOnlyList<string> RemapImportedPaths(IEnumerable<string> importedPaths, string destinationFolder)
        {
            var destination = NormalizeAssetPath(destinationFolder);
            return (importedPaths ?? Array.Empty<string>())
                .Select(path => RemapAssetPath(path, destination))
                .Where(path => !string.IsNullOrEmpty(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static string GetParentPath(string assetPath)
        {
            var normalized = NormalizeAssetPath(assetPath);
            var index = normalized.LastIndexOf('/');
            return index <= 0 ? string.Empty : normalized.Substring(0, index);
        }

        public static string NormalizeAssetPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return string.Empty;

            return assetPath.Trim().Replace('\\', '/').TrimEnd('/');
        }

        private static bool IsUnderFolder(string assetPath, string folder)
        {
            return string.Equals(assetPath, folder, StringComparison.OrdinalIgnoreCase)
                   || assetPath.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase);
        }

        public readonly struct AssetMove
        {
            public AssetMove(string from, string to)
            {
                From = from;
                To = to;
            }

            public string From { get; }
            public string To { get; }
        }
    }
}
