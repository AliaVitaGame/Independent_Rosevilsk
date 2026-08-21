using System.Collections.Generic;

namespace SampleProjectAV.CustomUpmManager.Editor
{
    public sealed class UnityPackageImportResult
    {
        public UnityPackageImportResult(IEnumerable<string> importedAssetPaths)
        {
            ImportedAssetPaths = new List<string>(importedAssetPaths ?? new string[0]);
        }

        public List<string> ImportedAssetPaths { get; }
    }
}
