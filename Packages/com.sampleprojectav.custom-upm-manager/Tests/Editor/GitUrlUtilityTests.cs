using NUnit.Framework;

namespace SampleProjectAV.CustomUpmManager.Editor.Tests
{
    public sealed class GitUrlUtilityTests
    {
        [Test]
        public void ComposePackageIdentifierAddsRevisionAfterQuery()
        {
            var identifier = GitUrlUtility.ComposePackageIdentifier(
                "https://github.com/org/repo.git?path=Packages/Tool",
                "release/1.2.0");

            Assert.AreEqual("https://github.com/org/repo.git?path=Packages/Tool#release/1.2.0", identifier);
        }

        [Test]
        public void ComposePackageIdentifierReplacesExistingRevision()
        {
            var identifier = GitUrlUtility.ComposePackageIdentifier(
                "https://github.com/org/repo.git?path=Packages/Tool#old",
                "main");

            Assert.AreEqual("https://github.com/org/repo.git?path=Packages/Tool#main", identifier);
        }

        [Test]
        public void ComposePackageIdentifierDoesNotAppendEmptyRevision()
        {
            var identifier = GitUrlUtility.ComposePackageIdentifier(
                "https://github.com/org/repo.git",
                string.Empty);

            Assert.AreEqual("https://github.com/org/repo.git", identifier);
        }

        [Test]
        public void ExtractPackagePathReturnsPathQueryValueWithoutLeadingSlash()
        {
            var packagePath = GitUrlUtility.ExtractPackagePath("https://github.com/org/repo.git?path=/Packages/Tool#main");

            Assert.AreEqual("Packages/Tool", packagePath);
        }
    }
}
