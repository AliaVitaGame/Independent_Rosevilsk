using UnityEngine;

namespace SampleProjectAV.CustomUpmManager.Editor
{
    public sealed class AssetStoreClient : IAssetStoreClient
    {
        public void OpenAssetStore(string assetStoreUrl)
        {
            if (!string.IsNullOrWhiteSpace(assetStoreUrl))
                Application.OpenURL(assetStoreUrl);
        }
    }
}
