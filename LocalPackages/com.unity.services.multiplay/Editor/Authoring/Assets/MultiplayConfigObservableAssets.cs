using Unity.Services.Multiplay.Authoring.Editor.Analytics;
using Unity.Services.Multiplay.Authoring.Editor.Shared.Assets;

namespace Unity.Services.Multiplay.Authoring.Editor.Assets
{
    class MultiplayConfigObservableAssets : ObservableAssets<MultiplayConfigAsset>
    {
        readonly IAssetAnalytics m_AssetAnalytics;

        public MultiplayConfigObservableAssets(IAssetAnalytics assetAnalytics)
            : base(new [] { MultiplayConfigResource.FileExtension }, new AssetPostprocessorProxy(), false)
        {
            m_AssetAnalytics = assetAnalytics;
            LoadAllAssets();
        }

        protected override void AddForPath(string path, MultiplayConfigAsset asset)
        {
            base.AddForPath(path, asset);
            m_AssetAnalytics?.AddAsset(asset);
        }

        protected override void UpdateForPath(string path, MultiplayConfigAsset asset)
        {
            base.UpdateForPath(path, asset);
            m_AssetAnalytics?.UpdateAsset(asset);
        }

        protected override void MovePath(string toPath, string fromPath)
        {
            base.MovePath(toPath, fromPath);
            m_AssetAnalytics?.UpdateAsset(m_AssetPaths[toPath]);
        }

        protected override void RemoveForPath(string path, MultiplayConfigAsset asset)
        {
            base.RemoveForPath(path, asset);
            m_AssetAnalytics?.DeleteAsset(asset);
        }
    }
}
