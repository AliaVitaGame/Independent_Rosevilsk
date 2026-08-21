using System;
using System.Diagnostics;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine.Analytics;

namespace Unity.Multiplayer.PlayMode.Analytics.Editor
{
    internal static class AnalyticsEvent
    {
        public static event Action<IAnalytic.IData> AnalyticSent;

        internal static void InvokeAnalyticSent<E, T>(AnalyticsEvent<E, T> evt)
            where T : IAnalytic.IData
            where E : AnalyticsEvent<E, T>, new()
        {
            evt.TryGatherData(out IAnalytic.IData data, out Exception error);

            AnalyticSent?.Invoke(data);
        }
    }

    internal abstract class AnalyticsEvent<E, T> : IAnalytic
        where T : IAnalytic.IData
        where E : AnalyticsEvent<E, T>, new()
    {
        private T m_Data;
        public static void Send(T data)
        {
            var analytic = new E { m_Data = data };
            EditorAnalytics.SendAnalytic(analytic);

            AnalyticsEvent.InvokeAnalyticSent(analytic);
            DebugAnalytics($"Data Name: {data.GetType()} - Data: {JsonConvert.SerializeObject(data)}");
        }

        public bool TryGatherData(out IAnalytic.IData data, out Exception error)
        {
            data = m_Data;
            error = null;
            return true;
        }

        [Conditional("UNITY_MULTIPLAYER_PLAYMODE_ANALYTICS_DEBUG")]
        private static void DebugAnalytics(object message) => UnityEngine.Debug.Log(message);
    }
}
