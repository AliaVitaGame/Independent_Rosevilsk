using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Multiplay.Authoring.Core.Deployment;
using Unity.Services.Multiplay.Authoring.Core.Model;
using UnityEngine;

namespace Unity.Services.Multiplay.Authoring.Editor.Deployment
{
    class UploadCommand : ServiceCommand
    {
        public override string Name => "Upload";
        public override async Task ExecuteAsync(IEnumerable<DeploymentItem> items, CancellationToken cancellationToken = default)
        {
            try
            {
                using (var scope = Provider.CreateScope())
                {
                    var deployer = scope.GetService<IMultiplayDeployer>();
                    var buildItems = items.OfType<BuildItem>().ToList();

                    await deployer.InitAsync();
                    await deployer.UploadAndSyncBuilds(buildItems, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public override bool IsEnabled(IEnumerable<DeploymentItem> items)
        {
            return items.All(i => i is IResourceRef);
        }

        public override bool IsVisible(IEnumerable<DeploymentItem> items)
        {
            return items.All(i => i is IResourceRef);
        }
    }
}
