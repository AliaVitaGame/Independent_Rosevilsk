using Unity.Services.Multiplay.Authoring.Editor.Assets;
using Unity.Services.Multiplay.Authoring.Editor.Shared.Analytics;

namespace Unity.Services.Multiplay.Authoring.Editor.Analytics
{
    class AssetAnalytics : IAssetAnalytics
    {
        readonly IAnalyticsUtils m_AnalyticsUtils;

        public AssetAnalytics(IAnalyticsUtils analyticsUtils)
        {
            m_AnalyticsUtils = analyticsUtils;
        }

        public void AddAsset(MultiplayConfigAsset asset)
        {
            m_AnalyticsUtils.SendCommonEvent(new ICommonAnalytics.CommonEventPayload()
            {
                action = "multiplay_file_created",
            });
        }

        public void UpdateAsset(MultiplayConfigAsset asset)
        {
            m_AnalyticsUtils.SendCommonEvent(new ICommonAnalytics.CommonEventPayload()
            {
                action = "multiplay_file_updated",
            });
        }

        public void DeleteAsset(MultiplayConfigAsset asset)
        {
            m_AnalyticsUtils.SendCommonEvent(new ICommonAnalytics.CommonEventPayload()
            {
                action = "multiplay_file_deleted",
            });
        }
    }
}
