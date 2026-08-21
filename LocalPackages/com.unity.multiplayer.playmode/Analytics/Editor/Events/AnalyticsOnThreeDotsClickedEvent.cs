using System;
using Unity.Multiplayer.PlayMode.Analytics.Editor;
using UnityEngine.Analytics;
using UnityEngine.Serialization;

namespace Unity.Multiplayer.PlayMode.Analytics.Editor
{
    [Serializable]
    internal struct OnThreeDotsClickedData : IAnalytic.IData
    {
        public const string k_OnThreeDotsClickedEventName = "multiplayer_playmode_onThreeDotsClicked";

        public bool IsPlayMode;
    }

    [AnalyticInfo(eventName: OnThreeDotsClickedData.k_OnThreeDotsClickedEventName, vendorKey: Constants.k_VendorKey)]
    internal class AnalyticsOnTreeDotsClickedEvent : AnalyticsEvent<AnalyticsOnTreeDotsClickedEvent, OnThreeDotsClickedData>
    {
    }
}
