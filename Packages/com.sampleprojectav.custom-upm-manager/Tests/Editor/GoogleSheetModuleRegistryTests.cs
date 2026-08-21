using System;
using NUnit.Framework;

namespace SampleProjectAV.CustomUpmManager.Editor.Tests
{
    public sealed class GoogleSheetModuleRegistryTests
    {
        [Test]
        public void BuildsCsvExportUrlsWithHeaderRowAndExportFallback()
        {
            var urls = GoogleSheetModuleRegistryStore.BuildCsvExportUrls(
                "https://docs.google.com/spreadsheets/d/15QVhNhdguL50j_8zzofHACE22VdGe7xF9mjxYvKy8HY/edit?gid=0#gid=0");

            Assert.AreEqual(2, urls.Count);
            Assert.AreEqual(
                "https://docs.google.com/spreadsheets/d/15QVhNhdguL50j_8zzofHACE22VdGe7xF9mjxYvKy8HY/gviz/tq?tqx=out:csv&gid=0&headers=1",
                urls[0]);
            Assert.AreEqual(
                "https://docs.google.com/spreadsheets/d/15QVhNhdguL50j_8zzofHACE22VdGe7xF9mjxYvKy8HY/export?format=csv&gid=0",
                urls[1]);
        }

        [Test]
        public void ParsesCustomPluginsFromSheetCsv()
        {
            const string csv = "Id,Display Name,Source Kind,Source URL,Package Name,Docs URL,Default Version,Unity Package Path,Category,Enabled\n"
                               + "ai-developer-from-murzak-a6b408e1,AI Developer from Murzak,UnityPackageUrl,https://github.com/IvanMurzak/Unity-MCP/releases/latest/download/AI-Game-Dev-Installer.unitypackage,,https://github.com/IvanMurzak/Unity-MCP,1.0.0,,Code,TRUE\n"
                               + "plugin-your-games-302343,Plugin Your Games (Unified API for WebGL and mobile stores),AssetStoreUrl,https://assetstore.unity.com/packages/tools/integration/plugin-your-games-unified-api-for-webgl-and-mobile-stores-302343,,https://assetstore.unity.com/packages/tools/integration/plugin-your-games-unified-api-for-webgl-and-mobile-stores-302343,2.007,,Code,TRUE\n"
                               + "timescale-toolbar,TimeScale Toolbar,AssetStoreUrl,https://assetstore.unity.com/packages/tools/utilities/timescale-toolbar-291564,,https://assetstore.unity.com/packages/tools/utilities/timescale-toolbar-291564,,,Code,TRUE";

            var registry = GoogleSheetModuleRegistryParser.Parse(csv);

            Assert.AreEqual(3, registry.Modules.Count);
            Assert.AreEqual("ai-developer-from-murzak-a6b408e1", registry.Modules[0].Id);
            Assert.AreEqual(CustomUpmSourceKind.UnityPackageUrl, registry.Modules[0].SourceKind);
            Assert.AreEqual("https://github.com/IvanMurzak/Unity-MCP", registry.Modules[0].DocumentationUrl);
            Assert.AreEqual("plugin-your-games-302343", registry.Modules[1].Id);
            Assert.AreEqual(CustomUpmSourceKind.AssetStoreUrl, registry.Modules[1].SourceKind);
            Assert.AreEqual("timescale-toolbar", registry.Modules[2].Id);
            Assert.AreEqual("Code", registry.Modules[2].Category);
        }

        [Test]
        public void RejectsMalformedGvizCsvWithoutHeaderRow()
        {
            const string csv = "\"Id ai-developer-from-murzak-a6b408e1 plugin-your-games-302343\",\"Display Name AI Developer from Murzak\",\"Source Kind UnityPackageUrl AssetStoreUrl\",\"Source URL https://github.com/IvanMurzak/Unity-MCP/releases/latest/download/AI-Game-Dev-Installer.unitypackage\"";

            Assert.IsFalse(GoogleSheetModuleRegistryParser.HasRequiredHeaders(csv));
            Assert.Throws<InvalidOperationException>(() => GoogleSheetModuleRegistryParser.Parse(csv));
        }

