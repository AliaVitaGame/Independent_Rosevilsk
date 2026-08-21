using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SampleProjectAV.CustomUpmManager.Editor
{
    public sealed class ModuleMetadataProvider : IModuleMetadataProvider
    {
        private readonly IGitClient gitClient;

        public ModuleMetadataProvider(IGitClient gitClient)
        {
            this.gitClient = gitClient;
        }

        public async Task<PackageJsonMetadata> GetMetadataAsync(
            CustomUpmSourceKind sourceKind,
            string sourceUrl,
            string unityPackagePath,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sourceUrl))
                return PackageJsonMetadata.Empty;

            if (sourceKind == CustomUpmSourceKind.UnityPackageUrl)
                return FromFallbackName(InferDisplayName(sourceUrl, unityPackagePath));

            var metadata = await TryReadPackageJsonAsync(sourceUrl, cancellationToken);
            if (metadata.HasAnyValue)
                return metadata;

            return FromFallbackName(InferDisplayName(sourceUrl, unityPackagePath));
        }

        private async Task<PackageJsonMetadata> TryReadPackageJsonAsync(string sourceUrl, CancellationToken cancellationToken)
        {
            var repositoryUrl = GitUrlUtility.ExtractRepositoryUrl(sourceUrl);
            if (string.IsNullOrWhiteSpace(repositoryUrl))
                return PackageJsonMetadata.Empty;

            CustomUpmPaths.EnsureCacheDirectories();
            var repositoryPath = await gitClient.PrepareRepositoryAsync(repositoryUrl, string.Empty, CustomUpmPaths.GitCacheRoot, cancellationToken);
            var packagePath = GitUrlUtility.ExtractPackagePath(sourceUrl);
            var packageJsonPath = string.IsNullOrWhiteSpace(packagePath)
                ? Path.Combine(repositoryPath, "package.json")
                : Path.Combine(repositoryPath, packagePath.Replace('/', Path.DirectorySeparatorChar), "package.json");

            if (!File.Exists(packageJsonPath))
                return PackageJsonMetadata.Empty;

            return PackageJsonMetadataParser.Parse(File.ReadAllText(packageJsonPath));
        }

        private static PackageJsonMetadata FromFallbackName(string displayName)
        {
            return string.IsNullOrWhiteSpace(displayName)
                ? PackageJsonMetadata.Empty
                : new PackageJsonMetadata(displayName, null, null);
        }

        private static string InferDisplayName(string sourceUrl, string unityPackagePath)
        {
            var candidate = !string.IsNullOrWhiteSpace(unityPackagePath)
                ? Path.GetFileNameWithoutExtension(unityPackagePath)
                : ExtractFileNameFromSourceUrl(sourceUrl);

            if (string.IsNullOrWhiteSpace(candidate))
                candidate = "Custom Module";

            return HumanizeName(candidate);
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

        private static string HumanizeName(string value)
        {
            return value.Replace('-', ' ').Replace('_', ' ').Trim();
        }
    }
}
