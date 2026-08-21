using System;
using System.IO;
using System.Text.RegularExpressions;

namespace SampleProjectAV.CustomUpmManager.Editor
{
    public static class CustomUpmModuleFactory
    {
        public static CustomUpmModule Create(
            string displayName,
            string sourceUrl,
            CustomUpmSourceKind sourceKind,
            string packageName,
            string documentationUrl,
            string defaultVersion,
            string unityPackagePath = null,
            string category = null)
        {
            var resolvedDisplayName = string.IsNullOrWhiteSpace(displayName)
                ? InferDisplayName(sourceUrl, unityPackagePath)
                : displayName.Trim();

            return new CustomUpmModule
            {
                Id = MakeId(resolvedDisplayName, sourceUrl, unityPackagePath),
                DisplayName = resolvedDisplayName,
                Category = category,
                SourceKind = sourceKind,
                SourceUrl = sourceUrl?.Trim(),
                PackageName = packageName?.Trim(),
                DocumentationUrl = string.IsNullOrWhiteSpace(documentationUrl) ? sourceUrl?.Trim() : documentationUrl.Trim(),
                DefaultVersion = string.IsNullOrWhiteSpace(defaultVersion) ? "1.0.0" : defaultVersion.Trim(),
                UnityPackagePath = unityPackagePath?.Trim()
            };
        }

        private static string InferDisplayName(string sourceUrl, string unityPackagePath)
        {
            var candidate = !string.IsNullOrWhiteSpace(unityPackagePath)
                ? Path.GetFileNameWithoutExtension(unityPackagePath)
                : ExtractFileNameFromSourceUrl(sourceUrl);

            if (string.IsNullOrWhiteSpace(candidate))
                candidate = "Custom Module";

            return candidate.Replace('-', ' ').Replace('_', ' ').Trim();
        }

        private static string ExtractFileNameFromSourceUrl(string sourceUrl)
        {
            var cleanUrl = GitUrlUtility.ExtractRepositoryUrl(sourceUrl);
            if (string.IsNullOrWhiteSpace(cleanUrl))
                return string.Empty;

            if (Uri.TryCreate(cleanUrl, UriKind.Absolute, out var uri))
                cleanUrl = Uri.UnescapeDataString(uri.AbsolutePath);

            var fileName = Path.GetFileName(cleanUrl.TrimEnd('/', '\\'));
            if (fileName.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
                fileName = fileName.Substring(0, fileName.Length - ".git".Length);

            return Path.GetFileNameWithoutExtension(fileName);
        }

        private static string MakeId(string displayName, string sourceUrl, string unityPackagePath)
        {
            var slug = Regex.Replace((displayName ?? "module").ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
            if (string.IsNullOrWhiteSpace(slug))
                slug = "module";

            var hash = StableHash.Sha1($"{sourceUrl}|{unityPackagePath}");
            return $"{slug}-{hash.Substring(0, 8)}";
        }
    }
}
