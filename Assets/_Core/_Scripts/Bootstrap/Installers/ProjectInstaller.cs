using Game.Bootstrap.SceneManagement;
using Game.Core.StateMachines;
using Game.Meta.Analytics;
using Game.Meta.Lifecycle;
using Modules.Audio;
using Modules.Haptics;
using Modules.SaveSystem;
using Modules.SaveSystem.Services;
using Modules.Settings;
using Shared.Providers;
using Shared.UI.Loading;
using Systems.CurrencySystem;
using Systems.CurrencySystem.Interfaces;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;
using VContainer.Unity;

namespace Game.Shared
{
    public class ProjectInstaller : LifetimeScope
    {
        [FormerlySerializedAs("loadingPanel")]
        [SerializeField] private LoadingPanelView loadingPanelPrefab;
        [SerializeField] private SettingsProvider settingsProvider;

        protected override void Awake()
        {
            DontDestroyOnLoad(gameObject);
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(settingsProvider).AsImplementedInterfaces();
            builder.RegisterComponentInNewPrefab(loadingPanelPrefab, Lifetime.Singleton)
                .DontDestroyOnLoad()
                .As<ILoadingPanelView>();

            builder.Register<ICurrencyService, CurrencyService>(Lifetime.Singleton);
            builder.Register<IPlayerSaveService, NoOpPlayerSaveService>(Lifetime.Singleton);
            builder.Register<IGameSaveService, GameSaveService>(Lifetime.Singleton);

            builder.Register<ISceneCatalog, SceneCatalog>(Lifetime.Singleton);
            builder.Register<ISceneLoader, SceneLoaderService>(Lifetime.Singleton);
            builder.Register<ILoadingPanelService, LoadingPanelService>(Lifetime.Singleton);
            builder.RegisterComponentOnNewGameObject<ApplicationStateService>(Lifetime.Singleton, "Application State Service")
                .UnderTransform(transform)
                .As<IAppStateService>();
            builder.Register<BootstrapState>(Lifetime.Singleton);
            builder.Register<MenuState>(Lifetime.Singleton);
            builder.Register<LoadSceneState>(Lifetime.Singleton);
            builder.Register<PauseState>(Lifetime.Singleton);
            builder.Register<WinState>(Lifetime.Singleton);
            builder.Register<LoseState>(Lifetime.Singleton);
            builder.Register<IStateMachine, StateMachine>(Lifetime.Singleton);
            builder.Register<IServiceInitializer, ServiceInitializer>(Lifetime.Singleton);
            builder.Register<ISettingsService, RuntimeSettingsService>(Lifetime.Singleton);
            builder.Register<ISoundService, NoOpSoundService>(Lifetime.Singleton);
            builder.Register<IHapticsService, NoOpHapticsService>(Lifetime.Singleton);
            builder.Register<IAnalyticsService, NoOpAnalyticsService>(Lifetime.Singleton);
            builder.Register<IServicePreloader, AnalyticsLifecycleReporter>(Lifetime.Singleton);

            Debug.Log("ProjectInstaller configured");
        }
    }
}
