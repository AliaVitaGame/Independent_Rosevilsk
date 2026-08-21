using System;
using Unity.Multiplayer.PlayMode.Analytics.Editor;
using UnityEngine.Analytics;
using UnityEngine.Serialization;

namespace Unity.Multiplayer.PlayMode.Analytics.Editor
{
    [Serializable]
    internal struct OnVirtualPlayerActiveData : IAnalytic.IData
    {
        public const string k_OnVirtualPlayerActiveEventName = "multiplayer_playmode_onVirtualPlayerActive";
        public long LaunchingDurationMs;
    }

    [AnalyticInfo(eventName: OnVirtualPlayerActiveData.k_OnVirtualPlayerActiveEventName, vendorKey: Constants.k_VendorKey)]
    internal class AnalyticsOnVirtualPlayerActiveEvent : AnalyticsEvent<AnalyticsOnVirtualPlayerActiveEvent, OnVirtualPlayerActiveData> { }
}
