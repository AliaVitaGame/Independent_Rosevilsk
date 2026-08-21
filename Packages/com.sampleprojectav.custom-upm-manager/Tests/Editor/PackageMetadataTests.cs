using NUnit.Framework;

namespace SampleProjectAV.CustomUpmManager.Editor.Tests
{
    public sealed class PackageMetadataTests
    {
        [Test]
        public void ModuleFactoryUsesSourceUrlAsDocumentationUrlFallback()
        {
            const string sourceUrl = "https://github.com/company/tool.git";

            var module = CustomUpmModuleFactory.Create(
                "Tool",
                sourceUrl,
                CustomUpmSourceKind.GitUpmPackage,
                "com.company.tool",
                string.Empty,
                "1.0.0");

            Assert.AreEqual(sourceUrl, module.DocumentationUrl);
        }

        [Test]
        public void ModuleFactoryKeepsCustomDocumentationUrl()
        {
            var module = CustomUpmModuleFactory.Create(
                "Tool",
                "https://github.com/company/tool.git",
                CustomUpmSourceKind.GitUpmPackage,
                "com.company.tool",
                "https://docs.company/tool",
                "1.0.0");

            Assert.AreEqual("https://docs.company/tool", module.DocumentationUrl);
        }

        [Test]
        public void ParsesPackageJsonDisplayNameNameAndVersion()
        {
            const string json = @"{
  ""name"": ""com.laziz.compact-view-for-editor"",
  ""version"": ""1.0.0"",
  ""displayName"": ""Compact View for Editor""
}";

            var metadata = PackageJsonMetadataParser.Parse(json);

            Assert.AreEqual("Compact View for Editor", metadata.DisplayName);
            Assert.AreEqual("com.laziz.compact-view-for-editor", metadata.PackageName);
            Assert.AreEqual("1.0.0", metadata.Version);
        }

        [Test]
        public void FallsBackToPackageNameWhenDisplayNameIsMissing()
        {
            const string json = @"{
  ""name"": ""com.company.tool"",
  ""version"": ""2.0.0""
}";

            var metadata = PackageJsonMetadataParser.Parse(json);

            Assert.AreEqual("com.company.tool", metadata.DisplayName);
            Assert.AreEqual("com.company.tool", metadata.PackageName);
            Assert.AreEqual("2.0.0", metadata.Version);
        }
    }
}
