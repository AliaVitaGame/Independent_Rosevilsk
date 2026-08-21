using Newtonsoft.Json;
using Unity.Services.Multiplay.Authoring.Editor.Analytics;
using Unity.Services.Multiplay.Authoring.Editor.Shared.Analytics;
using UnityEditor;
using UnityEngine;

namespace Unity.Services.Multiplay.Authoring.Editor
{
    class BuildsAnalytics : IBuildsAnalytics
    {
        readonly IAnalyticsUtils m_AnalyticsUtils;

        public BuildsAnalytics(IAnalyticsUtils analyticsUtils)
        {
            m_AnalyticsUtils = analyticsUtils;
        }

        public AnalyticsTimer BeginBuild()
        {
            return new AnalyticsTimer(_ => {});
        }

        public void BuildDetails(BuildTarget originalBuildTarget, StandaloneBuildSubtarget buildSubtarget, BuildTarget serverBuildTarget)
        {
            m_AnalyticsUtils.SendCommonEvent(new ICommonAnalytics.CommonEventPayload()
            {
                action = "multiplay_build",
                context = JsonConvert.SerializeObject(new BuildInformation()
                {
                    OriginalBuildTarget = originalBuildTarget.ToString(),
                    BuildSubtarget = buildSubtarget.ToString(),
                    ServerBuildTarget = serverBuildTarget.ToString()
                })
            });
        }

        class BuildInformation
        {
            public string OriginalBuildTarget;
            public string BuildSubtarget;
            public string ServerBuildTarget;
        }
    }
}
