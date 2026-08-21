using VContainer;
using VContainer.Unity;

namespace Game.Shared
{
    public class BootstrapInstaller : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<BootstrapEntryPoint>();
        }
    }
}