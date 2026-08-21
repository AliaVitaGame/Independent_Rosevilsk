using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SampleProjectAV.CustomUpmManager.Editor
{
    public sealed class ModuleInstaller : IModuleInstaller
    {
        private readonly IUnityPackageManagerClient packageManagerClient;
        private readonly IDownloadClient downloadClient;
        private readonly IGitClient gitClient;
        private readonly IUnityPackageImportTracker importTracker;
        private readonly IInstallStateStore installStateStore;
        private readonly IAssetStoreClient assetStoreClient;
        private readonly string packageCacheRoot;

        public ModuleInstaller(
            IUnityPackageManagerClient packageManagerClient,
            IDownloadClient downloadClient,
            IGitClient gitClient,
            IUnityPackageImportTracker importTracker,
            IInstallStateStore installStateStore,
            IAssetStoreClient assetStoreClient = null,
            string packageCacheRoot = null)
        {
            this.packageManagerClient = packageManagerClient;
            this.downloadClient = downloadClient;
            this.gitClient = gitClient;
            this.importTracker = importTracker;
            this.installStateStore = installStateStore;
            this.assetStoreClient = assetStoreClient;
            this.packageCacheRoot = string.IsNullOrWhiteSpace(packageCacheRoot)
                ? CustomUpmPaths.DownloadCacheRoot
                : packageCacheRoot;
        }

        public async Task InstallAsync(CustomUpmModule module, CustomUpmVersion version, bool deleteBeforeUpdate, CancellationToken cancellationToken)
        {
            ValidateModule(module);
            version = version ?? CreateDefaultVersion(module);

            if (deleteBeforeUpdate)
                await DeleteAsync(module, cancellationToken);

            switch (module.SourceKind)
            {
                case CustomUpmSourceKind.GitUpmPackage:
                    await InstallGitUpmPackageAsync(module, version, cancellationToken);
                    break;
                case CustomUpmSourceKind.UnityPackageUrl:
                    await DownloadAsync(module, version, null, cancellationToken);
                    break;
                case CustomUpmSourceKind.GitRepositoryUnityPackages:
                    await DownloadAsync(module, version, null, cancellationToken);
                    break;
                case CustomUpmSourceKind.AssetStoreUrl:
                    await InstallAssetStorePackageAsync(module, version, cancellationToken);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(module.SourceKind), module.SourceKind, "Unsupported module source kind.");
            }
        }

        public bool SupportsCachedPackage(CustomUpmModule module)
        {
            return module != null &&
                   (module.SourceKind == CustomUpmSourceKind.UnityPackageUrl ||
                    module.SourceKind == CustomUpmSourceKind.GitRepositoryUnityPackages);
        }

        public bool HasCachedPackage(CustomUpmModule module, CustomUpmVersion version)
        {
            return SupportsCachedPackage(module) && File.Exists(GetCachedPackagePath(module, version));
        }

        public async Task DownloadAsync(CustomUpmModule module, CustomUpmVersion version, IProgress<float> progress, CancellationToken cancellationToken)
        {
            ValidateModule(module);
            version = version ?? CreateDefaultVersion(module);

            if (!SupportsCachedPackage(module))
                throw new InvalidOperationException("This module does not use a local unitypackage cache.");

            var destinationPath = GetCachedPackagePath(module, version);
            Directory.CreateDirectory(packageCacheRoot);

            if (module.SourceKind == CustomUpmSourceKind.UnityPackageUrl)
            {
                if (downloadClient == null)
                    throw new InvalidOperationException("Download client is required for unitypackage URLs.");

                await downloadClient.DownloadAsync(FirstNonEmpty(version.SourceUrl, module.SourceUrl), destinationPath, progress, cancellationToken);
                return;
            }

            if (gitClient == null)
                throw new InvalidOperationException("Git client is required for Git repository unitypackages.");

            progress?.Report(0.05f);
            var repositoryPath = await gitClient.PrepareRepositoryAsync(module.SourceUrl, version.Revision, CustomUpmPaths.GitCacheRoot, cancellationToken);
            var sourcePath = Path.Combine(repositoryPath, FirstNonEmpty(version.UnityPackagePath, module.UnityPackagePath));

            if (!File.Exists(sourcePath))
                throw new FileNotFoundException($"Unitypackage was not found in cloned repository: {sourcePath}", sourcePath);

            progress?.Report(0.1f);
            var copyProgress = progress == null
                ? null
                : new Progress<float>(value => progress.Report(0.1f + Math.Max(0f, Math.Min(1f, value)) * 0.9f));
            await CopyToCacheAsync(sourcePath, destinationPath, copyProgress, cancellationToken);
        }

        public async Task ImportAsync(CustomUpmModule module, CustomUpmVersion version, CancellationToken cancellationToken)
        {
            ValidateModule(module);
            version = version ?? CreateDefaultVersion(module);

            if (!SupportsCachedPackage(module))
                throw new InvalidOperationException("This module does not use a local unitypackage cache.");

            if (importTracker == null)
                throw new InvalidOperationException("Unitypackage import tracker is required for unitypackage URLs.");

            var packagePath = GetCachedPackagePath(module, version);
            if (!File.Exists(packagePath))
                throw new FileNotFoundException($"Downloaded unitypackage was not found: {packagePath}", packagePath);

            var result = await importTracker.ImportPackageAsync(packagePath, module, version);
            cancellationToken.ThrowIfCancellationRequested();
            UpsertState(module, version, result, null);
        }

        public Task DeleteCachedPackageAsync(CustomUpmModule module, CustomUpmVersion version, CancellationToken cancellationToken)
        {
            ValidateModule(module);
            cancellationToken.ThrowIfCancellationRequested();

            if (!SupportsCachedPackage(module))
                throw new InvalidOperationException("This module does not use a local unitypackage cache.");

            var packagePath = GetCachedPackagePath(module, version ?? CreateDefaultVersion(module));
            if (File.Exists(packagePath))
                File.Delete(packagePath);

            return Task.CompletedTask;
        }

        public async Task DeleteAsync(CustomUpmModule module, CancellationToken cancellationToken)
        {
            ValidateModule(module);

            var database = installStateStore.Load();
            if (!database.TryGet(module.Id, out var state))
                return;

            var sourceKind = state.SourceKind;
            if (sourceKind == CustomUpmSourceKind.GitUpmPackage || sourceKind == CustomUpmSourceKind.AssetStoreUrl)
            {
                var packageName = FirstNonEmpty(state.PackageName, module.PackageName);
                if (!string.IsNullOrWhiteSpace(packageName))
                {
                    if (packageManagerClient == null)
                        throw new InvalidOperationException("Package manager client is required for Package Manager packages.");

                    await packageManagerClient.RemoveAsync(packageName, cancellationToken);
                }
            }
            else
            {
                if (state.ImportedAssetPaths.Count == 0)
                {
                    throw new InvalidOperationException(
                        "This import has no tracked file list, so Custom UPM will not delete it automatically. Remove it manually to avoid deleting unrelated assets.");
                }

                importTracker?.DeleteTrackedAssets(state.ImportedAssetPaths);
            }

            database.Remove(module.Id);
            installStateStore.Save(database);
        }

        public Task ForgetInstallationAsync(CustomUpmModule module, CancellationToken cancellationToken)
        {
            ValidateModule(module);
            cancellationToken.ThrowIfCancellationRequested();

            var database = installStateStore.Load();
            database.Remove(module.Id);
            installStateStore.Save(database);
            return Task.CompletedTask;
        }

        private async Task InstallAssetStorePackageAsync(CustomUpmModule module, CustomUpmVersion version, CancellationToken cancellationToken)
        {
            if (packageManagerClient != null && !string.IsNullOrWhiteSpace(module.PackageName))
            {
                try
                {
                    var resolvedPackageName = await packageManagerClient.AddAsync(module.PackageName, cancellationToken);
                    UpsertState(module, version, null, resolvedPackageName);
                    return;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    OpenAssetStore(module);
                    return;
                }
            }

            OpenAssetStore(module);
        }

        private void OpenAssetStore(CustomUpmModule module)
        {
            if (assetStoreClient == null)
                throw new InvalidOperationException("Asset Store client is required for Asset Store URLs.");

            assetStoreClient.OpenAssetStore(module.SourceUrl);
        }

        private async Task InstallGitUpmPackageAsync(CustomUpmModule module, CustomUpmVersion version, CancellationToken cancellationToken)
        {
            if (packageManagerClient == null)
                throw new InvalidOperationException("Package manager client is required for Git UPM packages.");

            var identifier = GitUrlUtility.ComposePackageIdentifier(module.SourceUrl, version.Revision);
            var resolvedPackageName = await packageManagerClient.AddAsync(identifier, cancellationToken);

            UpsertState(module, version, null, resolvedPackageName);
        }

        private string GetCachedPackagePath(CustomUpmModule module, CustomUpmVersion version)
        {
            var key = string.Join(
                "|",
                module.Id,
                FirstNonEmpty(version?.Revision, module.DefaultVersion),
                FirstNonEmpty(version?.SourceUrl, module.SourceUrl),
                FirstNonEmpty(version?.UnityPackagePath, module.UnityPackagePath));
            return Path.Combine(packageCacheRoot, $"{StableHash.Sha1(key)}.unitypackage");
        }

        private static async Task CopyToCacheAsync(string sourcePath, string destinationPath, IProgress<float> progress, CancellationToken cancellationToken)
        {
            var temporaryPath = destinationPath + ".part";
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);

            var totalBytes = new FileInfo(sourcePath).Length;
            var copiedBytes = 0L;
            var buffer = new byte[81920];

            using (var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, buffer.Length, true))
            using (var destination = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None, buffer.Length, true))
            {
                while (true)
                {
                    var read = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (read == 0)
                        break;

                    await destination.WriteAsync(buffer, 0, read, cancellationToken);
                    copiedBytes += read;
                    progress?.Report(totalBytes == 0 ? 1f : (float)copiedBytes / totalBytes);
                }
            }

            if (File.Exists(destinationPath))
                File.Delete(destinationPath);

            File.Move(temporaryPath, destinationPath);
            progress?.Report(1f);
        }

        private void UpsertState(CustomUpmModule module, CustomUpmVersion version, UnityPackageImportResult importResult, string resolvedPackageName)
        {
            var database = installStateStore.Load();
            var state = new CustomUpmInstallState
            {
                ModuleId = module.Id,
                InstalledVersion = version.Name,
                InstalledRevision = version.Revision,
                PackageName = FirstNonEmpty(module.PackageName, resolvedPackageName),
                SourceKind = module.SourceKind
            };

            if (importResult != null)
                state.SetImportedAssetPaths(importResult.ImportedAssetPaths);

            database.Upsert(state);
            installStateStore.Save(database);
        }

        private static CustomUpmVersion CreateDefaultVersion(CustomUpmModule module)
        {
            if (module.SourceKind == CustomUpmSourceKind.GitUpmPackage)
            {
                return new CustomUpmVersion("default branch", string.Empty)
                {
                    SourceUrl = module.SourceUrl,
                    UnityPackagePath = module.UnityPackagePath
                };
            }

            var name = string.IsNullOrWhiteSpace(module.DefaultVersion) ? "1.0.0" : module.DefaultVersion;
            var revision = module.SourceKind == CustomUpmSourceKind.GitRepositoryUnityPackages
                ? string.Empty
                : name;
            return new CustomUpmVersion(name, revision)
            {
                SourceUrl = module.SourceUrl,
                UnityPackagePath = module.UnityPackagePath
            };
        }

        private static void ValidateModule(CustomUpmModule module)
        {
            if (module == null)
                throw new ArgumentNullException(nameof(module));

            if (string.IsNullOrWhiteSpace(module.Id))
                throw new ArgumentException("Module id is required.", nameof(module));
        }

        private static string FirstNonEmpty(string first, string second)
        {
            return !string.IsNullOrWhiteSpace(first) ? first : second;
        }
    }
}
