using System;
using Game.Bootstrap.SceneManagement;
using Game.Core.StateMachines;
using Game.UI.MainMenu;
using HEAVYART.TopDownShooter.Netcode;
using UnityEngine;
using VContainer.Unity;

namespace Game.Shared
{
    public sealed class MenuEntryPoint : IStartable, IDisposable
    {
        private readonly IStateMachine _stateMachine;
        private MainMenuController _menuController;
        private bool _isLoading;

        public MenuEntryPoint(IStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }

        public void Start()
        {
            _menuController = UnityEngine.Object.FindFirstObjectByType<MainMenuController>(FindObjectsInactive.Include);
            if (_menuController == null)
            {
                var canvas = GameObject.Find("Canvas");
                if (canvas != null)
                    _menuController = canvas.AddComponent<MainMenuController>();
            }

            if (_menuController == null)
            {
                Debug.LogError("MainMenuController was not found and could not be created.");
                return;
            }

            _menuController.ContinueClicked += OnContinue;
            _menuController.NewGameClicked += OnNewGame;
            _menuController.LoadGameClicked += OnLoadGame;
            _menuController.NpcsClicked += OnNpcs;
            _menuController.SettingsClicked += OnSettings;
        }

        public void Dispose()
        {
            if (_menuController == null)
                return;

            _menuController.ContinueClicked -= OnContinue;
            _menuController.NewGameClicked -= OnNewGame;
            _menuController.LoadGameClicked -= OnLoadGame;
            _menuController.NpcsClicked -= OnNpcs;
            _menuController.SettingsClicked -= OnSettings;
        }

        private void OnContinue() => StartGame(offlineMode: true);

        private void OnNewGame() => StartGame(offlineMode: true);

        private void OnLoadGame()
        {
            // Save/load pipeline is not implemented yet — enter the game for now.
            StartGame(offlineMode: true);
        }

        private void OnNpcs()
        {
            // Reserved for character / NPC gallery flow.
        }

        private void OnSettings()
        {
            // Visual settings popup is handled inside MainMenuController.
        }

        private async void StartGame(bool offlineMode)
        {
            if (_isLoading)
                return;

            _isLoading = true;
            _menuController.SetInteractable(false);

            var lobbyManager = LobbyManager.Instance;
            if (lobbyManager == null)
            {
                _isLoading = false;
                _menuController.SetInteractable(true);
                Debug.LogError($"{nameof(LobbyManager)} is missing. Start the project from the Bootstrap scene.");
                return;
            }

            lobbyManager.isOfflineMode = offlineMode;

            try
            {
                await _stateMachine.EnterAsync<LoadSceneState, SceneId>(SceneId.Game);
            }
            catch (Exception exception)
            {
                lobbyManager.isOfflineMode = false;
                _isLoading = false;
                _menuController.SetInteractable(true);
                Debug.LogException(exception);
            }
        }
    }
}
