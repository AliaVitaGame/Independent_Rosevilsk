using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Multiplay.Authoring.Core.Deployment;
using Unity.Services.Multiplay.Authoring.Editor.Shared.Logging;
using Unity.Services.Multiplay.Authoring.Editor.Analytics;

namespace Unity.Services.Multiplay.Authoring.Editor.Deployment
{
    class DeployCommand : ServiceCommand
    {
        bool m_DeployInProgres;
        public override string Name => "Deploy";

        readonly IDeployAnalytics m_DeployAnalytics;

        public DeployCommand(IDeployAnalytics deployAnalytics)
        {
            m_DeployAnalytics = deployAnalytics;
        }

        public override async Task ExecuteAsync(IEnumerable<DeploymentItem> items, CancellationToken cancellationToken = default)
        {
            if (m_DeployInProgres)
            {
                Logger.LogVerbose($"[{nameof(DeployCommand)}] Skipping Deploy, one in progress already");
                return;
            }

            m_DeployInProgres = true;
            var deploymentItems = items.ToList();
            try
            {
                using var scope = Provider.CreateScope();
                var deployer = scope.GetService<IMultiplayDeployer>();

                await deployer.InitAsync();
                await deployer.Deploy(deploymentItems, cancellationToken);
            }
            catch (Exception e)
            {
                deploymentItems.ForEach(i =>
                    i.Status = new DeploymentStatus(e.Message, messageSeverity: SeverityLevel.Error));
                throw;
            }
            finally
            {
                m_DeployInProgres = false;
                deploymentItems.ForEach(i => m_DeployAnalytics.ItemDeployed(i));
            }
        }
    }
}
