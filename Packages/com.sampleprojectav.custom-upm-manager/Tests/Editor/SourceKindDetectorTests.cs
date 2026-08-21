using NUnit.Framework;

namespace SampleProjectAV.CustomUpmManager.Editor.Tests
{
    public sealed class SourceKindDetectorTests
    {
        [Test]
        public void DetectsDirectUnityPackageUrlIgnoringQuery()
        {
            var kind = CustomUpmSourceKindDetector.Detect("https://example.com/tools/MyTool.unitypackage?download=1");

            Assert.AreEqual(CustomUpmSourceKind.UnityPackageUrl, kind);
        }

        [Test]
        public void DefaultsGitUrlsToGitUpmPackage()
        {
            var kind = CustomUpmSourceKindDetector.Detect("https://github.com/org/tool.git?path=Assets/Package");

            Assert.AreEqual(CustomUpmSourceKind.GitUpmPackage, kind);
        }

        [Test]
        public void DetectsAssetStoreUrls()
        {
            var kind = CustomUpmSourceKindDetector.Detect("https://assetstore.unity.com/packages/tools/asset-tool-123");

            Assert.AreEqual(CustomUpmSourceKind.AssetStoreUrl, kind);
        }
    }
}
