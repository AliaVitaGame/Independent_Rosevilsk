using Unity.Services.Multiplay.Authoring.Editor.Shared.Analytics;
using UnityEngine.Analytics;

namespace Unity.Services.Multiplay.Authoring.Editor.Analytics
{
    interface IAnalyticsUtils
    {
        AnalyticsResult SendCommonEvent(ICommonAnalytics.CommonEventPayload payload);
    }
}
