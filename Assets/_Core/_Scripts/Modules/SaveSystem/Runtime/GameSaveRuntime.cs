using Modules.SaveSystem;
using UnityEngine;

namespace Modules.SaveSystem.Runtime
{
    /// <summary>
    /// Autosaves the active slot every 5 minutes and on quit/pause.
    /// </summary>
    public sealed class GameSaveRuntime : MonoBehaviour
    {
        private const float AutosaveIntervalSeconds = 300f;

        private IGameSaveService _saveService;
        private float _nextAutosaveTime;
        private bool _quitSaved;

        public void Initialize(IGameSaveService saveService)
        {
            _saveService = saveService;
            _nextAutosaveTime = Time.unscaledTime + AutosaveIntervalSeconds;
            _quitSaved = false;
        }

        private void Update()
        {
            if (_saveService == null || _saveService.ActiveSlotIndex < 0)
                return;

            if (Time.unscaledTime < _nextAutosaveTime)
                return;

            _saveService.CaptureAndSaveActiveSlot();
            _nextAutosaveTime = Time.unscaledTime + AutosaveIntervalSeconds;
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
