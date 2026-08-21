using System.IO;
using NUnit.Framework;

namespace SampleProjectAV.CustomUpmManager.Editor.Tests
{
    public sealed class RegistryStoreTests
    {
        [Test]
        public void SavesAndLoadsModuleRegistry()
        {
            var root = CreateTempRoot();
            var store = new JsonModuleRegistryStore(Path.Combine(root, "modules.json"));
            var registry = new CustomUpmRegistry();
            registry.Modules.Add(new CustomUpmModule
            {
                Id = "sample-tool",
                DisplayName = "Sample Tool",
                Category = "Editor Tools",
                SourceKind = CustomUpmSourceKind.GitUpmPackage,
                SourceUrl = "https://github.com/org/sample.git",
                PackageName = "com.org.sample"
            });

            store.Save(registry);
            var loaded = store.Load();

            Assert.AreEqual(1, loaded.Modules.Count);
            Assert.AreEqual("sample-tool", loaded.Modules[0].Id);
            Assert.AreEqual("Sample Tool", loaded.Modules[0].DisplayName);
            Assert.AreEqual("Editor Tools", loaded.Modules[0].Category);
            Assert.AreEqual(CustomUpmSourceKind.GitUpmPackage, loaded.Modules[0].SourceKind);
            Assert.AreEqual("com.org.sample", loaded.Modules[0].PackageName);
        }

        private static string CreateTempRoot()
        {
            var root = Path.Combine(Path.GetTempPath(), "CustomUpmManagerTests", Path.GetRandomFileName());
            Directory.CreateDirectory(root);
            return root;
        }
    }
}
