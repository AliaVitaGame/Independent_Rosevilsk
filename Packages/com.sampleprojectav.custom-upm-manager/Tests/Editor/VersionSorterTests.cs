using System.Linq;
using NUnit.Framework;

namespace SampleProjectAV.CustomUpmManager.Editor.Tests
{
    public sealed class VersionSorterTests
    {
        [Test]
        public void SortsSemanticVersionsBeforeNamedRefs()
        {
            var versions = new[]
            {
                new CustomUpmVersion("develop", "develop"),
                new CustomUpmVersion("v1.10.0", "v1.10.0"),
                new CustomUpmVersion("v1.2.0", "v1.2.0"),
                new CustomUpmVersion("main", "main")
            };

            var sorted = VersionSorter.SortDescending(versions).Select(version => version.Name).ToArray();

            CollectionAssert.AreEqual(new[] { "v1.10.0", "v1.2.0", "main", "develop" }, sorted);
        }
    }
}
