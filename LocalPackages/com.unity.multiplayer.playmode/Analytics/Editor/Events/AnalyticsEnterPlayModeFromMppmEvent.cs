using System;
using UnityEngine.Analytics;
using UnityEngine.Serialization;

namespace Unity.Multiplayer.PlayMode.Analytics.Editor
{

    [Serializable]
    internal struct EnterPlayModeFromMppmData : IAnalytic.IData
    {
        public const string k_EnterPlayModeFromMppmEventName = "multiplayer_playmode_enterPlayModeFromMppm";

        public int VirtualPlayerCount;
        public int CloneWindowErrorCount;
    }

    [AnalyticInfo(eventName: EnterPlayModeFromMppmData.k_EnterPlayModeFromMppmEventName, vendorKey: Constants.k_VendorKey)]
    internal class AnalyticsEnterPlayModeFromMppmEvent : AnalyticsEvent<AnalyticsEnterPlayModeFromMppmEvent, EnterPlayModeFromMppmData>
    {
    }
}
