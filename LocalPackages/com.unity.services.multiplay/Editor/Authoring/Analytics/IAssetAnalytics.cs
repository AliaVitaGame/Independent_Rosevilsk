using Unity.Services.Multiplay.Authoring.Editor.Assets;

namespace Unity.Services.Multiplay.Authoring.Editor.Analytics
{
    interface IAssetAnalytics
    {
        void AddAsset(MultiplayConfigAsset asset);
        void UpdateAsset(MultiplayConfigAsset asset);
        void DeleteAsset(MultiplayConfigAsset asset);
    }
}
