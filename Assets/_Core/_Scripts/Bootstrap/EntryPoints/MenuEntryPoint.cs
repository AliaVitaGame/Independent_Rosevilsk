using System;
using Game.Bootstrap.SceneManagement;
using Game.Core.StateMachines;
using Game.UI.MainMenu;
using HEAVYART.TopDownShooter.Netcode;
using Modules.SaveSystem;
using Modules.SaveSystem.UI;
using UnityEngine;
using VContainer.Unity;

namespace Game.Shared
{
    public sealed class MenuEntryPoint : IStartable, IDisposable
    {
        private readonly IStateMachine _stateMachine;
        private readonly IGameSaveService _saveService;

        private MainMenuController _menuController;
        private SaveSlotsPanelController _slotsPanel;
        private SaveSlotsPanelMode _pendingMode;
        private bool _isLoading;

        public MenuEntryPoint(IStateMachine stateMachine, IGameSaveService saveService)
        {
            _stateMachine = stateMachine;
            _saveService = saveService;
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

            EnsureSlotsPanel();

            _menuController.ContinueClicked += OnContinue;
            _menuController.NewGameClicked += OnNewGame;
            _menuController.LoadGameClicked += OnLoadGame;
            _menuController.NpcsClicked += OnNpcs;
            _menuController.SettingsClicked += OnSettings;

            if (_slotsPanel != null)
                _slotsPanel.SlotChosen += OnSlotChosen;

            RefreshMenuSaveButtons();
        }

        public void Dispose()
        {
            if (_menuController != null)
            {
                _menuController.ContinueClicked -= OnContinue;
                _menuController.NewGameClicked -= OnNewGame;
                _menuController.LoadGameClicked -= OnLoadGame;
                _menuController.NpcsClicked -= OnNpcs;
                _menuController.SettingsClicked -= OnSettings;
            }

            if (_slotsPanel != null)
                _slotsPanel.SlotChosen -= OnSlotChosen;
        }

        private void EnsureSlotsPanel()
        {
            _slotsPanel = UnityEngine.Object.FindFirstObjectByType<SaveSlotsPanelController>(FindObjectsInactive.Include);
            if (_slotsPanel == null)
            {
                var canvas = _menuController.GetComponentInParent<Canvas>()
                             ?? UnityEngine.Object.FindFirstObjectByType<Canvas>();
                if (canvas == null)
                {
                    Debug.LogError("[MainMenu] No Canvas found for SaveSlotsPanel.");
                    return;
                }

                var panelGo = new GameObject("SaveSlotsPanel", typeof(RectTransform));
                panelGo.transform.SetParent(canvas.transform, false);
                _slotsPanel = panelGo.AddComponent<SaveSlotsPanelController>();
            }

            _slotsPanel.Initialize(_saveService);
        }

        private void RefreshMenuSaveButtons()
        {
            var hasSaves = _saveService.HasAnySave();
            // Continue only when a save exists. Load stays available to open the slots panel.
            _menuController.SetContinueInteractable(hasSaves);
            _menuController.SetLoadInteractable(true);
        }

        private void OnContinue()
        {
            if (!_saveService.TryBeginContinue(out _))
            {
                RefreshMenuSaveButtons();
                Debug.LogWarning("[MainMenu] Continue: no save found.");
                return;
            }

            StartGame();
        }

        private void OnNewGame()
        {
            if (_slotsPanel == null)
            {
                EnsureSlotsPanel();
                if (_slotsPanel == null)
                    return;
            }

            _pendingMode = SaveSlotsPanelMode.NewGame;
            _slotsPanel.Show(SaveSlotsPanelMode.NewGame);
        }

        private void OnLoadGame()
        {
            if (_slotsPanel == null)
            {
                EnsureSlotsPanel();
                if (_slotsPanel == null)
                    return;
            }

            // Always open the panel. Empty slots stay non-clickable inside Load mode.
            _pendingMode = SaveSlotsPanelMode.LoadGame;
            _slotsPanel.Show(SaveSlotsPanelMode.LoadGame);

            if (!_saveService.HasAnySave())
                Debug.Log("[MainMenu] Load Game: no occupied slots yet.");
        }

        private void OnSlotChosen(int slotIndex)
        {
            if (_pendingMode == SaveSlotsPanelMode.NewGame)
                _saveService.BeginNewGame(slotIndex);
            else
            {
                if (!_saveService.HasAnySave() || _saveService.GetSlot(slotIndex).isEmpty)
                {
                    Debug.LogWarning($"[MainMenu] Load Game: slot {slotIndex + 1} is empty.");
                    return;
                }

                _saveService.BeginLoadGame(slotIndex);
            }

            StartGame();
        }

        private void OnNpcs() { }

        private void OnSettings() { }

        private async void StartGame()
        {
            if (_isLoading)
                return;

            _isLoading = true;
            _menuController.SetInteractable(false);
            _slotsPanel?.Hide();

            var lobbyManager = LobbyManager.Instance;
            if (lobbyManager == null)
            {
                _isLoading = false;
                _menuController.SetInteractable(true);
                RefreshMenuSaveButtons();
                Debug.LogError($"{nameof(LobbyManager)} is missing. Start from Bootstrap.");
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
                _isLoading = false;
                _menuController.SetInteractable(true);
                RefreshMenuSaveButtons();
                Debug.LogException(exception);
            }
        }
    }
}
