using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SampleProjectAV.CustomUpmManager.Editor
{
    public sealed class ModuleVersionProvider : IModuleVersionProvider
    {
        private readonly IGitClient gitClient;

        public ModuleVersionProvider(IGitClient gitClient)
        {
            this.gitClient = gitClient;
        }

        public async Task<IReadOnlyList<CustomUpmVersion>> GetVersionsAsync(CustomUpmModule module, CancellationToken cancellationToken)
        {
            if (module == null)
                return new List<CustomUpmVersion>();

            if (module.SourceKind == CustomUpmSourceKind.UnityPackageUrl ||
                module.SourceKind == CustomUpmSourceKind.AssetStoreUrl)
                return GetManualUnityPackageVersions(module);

            var repositoryUrl = GitUrlUtility.ExtractRepositoryUrl(module.SourceUrl);
            var refs = await gitClient.ListRemoteRefsAsync(repositoryUrl, cancellationToken);
            var versions = refs
                .Select(remoteRef => new CustomUpmVersion(remoteRef.Name, remoteRef.Revision)
                {
                    IsTag = remoteRef.IsTag,
                    SourceUrl = module.SourceUrl,
                    UnityPackagePath = module.UnityPackagePath
                })
                .ToList();

            return VersionSorter.SortDescending(versions);
        }

        public async Task<IReadOnlyList<string>> GetUnityPackageFilesAsync(CustomUpmModule module, string revision, CancellationToken cancellationToken)
        {
            if (module == null || module.SourceKind != CustomUpmSourceKind.GitRepositoryUnityPackages)
                return new List<string>();

            var repositoryPath = await gitClient.PrepareRepositoryAsync(module.SourceUrl, revision, CustomUpmPaths.GitCacheRoot, cancellationToken);
            return await gitClient.FindUnityPackageFilesAsync(repositoryPath, cancellationToken);
        }

        private static IReadOnlyList<CustomUpmVersion> GetManualUnityPackageVersions(CustomUpmModule module)
        {
            module.EnsureLists();

            if (module.Versions.Count > 0)
                return VersionSorter.SortDescending(module.Versions);

            var versionName = string.IsNullOrWhiteSpace(module.DefaultVersion) ? "1.0.0" : module.DefaultVersion;
            return new List<CustomUpmVersion>
            {
                new CustomUpmVersion(versionName, versionName)
                {
                    SourceUrl = module.SourceUrl,
                    UnityPackagePath = module.UnityPackagePath
                }
            };
        }
    }
}
