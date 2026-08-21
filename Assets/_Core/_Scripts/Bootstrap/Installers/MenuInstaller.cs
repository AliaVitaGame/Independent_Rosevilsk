using VContainer;
using VContainer.Unity;

namespace Game.Shared
{
    public class MenuInstaller : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<MenuEntryPoint>();
        }
    }
}
