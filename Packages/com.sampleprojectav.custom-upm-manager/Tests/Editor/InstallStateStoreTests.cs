using System.IO;
using NUnit.Framework;

namespace SampleProjectAV.CustomUpmManager.Editor.Tests
{
    public sealed class InstallStateStoreTests
    {
        [Test]
        public void UpsertsAndPersistsInstallState()
        {
            var root = Path.Combine(Path.GetTempPath(), "CustomUpmManagerTests", Path.GetRandomFileName());
            Directory.CreateDirectory(root);
            var store = new JsonInstallStateStore(Path.Combine(root, "install-state.json"));
            var database = new CustomUpmInstallStateDatabase();
            database.Upsert(new CustomUpmInstallState
            {
                ModuleId = "sample-tool",
                InstalledVersion = "v1.0.0",
                InstalledRevision = "v1.0.0",
                PackageName = "com.org.sample"
            });

            store.Save(database);
            var loaded = store.Load();

            Assert.IsTrue(loaded.TryGet("sample-tool", out var state));
            Assert.AreEqual("v1.0.0", state.InstalledVersion);
            Assert.AreEqual("com.org.sample", state.PackageName);
        }
    }
}
