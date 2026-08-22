using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Shared
{
    public class MenuInstaller : LifetimeScope
    {
        protected override void Awake()
        {
            // Nest under ProjectInstaller (DontDestroyOnLoad) for IGameSaveService / IStateMachine.
            if (string.IsNullOrEmpty(parentReference.TypeName))
                parentReference = ParentReference.Create<ProjectInstaller>();

            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<MenuEntryPoint>();
        }
    }
}
