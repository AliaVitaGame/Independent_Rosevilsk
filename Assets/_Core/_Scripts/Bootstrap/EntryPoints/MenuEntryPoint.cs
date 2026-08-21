using System;
using Game.Bootstrap.SceneManagement;
using Game.Core.StateMachines;
using HEAVYART.TopDownShooter.Netcode;
using UnityEngine;
using UnityEngine.UI;
using VContainer.Unity;

namespace Game.Shared
{
    public sealed class MenuEntryPoint : IStartable, IDisposable
    {
        private const string StartButtonName = "StartButton";

        private readonly IStateMachine _stateMachine;
        private Button _startButton;

        public MenuEntryPoint(IStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }

        public void Start()
        {
            var buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            _startButton = Array.Find(buttons, button => button.name == StartButtonName);

            if (_startButton == null)
            {
                Debug.LogError($"Menu button '{StartButtonName}' was not found.");
                return;
            }

            _startButton.onClick.AddListener(LoadGame);
        }

        public void Dispose()
        {
            if (_startButton != null)
                _startButton.onClick.RemoveListener(LoadGame);
        }

        private async void LoadGame()
        {
            _startButton.interactable = false;
            var lobbyManager = LobbyManager.Instance;

            if (lobbyManager == null)
            {
                _startButton.interactable = true;
                Debug.LogError($"{nameof(LobbyManager)} is missing. Start the project from the Bootstrap scene.");
                return;
            }

            lobbyManager.isOfflineMode = true;

            try
            {
                await _stateMachine.EnterAsync<LoadSceneState, SceneId>(SceneId.Game);
            }
            catch (Exception exception)
            {
                lobbyManager.isOfflineMode = false;
                _startButton.interactable = true;
                Debug.LogException(exception);
            }
        }
    }
}