        [Test]
        public void ParsesEnabledModulesFromSheetCsv()
        {
            const string csv = "Id,Display Name,Source Kind,Source URL,Package Name,Docs URL,Default Version,Unity Package Path,Category,Enabled\n"
                               + "tool-1,Tool One,GitUpmPackage,https://github.com/company/tool.git,com.company.tool,,2.0.0,,Editor Tools,TRUE\n"
                               + "tool-2,Tool Two,UnityPackageUrl,https://example.com/tool.unitypackage,,,,,,FALSE";

            var registry = GoogleSheetModuleRegistryParser.Parse(csv);

            Assert.AreEqual(1, registry.Modules.Count);
            Assert.AreEqual("tool-1", registry.Modules[0].Id);
            Assert.AreEqual("Tool One", registry.Modules[0].DisplayName);
            Assert.AreEqual(CustomUpmSourceKind.GitUpmPackage, registry.Modules[0].SourceKind);
            Assert.AreEqual("https://github.com/company/tool.git", registry.Modules[0].DocumentationUrl);
            Assert.AreEqual("2.0.0", registry.Modules[0].DefaultVersion);
            Assert.AreEqual("Editor Tools", registry.Modules[0].Category);
        }

        [Test]
        public void ParsesQuotedCsvValues()
        {
            const string csv = "Display Name,Source Kind,Source URL,Enabled\n"
                               + "\"Tool, With Comma\",UnityPackageUrl,https://example.com/tool.unitypackage,TRUE";

            var registry = GoogleSheetModuleRegistryParser.Parse(csv);

            Assert.AreEqual(1, registry.Modules.Count);
            Assert.AreEqual("Tool, With Comma", registry.Modules[0].DisplayName);
            Assert.AreEqual(CustomUpmSourceKind.UnityPackageUrl, registry.Modules[0].SourceKind);
        }

        [Test]
        public void ParsesAssetStoreUrlSourceKind()
        {
            const string csv = "Display Name,Source Kind,Source URL,Package Name,Enabled\n"
                               + "Asset Tool,AssetStoreUrl,https://assetstore.unity.com/packages/tools/asset-tool-123,com.company.asset-tool,TRUE";

            var registry = GoogleSheetModuleRegistryParser.Parse(csv);

            Assert.AreEqual(1, registry.Modules.Count);
            Assert.AreEqual(CustomUpmSourceKind.AssetStoreUrl, registry.Modules[0].SourceKind);
            Assert.AreEqual("com.company.asset-tool", registry.Modules[0].PackageName);
        }

        [Test]
        public void UsesUncategorizedWhenCategoryColumnIsMissing()
        {
            const string csv = "Display Name,Source Kind,Source URL,Enabled\n"
                               + "Tool One,GitUpmPackage,https://github.com/company/tool.git,TRUE";

            var registry = GoogleSheetModuleRegistryParser.Parse(csv);

            Assert.AreEqual(CustomUpmModule.UncategorizedCategory, registry.Modules[0].Category);
        }

        [Test]
        public void ParsesContentCategoriesUsedForImportFolders()
        {
            const string csv = "Id,Display Name,Source Kind,Source URL,Category,Enabled\n"
                               + "feel,Feel,GitRepositoryUnityPackages,https://example.com/packages.git,VFX,TRUE\n"
                               + "monsters,Monsters,GitRepositoryUnityPackages,https://example.com/packages.git,Models,TRUE\n"
                               + "shadow,True Shadow,GitRepositoryUnityPackages,https://example.com/packages.git,UI,TRUE\n"
                               + "hot-reload,Hot Reload,GitRepositoryUnityPackages,https://example.com/packages.git,Code,TRUE\n"
                               + "template,Downhill Ride,GitRepositoryUnityPackages,https://example.com/packages.git,Templates,TRUE";

            var registry = GoogleSheetModuleRegistryParser.Parse(csv);

            Assert.AreEqual("VFX", registry.Modules[0].Category);
            Assert.IsTrue(CustomUpmCategoryImportPaths.TryGetImportFolder(registry.Modules[0].Category, out var vfx));
            Assert.AreEqual(CustomUpmCategoryImportPaths.VfxFolder, vfx);

            Assert.AreEqual("Models", registry.Modules[1].Category);
            Assert.IsTrue(CustomUpmCategoryImportPaths.TryGetImportFolder(registry.Modules[1].Category, out var models));
            Assert.AreEqual(CustomUpmCategoryImportPaths.ModelsFolder, models);

            Assert.AreEqual("UI", registry.Modules[2].Category);
            Assert.IsTrue(CustomUpmCategoryImportPaths.TryGetImportFolder(registry.Modules[2].Category, out var ui));
            Assert.AreEqual(CustomUpmCategoryImportPaths.UiFolder, ui);

            Assert.AreEqual("Code", registry.Modules[3].Category);
            Assert.IsFalse(CustomUpmCategoryImportPaths.TryGetImportFolder(registry.Modules[3].Category, out _));

            Assert.AreEqual("Templates", registry.Modules[4].Category);
            Assert.IsFalse(CustomUpmCategoryImportPaths.TryGetImportFolder(registry.Modules[4].Category, out _));
        }
    }
}
