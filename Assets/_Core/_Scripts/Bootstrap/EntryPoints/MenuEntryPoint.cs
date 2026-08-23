using System;
using CC;
using Game.Bootstrap.SceneManagement;
using Game.Core.StateMachines;
using Game.UI.MainMenu;
using HEAVYART.TopDownShooter.Netcode;
using Modules.CharacterCreator;
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
        private CharacterCreatorController _characterCreator;
        private SaveSlotsPanelMode _pendingMode;
        private int _pendingSlotIndex = -1;
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
            EnsureCharacterCreator();

            _menuController.ContinueClicked += OnContinue;
            _menuController.NewGameClicked += OnNewGame;
            _menuController.LoadGameClicked += OnLoadGame;
            _menuController.NpcsClicked += OnNpcs;
            _menuController.SettingsClicked += OnSettings;

            if (_slotsPanel != null)
                _slotsPanel.SlotChosen += OnSlotChosen;

            if (_characterCreator != null)
            {
                _characterCreator.Confirmed += OnCharacterConfirmed;
                _characterCreator.Cancelled += OnCharacterCancelled;
            }

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

            if (_characterCreator != null)
            {
                _characterCreator.Confirmed -= OnCharacterConfirmed;
                _characterCreator.Cancelled -= OnCharacterCancelled;
            }
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

        private void EnsureCharacterCreator()
        {
            var sceneCreator =
                UnityEngine.Object.FindFirstObjectByType<CharacterCreatorController>(FindObjectsInactive.Include);

            if (sceneCreator != null)
            {
                if (_characterCreator != null && !ReferenceEquals(_characterCreator, sceneCreator))
                {
                    _characterCreator.Confirmed -= OnCharacterConfirmed;
                    _characterCreator.Cancelled -= OnCharacterCancelled;
                }

                _characterCreator = sceneCreator;
                return;
            }

            if (_characterCreator != null)
                return;

            var go = new GameObject("CharacterCreatorRoot");
            _characterCreator = go.AddComponent<CharacterCreatorController>();
            go.SetActive(false);
        }

        private void RefreshMenuSaveButtons()
        {
            var hasSaves = _saveService.HasAnySave();
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

            _pendingMode = SaveSlotsPanelMode.LoadGame;
            _slotsPanel.Show(SaveSlotsPanelMode.LoadGame);

            if (!_saveService.HasAnySave())
                Debug.Log("[MainMenu] Load Game: no occupied slots yet.");
        }

        private void OnSlotChosen(int slotIndex)
        {
            if (_pendingMode == SaveSlotsPanelMode.NewGame)
            {
                _pendingSlotIndex = slotIndex;
                OpenCharacterCreator(slotIndex);
                return;
            }

            if (!_saveService.HasAnySave() || _saveService.GetSlot(slotIndex).isEmpty)
            {
                Debug.LogWarning($"[MainMenu] Load Game: slot {slotIndex + 1} is empty.");
                return;
            }

            _saveService.BeginLoadGame(slotIndex);
            StartGame();
        }

        private void OpenCharacterCreator(int slotIndex)
        {
            EnsureCharacterCreator();
            if (_characterCreator == null)
            {
                Debug.LogError("[MainMenu] Character creator is missing.");
                return;
            }

            _characterCreator.Confirmed -= OnCharacterConfirmed;
            _characterCreator.Cancelled -= OnCharacterCancelled;
            _characterCreator.Confirmed += OnCharacterConfirmed;
            _characterCreator.Cancelled += OnCharacterCancelled;

            _pendingSlotIndex = slotIndex;
            _menuController.SetInteractable(false);
            _slotsPanel?.Hide();
            _characterCreator.ShowForNewGame(slotIndex);
        }

        private void OnCharacterConfirmed(int slotIndex, CC_CharacterData characterData)
        {
            Debug.Log(
                $"[MainMenu] Character confirmed for slot {slotIndex}: " +
                $"prefab='{characterData?.CharacterPrefab}' name='{characterData?.CharacterName}'.");

            var slot = slotIndex >= 0 ? slotIndex : _pendingSlotIndex;
            if (slot < 0)
            {
                Debug.LogWarning("[MainMenu] Character confirmed without a pending slot.");
                return;
            }

            var appearance = CharacterAppearanceCodec.ToSaveData(characterData);
            _saveService.BeginNewGame(slot, appearance);
            _pendingSlotIndex = -1;
            _characterCreator?.Hide();
            StartGame();
        }

        private void OnCharacterCancelled()
        {
            _pendingSlotIndex = -1;
            _menuController.SetInteractable(true);
            RefreshMenuSaveButtons();
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
            _characterCreator?.Hide();

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
