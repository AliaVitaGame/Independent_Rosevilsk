using GameTest;
using Shared.Providers;
using VContainer;
using VContainer.Unity;
using UnityEngine;

namespace Game.Shared
{
    public class GameInstaller : LifetimeScope
    {

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ICheatCodeRegistry, CheatCodeRegistry>(Lifetime.Singleton);
            builder.Register<ICheatCodesRuntimeUi, CheatCodesCanvasBootstrapper>(Lifetime.Singleton);
            builder.RegisterEntryPoint<GameEntryPoint>();
            Debug.Log("GameInstaller configured");
        }
    }
}
