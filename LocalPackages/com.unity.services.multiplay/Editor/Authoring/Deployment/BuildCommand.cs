using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Multiplay.Authoring.Core.Deployment;
using Unity.Services.Multiplay.Authoring.Core.Model;
using Unity.Services.Multiplay.Authoring.Editor.Shared.Collections;

namespace Unity.Services.Multiplay.Authoring.Editor.Deployment
{
    class BuildCommand : ServiceCommand
    {
        public override string Name => "Build";

        public override async Task ExecuteAsync(IEnumerable<DeploymentItem> items, CancellationToken cancellationToken = default)
        {
            var deploymentItems = items as IReadOnlyList<DeploymentItem> ?? items.ToList();
            try
            {
                using (var scope = Provider.CreateScope())
                {
                    var buildItems = deploymentItems.OfType<BuildItem>().ToList();
                    var deployer = scope.GetService<IMultiplayDeployer>();

                    await deployer.InitAsync();
                    await deployer.BuildBinaries(buildItems, cancellationToken);
                }
            }
            catch (Exception e)
            {
                deploymentItems.ForEach(i => i.Status = new DeploymentStatus(e.Message, messageSeverity: SeverityLevel.Error));
                throw;
            }
        }

        public override bool IsEnabled(IEnumerable<DeploymentItem> items)
        {
            return items.All(i => i is BuildItem);
        }

        public override bool IsVisible(IEnumerable<DeploymentItem> items)
        {
            return items.All(i => i is BuildItem);
        }
    }
}
