using Unity.Services.Multiplay.Authoring.Editor.Shared.Analytics;
using Unity.Services.Multiplay.Authoring.Editor.Shared.Logging;
using UnityEngine.Analytics;

namespace Unity.Services.Multiplay.Authoring.Editor.Analytics
{
    class AnalyticsUtils : IAnalyticsUtils
    {
        readonly ICommonAnalytics m_CommonAnalytics;

        public AnalyticsUtils(ICommonAnalytics commonAnalytics)
        {
            m_CommonAnalytics = commonAnalytics;
        }

        public AnalyticsResult SendCommonEvent(ICommonAnalytics.CommonEventPayload payload)
        {
            var result = m_CommonAnalytics.Send(payload);

            Logger.LogVerbose($"Sent Analytics Event: '{CommonAnalytics.eventName}.v{CommonAnalytics.version}' with Action '{payload.action}'. Result: {result}");

            return result;
        }
    }
}
