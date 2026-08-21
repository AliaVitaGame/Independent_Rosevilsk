using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Multiplay.Authoring.Editor.Analytics
{
    interface IDeployAnalytics
    {
        void ItemDeployed(IDeploymentItem item);
    }
}
