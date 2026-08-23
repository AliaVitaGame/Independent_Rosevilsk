using Game.Core.Passengers;
using Game.Core.Vehicles;
using Modules.TargetHints;
using PrimeTween;
using UnityEngine;

namespace Game.Core.Mission
{
    /// <summary>
    /// Completes the escape when the player drives a full vehicle to ExitPoint.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MissionCompletionSystem : MonoBehaviour
    {
        [Header("Bindings")]
        [SerializeField] private Transform _exitPoint;
        [SerializeField] private PassengerPickupSystem _pickupSystem;
        [SerializeField] private DriveableVehicleInteraction _vehicle;
        [SerializeField] private MissionCompleteUI _ui;
        [SerializeField] private TargetHintTarget _exitHint;
        [SerializeField] private Camera _cinematicCamera;

        [Header("Arrival")]
        [SerializeField] private float _arrivalRadius = 22f;
        [SerializeField] private float _repromptCooldown = 4f;

        [Header("Cinematic")]
        [SerializeField] private Vector3 _cityCenter = new(-80f, 0f, 40f);
        [SerializeField] private float _flyHeight = 95f;
        [SerializeField] private float _flyRadius = 220f;
        [SerializeField] private float _segmentDuration = 3.2f;
        [SerializeField] private int _segmentCount = 3;

        private bool _promptOpen;
        private bool _completed;
        private bool _insideZone;
        private float _nextPromptTime;
        private Sequence _flySequence;

        private void Awake()
        {
            if (_exitPoint == null)
            {
                var exitGo = GameObject.Find("ExitPoint");
                if (exitGo != null)
                    _exitPoint = exitGo.transform;
            }

            if (_pickupSystem == null)
                _pickupSystem = FindFirstObjectByType<PassengerPickupSystem>();

            if (_vehicle == null)
                _vehicle = FindFirstObjectByType<DriveableVehicleInteraction>();

            if (_exitHint == null && _exitPoint != null)
            {
                _exitHint = _exitPoint.GetComponent<TargetHintTarget>();
                if (_exitHint == null)
                    _exitHint = _exitPoint.gameObject.AddComponent<TargetHintTarget>();
            }

            _exitHint?.SetKind(TargetHintKind.Exit);

            if (_ui == null)
                _ui = FindFirstObjectByType<MissionCompleteUI>(FindObjectsInactive.Include);

            if (_ui == null)
            {
                var canvas = FindGameplayCanvas();
                var host = canvas != null ? canvas.gameObject : gameObject;
                _ui = host.GetComponent<MissionCompleteUI>() ?? host.AddComponent<MissionCompleteUI>();
            }
        }

        private void OnDestroy()
        {
            _flySequence.Stop();
        }

        private void Update()
        {
            if (_completed || _promptOpen || _exitPoint == null || _vehicle == null || _pickupSystem == null)
                return;

            if (!_vehicle.IsDriving || !_pickupSystem.IsFull)
            {
                _insideZone = false;
                return;
            }

            var distance = Vector3.Distance(_vehicle.transform.position, _exitPoint.position);
            var inZone = distance <= _arrivalRadius;
            if (!inZone)
            {
                _insideZone = false;
                return;
            }

            if (_insideZone || Time.unscaledTime < _nextPromptTime)
                return;

            _insideZone = true;
            OpenPrompt();
        }

        private void OpenPrompt()
        {
            _promptOpen = true;
            _vehicle.SetGameplayLocked(true);
            _ui.ShowConfirm(OnConfirmEscape, OnCancelEscape);
        }

        private void OnCancelEscape()
        {
            _promptOpen = false;
            _nextPromptTime = Time.unscaledTime + _repromptCooldown;
            _vehicle.SetGameplayLocked(false);
        }

        private void OnConfirmEscape()
        {
            _promptOpen = false;
            _completed = true;
            PlayEscapeCinematic();
        }

        private void PlayEscapeCinematic()
        {
            var camera = _cinematicCamera != null ? _cinematicCamera : Camera.main;
            if (camera == null)
            {
                _ui.PlayEndingOverlay();
                return;
            }

            _vehicle.SetGameplayLocked(true);
            _vehicle.SetCameraLocked(true);

            var lookAt = _cityCenter + Vector3.up * 8f;
            var startYaw = Mathf.Atan2(camera.transform.position.x - _cityCenter.x, camera.transform.position.z - _cityCenter.z);
            _flySequence.Stop();
            _flySequence = Sequence.Create();

            for (var i = 1; i <= _segmentCount; i++)
            {
                var yaw = startYaw + i * (Mathf.PI * 2f / _segmentCount);
                var targetPos = new Vector3(
                    _cityCenter.x + Mathf.Sin(yaw) * _flyRadius,
                    _flyHeight,
                    _cityCenter.z + Mathf.Cos(yaw) * _flyRadius);
                var lookRotation = Quaternion.LookRotation((lookAt - targetPos).normalized, Vector3.up);
                var duration = _segmentDuration;

                _flySequence.Chain(Tween.Position(camera.transform, targetPos, duration, Ease.InOutSine));
                _flySequence.Group(Tween.Rotation(camera.transform, lookRotation, duration, Ease.InOutSine));
            }

            var fadeDelay = Mathf.Max(0.2f, _segmentCount * _segmentDuration - 1.6f);
            Tween.Delay(fadeDelay).OnComplete(() => _ui.PlayEndingOverlay());
        }

        private static Canvas FindGameplayCanvas()
        {
            var inGameUi = GameObject.Find("InGameUI");
            if (inGameUi != null)
                return inGameUi.GetComponent<Canvas>();

            return FindFirstObjectByType<Canvas>();
        }
    }
}
