using System;
using GameTest;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Shared
{
    public class GameInstaller : LifetimeScope
    {
        protected override void Awake()
        {
            if (string.IsNullOrEmpty(parentReference.TypeName))
                parentReference = ParentReference.Create<ProjectInstaller>();

            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            // Required by GameEntryPoint (used in Editor/dev builds).
            builder.Register<CheatCodeRegistry>(Lifetime.Scoped)
                .As<ICheatCodeRegistry>()
                .AsImplementedInterfaces()
                .AsSelf();

            builder.Register<CheatCodesCanvasBootstrapper>(Lifetime.Scoped)
                .As<ICheatCodesRuntimeUi>()
                .AsImplementedInterfaces()
                .AsSelf();

            builder.RegisterEntryPoint<GameEntryPoint>();
        }
    }
}
