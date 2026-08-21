using Unity.Services.Multiplay.Authoring.Core.Assets;

namespace Unity.Services.Multiplay.Authoring.Core.Deployment
{
    record DeployProgress(IResourceName Name, string Status, float Percentage);
}
