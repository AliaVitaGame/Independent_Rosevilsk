using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SampleProjectAV.CustomUpmManager.Editor
{
    public interface IModuleRegistryStore
    {
        CustomUpmRegistry Load();
        void Save(CustomUpmRegistry registry);
    }

    public interface IInstallStateStore
    {
        CustomUpmInstallStateDatabase Load();
        void Save(CustomUpmInstallStateDatabase database);
    }

    public interface IModuleVersionProvider
    {
        Task<IReadOnlyList<CustomUpmVersion>> GetVersionsAsync(CustomUpmModule module, CancellationToken cancellationToken);
        Task<IReadOnlyList<string>> GetUnityPackageFilesAsync(CustomUpmModule module, string revision, CancellationToken cancellationToken);
    }

    public interface IModuleInstaller
    {
        Task InstallAsync(CustomUpmModule module, CustomUpmVersion version, bool deleteBeforeUpdate, CancellationToken cancellationToken);
        Task DownloadAsync(CustomUpmModule module, CustomUpmVersion version, IProgress<float> progress, CancellationToken cancellationToken);
        Task ImportAsync(CustomUpmModule module, CustomUpmVersion version, CancellationToken cancellationToken);
        Task DeleteAsync(CustomUpmModule module, CancellationToken cancellationToken);
        Task ForgetInstallationAsync(CustomUpmModule module, CancellationToken cancellationToken);
        Task DeleteCachedPackageAsync(CustomUpmModule module, CustomUpmVersion version, CancellationToken cancellationToken);
        bool HasCachedPackage(CustomUpmModule module, CustomUpmVersion version);
        bool SupportsCachedPackage(CustomUpmModule module);
    }

    public interface IModuleMetadataProvider
    {
        Task<PackageJsonMetadata> GetMetadataAsync(
            CustomUpmSourceKind sourceKind,
            string sourceUrl,
            string unityPackagePath,
            CancellationToken cancellationToken);
    }

    public interface IGitClient
    {
        Task<IReadOnlyList<GitRemoteRef>> ListRemoteRefsAsync(string repositoryUrl, CancellationToken cancellationToken);
        Task<string> PrepareRepositoryAsync(string repositoryUrl, string revision, string cacheRoot, CancellationToken cancellationToken);
        Task<IReadOnlyList<string>> FindUnityPackageFilesAsync(string repositoryPath, CancellationToken cancellationToken);
    }

    public interface IDownloadClient
    {
        Task DownloadAsync(string url, string destinationPath, IProgress<float> progress, CancellationToken cancellationToken);
    }

    public interface IUnityPackageImportTracker
    {
        Task<UnityPackageImportResult> ImportPackageAsync(string packagePath, CustomUpmModule module, CustomUpmVersion version);
        void DeleteTrackedAssets(IEnumerable<string> assetPaths);
    }

    public interface IUnityPackageManagerClient
    {
        Task<string> AddAsync(string packageIdentifier, CancellationToken cancellationToken);
        Task RemoveAsync(string packageName, CancellationToken cancellationToken);
    }

    public interface IAssetStoreClient
    {
        void OpenAssetStore(string assetStoreUrl);
    }
}
