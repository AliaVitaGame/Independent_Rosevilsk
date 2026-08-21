using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace SampleProjectAV.CustomUpmManager.Editor.Tests
{
    public sealed class UnityPackageLifecycleTests
    {
        [Test]
        public void DownloadImportDeleteAndDeleteCompletelyUseSeparateStates()
        {
            var cacheRoot = Path.Combine(Path.GetTempPath(), "CustomUpmManagerTests", Guid.NewGuid().ToString("N"));
            var downloadClient = new FakeDownloadClient();
            var importTracker = new FakeImportTracker();
            var stateStore = new InMemoryInstallStateStore();
            var installer = new ModuleInstaller(null, downloadClient, null, importTracker, stateStore, null, cacheRoot);
            var module = new CustomUpmModule
            {
                Id = "sample-unitypackage",
                SourceKind = CustomUpmSourceKind.UnityPackageUrl,
                SourceUrl = "https://example.com/sample.unitypackage"
            };
            var version = new CustomUpmVersion("1.0.0", "1.0.0");

            try
            {
                AssertCompletesSuccessfully(installer.DownloadAsync(module, version, null, CancellationToken.None));

                Assert.IsTrue(installer.HasCachedPackage(module, version));
                Assert.IsFalse(stateStore.Database.TryGet(module.Id, out _));

                AssertCompletesSuccessfully(installer.ImportAsync(module, version, CancellationToken.None));

                Assert.IsTrue(stateStore.Database.TryGet(module.Id, out _));
                Assert.AreEqual(1, importTracker.ImportedPackages.Count);

                AssertCompletesSuccessfully(installer.DeleteAsync(module, CancellationToken.None));

                Assert.IsFalse(stateStore.Database.TryGet(module.Id, out _));
                Assert.IsTrue(installer.HasCachedPackage(module, version));
                CollectionAssert.AreEqual(new[] { "Assets/Imported.asset" }, importTracker.DeletedAssets);

                AssertCompletesSuccessfully(installer.DeleteCachedPackageAsync(module, version, CancellationToken.None));

                Assert.IsFalse(installer.HasCachedPackage(module, version));
            }
            finally
            {
                if (Directory.Exists(cacheRoot))
                    Directory.Delete(cacheRoot, true);
            }
        }

        [Test]
        public void GitRepositoryDefaultVersionDoesNotUsePackageVersionAsGitRevision()
        {
            var module = new CustomUpmModule
            {
                Id = "hot-reload",
                SourceKind = CustomUpmSourceKind.GitRepositoryUnityPackages,
                SourceUrl = "https://example.com/packages.git",
                UnityPackagePath = "Hot Reload.unitypackage",
                DefaultVersion = "1.12.14"
            };

            var method = typeof(ModuleInstaller).GetMethod("CreateDefaultVersion", BindingFlags.NonPublic | BindingFlags.Static);
            var version = (CustomUpmVersion)method.Invoke(null, new object[] { module });

            Assert.AreEqual("1.12.14", version.Name);
            Assert.AreEqual(string.Empty, version.Revision);
        }

        [Test]
        public void DeleteDoesNotTouchProjectAssetsWhenImportHasNoTrackedFileList()
        {
            var stateStore = new InMemoryInstallStateStore();
            var module = new CustomUpmModule
            {
                Id = "untracked-import",
                SourceKind = CustomUpmSourceKind.UnityPackageUrl
            };
            stateStore.Database.Upsert(new CustomUpmInstallState
            {
                ModuleId = module.Id,
                SourceKind = module.SourceKind
            });
            var importTracker = new FakeImportTracker();
            var installer = new ModuleInstaller(null, null, null, importTracker, stateStore);

            var task = installer.DeleteAsync(module, CancellationToken.None);

            Assert.IsTrue(task.IsFaulted);
            StringAssert.Contains("no tracked file list", task.Exception.InnerException.Message);
            Assert.IsTrue(stateStore.Database.TryGet(module.Id, out _));
            Assert.IsEmpty(importTracker.DeletedAssets);
        }

        [Test]
        public void ForgetInstallationClearsOnlyTheCpmStatus()
        {
            var stateStore = new InMemoryInstallStateStore();
            var module = new CustomUpmModule
            {
                Id = "manually-removed-import",
                SourceKind = CustomUpmSourceKind.UnityPackageUrl
            };
            stateStore.Database.Upsert(new CustomUpmInstallState
            {
                ModuleId = module.Id,
                SourceKind = module.SourceKind
            });
            var importTracker = new FakeImportTracker();
            var installer = new ModuleInstaller(null, null, null, importTracker, stateStore);

            AssertCompletesSuccessfully(installer.ForgetInstallationAsync(module, CancellationToken.None));

            Assert.IsFalse(stateStore.Database.TryGet(module.Id, out _));
            Assert.IsEmpty(importTracker.DeletedAssets);
        }

        private static void AssertCompletesSuccessfully(Task task)
        {
            Assert.IsTrue(task.IsCompleted, "The fake operation must complete synchronously.");
            if (task.IsFaulted)
                throw task.Exception;
        }

        private sealed class FakeDownloadClient : IDownloadClient
        {
            public Task DownloadAsync(string url, string destinationPath, IProgress<float> progress, CancellationToken cancellationToken)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                File.WriteAllText(destinationPath, "unitypackage");
                progress?.Report(1f);
                return Task.CompletedTask;
            }
        }

        private sealed class FakeImportTracker : IUnityPackageImportTracker
        {
            public readonly List<string> ImportedPackages = new List<string>();
            public readonly List<string> DeletedAssets = new List<string>();

            public Task<UnityPackageImportResult> ImportPackageAsync(string packagePath, CustomUpmModule module, CustomUpmVersion version)
            {
                ImportedPackages.Add(packagePath);
                return Task.FromResult(new UnityPackageImportResult(new[] { "Assets/Imported.asset" }));
            }

            public void DeleteTrackedAssets(IEnumerable<string> assetPaths)
            {
                DeletedAssets.AddRange(assetPaths);
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
