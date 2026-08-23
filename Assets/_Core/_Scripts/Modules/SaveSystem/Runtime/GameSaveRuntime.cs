using Modules.CharacterCreator;
using UnityEngine;

namespace Modules.SaveSystem.Runtime
{
    /// <summary>
    /// Autosaves the active slot every 5 minutes and on quit/pause.
    /// Also re-applies character appearance when the local player (re)spawns.
    /// </summary>
    public sealed class GameSaveRuntime : MonoBehaviour
    {
        private const float AutosaveIntervalSeconds = 300f;
        private const float AppearancePollSeconds = 0.5f;

        private IGameSaveService _saveService;
        private float _nextAutosaveTime;
        private float _nextAppearanceCheckTime;
        private bool _quitSaved;
        private Transform _lastAppliedPlayer;

        public void Initialize(IGameSaveService saveService)
        {
            _saveService = saveService;
            _nextAutosaveTime = Time.unscaledTime + AutosaveIntervalSeconds;
            _nextAppearanceCheckTime = Time.unscaledTime + 0.25f;
            _quitSaved = false;
            _lastAppliedPlayer = null;
        }

        private void Update()
        {
            if (_saveService == null || _saveService.ActiveSlotIndex < 0)
                return;

            if (Time.unscaledTime >= _nextAppearanceCheckTime)
            {
                _nextAppearanceCheckTime = Time.unscaledTime + AppearancePollSeconds;
                TryApplyAppearanceToLocalPlayer();
            }

            if (Time.unscaledTime < _nextAutosaveTime)
                return;

            _saveService.CaptureAndSaveActiveSlot();
            _nextAutosaveTime = Time.unscaledTime + AutosaveIntervalSeconds;
        }

        private void TryApplyAppearanceToLocalPlayer()
        {
            var appearance = _saveService.GetActiveAppearance();
            if (appearance == null || !appearance.hasValue)
                return;

            var player = FindLocalPlayerTransform();
            if (player == null)
            {
                _lastAppliedPlayer = null;
                return;
            }

            var hasAppearance = player.Find("AppearanceRoot") != null
                                || (player.Find("Model") != null && player.Find("Model/AppearanceRoot") != null);
            if (hasAppearance && _lastAppliedPlayer == player)
                return;

            var data = CharacterAppearanceCodec.FromSaveData(appearance);
            if (data == null)
                return;

            PlayerAppearanceApplier.ApplyToHost(player, data);
            _lastAppliedPlayer = player;
        }

        private static Transform FindLocalPlayerTransform()
        {
            var players = Object.FindObjectsByType<HEAVYART.TopDownShooter.Netcode.PlayerBehaviour>(
                FindObjectsInactive.Exclude);
            foreach (var player in players)
            {
                if (player != null && player.IsOwner)
                    return player.transform;
            }

            return players is { Length: > 0 } && players[0] != null
                ? players[0].transform
                : null;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                TrySaveOnExit();
        }

        private void OnApplicationQuit() => TrySaveOnExit();

        private void OnDestroy() => TrySaveOnExit();

        private void TrySaveOnExit()
        {
            if (_quitSaved || _saveService == null || _saveService.ActiveSlotIndex < 0)
                return;

            _quitSaved = true;
            _saveService.CaptureAndSaveActiveSlot();
        }
    }
}
