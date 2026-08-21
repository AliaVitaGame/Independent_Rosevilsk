using System;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Multiplay.Authoring.Core.Model;
using Unity.Services.Multiplay.Authoring.Editor.Shared.Analytics;

namespace Unity.Services.Multiplay.Authoring.Editor.Analytics
{
    class DeployAnalytics : IDeployAnalytics
    {
        readonly IAnalyticsUtils m_AnalyticsUtils;

        public DeployAnalytics(IAnalyticsUtils analyticsUtils)
        {
            m_AnalyticsUtils = analyticsUtils;
        }

        public void ItemDeployed(IDeploymentItem deploymentItem)
        {
            var contextText = "unknown";
            switch (deploymentItem)
            {
                case BuildItem:
                    contextText = "build";
                    break;
                case BuildConfigurationItem:
                    contextText = "buildConfiguration";
                    break;
                case FleetItem:
                    contextText = "fleet";
                    break;
            }

            var success = deploymentItem.Status.MessageSeverity == SeverityLevel.Success;
            m_AnalyticsUtils.SendCommonEvent(new ICommonAnalytics.CommonEventPayload()
            {
                action = "multiplay_file_deployed",
                context = contextText,
                exception = success
                    ? "success"
                    : "fail",
            });
        }
    }
}
