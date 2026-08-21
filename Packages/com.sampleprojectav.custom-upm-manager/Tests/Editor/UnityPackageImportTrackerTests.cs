using System.Collections.Generic;
using NUnit.Framework;

namespace SampleProjectAV.CustomUpmManager.Editor.Tests
{
    public sealed class UnityPackageImportTrackerTests
    {
        [Test]
        public void FiltersOutAssetsThatExistedBeforeImport()
        {
            var result = UnityPackageImportTracker.FilterNewImportedPaths(
                new[] { "Assets/Existing.asset", "Assets/New.asset", "Packages/Other/file.txt" },
                new HashSet<string> { "Assets/Existing.asset" });

            CollectionAssert.AreEqual(new[] { "Assets/New.asset" }, result);
        }
    }
}
