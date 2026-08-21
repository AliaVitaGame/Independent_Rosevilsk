using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace SampleProjectAV.CustomUpmManager.Editor.Tests
{
    public sealed class GitPackageInstallerTests
    {
        [Test]
        public async Task InstallGitPackageUsesPackageManagerIdentifierAndStoresState()
        {
            var packageManager = new FakePackageManagerClient();
            var stateStore = new InMemoryInstallStateStore();
            var installer = new ModuleInstaller(packageManager, null, null, null, stateStore);
            var module = new CustomUpmModule
            {
                Id = "sample-tool",
                DisplayName = "Sample Tool",
                SourceKind = CustomUpmSourceKind.GitUpmPackage,
                SourceUrl = "https://github.com/org/sample.git?path=Packages/Sample",
                PackageName = "com.org.sample"
            };
            var version = new CustomUpmVersion("main", "main");

            await installer.InstallAsync(module, version, false, CancellationToken.None);

            Assert.AreEqual("https://github.com/org/sample.git?path=Packages/Sample#main", packageManager.AddedIdentifiers[0]);
            Assert.IsTrue(stateStore.Database.TryGet("sample-tool", out var state));
            Assert.AreEqual("main", state.InstalledVersion);
            Assert.AreEqual("com.org.sample", state.PackageName);
        }

        [Test]
        public async Task InstallGitPackageWithoutSelectedRevisionUsesRepositoryDefaultBranch()
        {
            var packageManager = new FakePackageManagerClient();
            var stateStore = new InMemoryInstallStateStore();
            var installer = new ModuleInstaller(packageManager, null, null, null, stateStore);
            var module = new CustomUpmModule
            {
                Id = "compact-view",
                DisplayName = "Compact View",
                SourceKind = CustomUpmSourceKind.GitUpmPackage,
                SourceUrl = "https://github.com/LazizAbduhalimov/Compact-View-for-Editor.git",
                PackageName = "com.laziz.compact-view-for-editor"
            };

            await installer.InstallAsync(module, null, false, CancellationToken.None);

            Assert.AreEqual("https://github.com/LazizAbduhalimov/Compact-View-for-Editor.git", packageManager.AddedIdentifiers[0]);
            Assert.IsTrue(stateStore.Database.TryGet("compact-view", out var state));
            Assert.AreEqual("default branch", state.InstalledVersion);
            Assert.AreEqual(string.Empty, state.InstalledRevision);
        }

        [Test]
        public async Task InstallGitPackageStoresPackageNameReturnedByUnityPackageManager()
        {
            var packageManager = new FakePackageManagerClient
            {
                PackageNameToReturn = "com.laziz.compact-view-for-editor"
            };
            var stateStore = new InMemoryInstallStateStore();
            var installer = new ModuleInstaller(packageManager, null, null, null, stateStore);
            var module = new CustomUpmModule
            {
                Id = "compact-view",
                DisplayName = "Compact View",
                SourceKind = CustomUpmSourceKind.GitUpmPackage,
                SourceUrl = "https://github.com/LazizAbduhalimov/Compact-View-for-Editor.git"
            };

            await installer.InstallAsync(module, null, false, CancellationToken.None);

            Assert.IsTrue(stateStore.Database.TryGet("compact-view", out var state));
            Assert.AreEqual("com.laziz.compact-view-for-editor", state.PackageName);
        }

        [Test]
        public async Task DeleteGitPackageUsesStoredPackageNameAndRemovesState()
        {
            var packageManager = new FakePackageManagerClient();
            var stateStore = new InMemoryInstallStateStore();
            stateStore.Database.Upsert(new CustomUpmInstallState
            {
                ModuleId = "sample-tool",
                PackageName = "com.org.sample",
                SourceKind = CustomUpmSourceKind.GitUpmPackage
            });
            var installer = new ModuleInstaller(packageManager, null, null, null, stateStore);
            var module = new CustomUpmModule
            {
                Id = "sample-tool",
                SourceKind = CustomUpmSourceKind.GitUpmPackage,
                PackageName = "com.org.sample"
            };

            await installer.DeleteAsync(module, CancellationToken.None);

            Assert.AreEqual("com.org.sample", packageManager.RemovedPackages[0]);
            Assert.IsFalse(stateStore.Database.TryGet("sample-tool", out _));
        }

        [Test]
        public async Task InstallAssetStoreUpmPackageUsesPackageNameWhenAvailable()
        {
            var packageManager = new FakePackageManagerClient
            {
                PackageNameToReturn = "com.company.asset-tool"
            };
            var assetStore = new FakeAssetStoreClient();
            var stateStore = new InMemoryInstallStateStore();
            var installer = new ModuleInstaller(packageManager, null, null, null, stateStore, assetStore);
            var module = new CustomUpmModule
            {
                Id = "asset-tool",
                DisplayName = "Asset Tool",
                SourceKind = CustomUpmSourceKind.AssetStoreUrl,
                SourceUrl = "https://assetstore.unity.com/packages/tools/asset-tool-123",
                PackageName = "com.company.asset-tool"
            };

            await installer.InstallAsync(module, null, false, CancellationToken.None);

            Assert.AreEqual("com.company.asset-tool", packageManager.AddedIdentifiers[0]);
            Assert.AreEqual(0, assetStore.OpenedUrls.Count);
            Assert.IsTrue(stateStore.Database.TryGet("asset-tool", out var state));
            Assert.AreEqual("com.company.asset-tool", state.PackageName);
        }

        [Test]
        public async Task InstallAssetStorePackageOpensStoreWhenUpmInstallFails()
        {
            var packageManager = new FakePackageManagerClient
            {
                ErrorOnAdd = true
            };
            var assetStore = new FakeAssetStoreClient();
            var stateStore = new InMemoryInstallStateStore();
            var installer = new ModuleInstaller(packageManager, null, null, null, stateStore, assetStore);
            var module = new CustomUpmModule
            {
                Id = "asset-tool",
                DisplayName = "Asset Tool",
                SourceKind = CustomUpmSourceKind.AssetStoreUrl,
                SourceUrl = "https://assetstore.unity.com/packages/tools/asset-tool-123",
                PackageName = "com.company.asset-tool"
            };

            await installer.InstallAsync(module, null, false, CancellationToken.None);

            Assert.AreEqual("https://assetstore.unity.com/packages/tools/asset-tool-123", assetStore.OpenedUrls[0]);
            Assert.IsFalse(stateStore.Database.TryGet("asset-tool", out _));
        }

        private sealed class FakePackageManagerClient : IUnityPackageManagerClient
        {
            public readonly List<string> AddedIdentifiers = new List<string>();
            public readonly List<string> RemovedPackages = new List<string>();
            public string PackageNameToReturn = "com.org.sample";
            public bool ErrorOnAdd;

            public Task<string> AddAsync(string packageIdentifier, CancellationToken cancellationToken)
            {
                AddedIdentifiers.Add(packageIdentifier);
                if (ErrorOnAdd)
                    throw new System.InvalidOperationException("Package is not available.");

                return Task.FromResult(PackageNameToReturn);
            }

            public Task RemoveAsync(string packageName, CancellationToken cancellationToken)
            {
                RemovedPackages.Add(packageName);
                return Task.CompletedTask;
            }
        }

        private sealed class FakeAssetStoreClient : IAssetStoreClient
        {
            public readonly List<string> OpenedUrls = new List<string>();

            public void OpenAssetStore(string assetStoreUrl)
            {
                OpenedUrls.Add(assetStoreUrl);
            }
        }

        private sealed class InMemoryInstallStateStore : IInstallStateStore
        {
            public CustomUpmInstallStateDatabase Database { get; } = new CustomUpmInstallStateDatabase();

            public CustomUpmInstallStateDatabase Load() => Database;

            public void Save(CustomUpmInstallStateDatabase database)
            {
            }
        }
    }
}
