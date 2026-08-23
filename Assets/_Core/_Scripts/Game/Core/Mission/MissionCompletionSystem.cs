using Cysharp.Threading.Tasks;
using Game.Core.StateMachines;
using Game.Core.Vehicles;
using Game.Shared;
using HEAVYART.TopDownShooter.Netcode;
using Modules.SaveSystem;
using Modules.SaveSystem.Runtime;
using Modules.TargetHints;
using PrimeTween;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Core.Mission
{
    /// <summary>
    /// Completes the escape when the player's vehicle is close to ExitPoint.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MissionCompletionSystem : MonoBehaviour
    {
        [Header("Bindings")]
        [SerializeField] private Transform _exitPoint;
        [SerializeField] private DriveableVehicleInteraction _vehicle;
        [SerializeField] private MissionCompleteUI _ui;
        [SerializeField] private TargetHintTarget _exitHint;
        [SerializeField] private Camera _cinematicCamera;

        [Header("Arrival")]
        [SerializeField] private float _arrivalRadius = 8f;
        [SerializeField] private float _repromptCooldown = 4f;
        [SerializeField] private Color _gizmoColor = new(0.15f, 0.95f, 0.35f, 0.9f);
        [SerializeField] private bool _drawArrivalGizmo = true;

        public float ArrivalRadius => _arrivalRadius;

        [Header("Cinematic")]
        [SerializeField] private Vector3 _cityCenter = new(-80f, 0f, 40f);
        [SerializeField] private float _pullDuration = 6.5f;
        [SerializeField] private float _pullHeight = 170f;
        [SerializeField] private float _pullDistance = 150f;
        [SerializeField] private float _endingHoldDuration = 2.4f;

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
            if (_completed || _promptOpen)
                return;

            ResolveBindings();
            if (_exitPoint == null || _vehicle == null)
                return;

            var inZone = PlanarDistance(_vehicle.transform.position, _exitPoint.position) <= _arrivalRadius;
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

        private void ResolveBindings()
        {
            if (_exitPoint == null)
            {
                var exitGo = GameObject.Find("ExitPoint");
                if (exitGo != null)
                    _exitPoint = exitGo.transform;
            }

            if (_vehicle == null)
                _vehicle = FindFirstObjectByType<DriveableVehicleInteraction>();
        }

        private void OpenPrompt()
        {
            _promptOpen = true;
            _vehicle.SetGameplayLocked(true);
            if (_ui == null)
                _ui = FindFirstObjectByType<MissionCompleteUI>(FindObjectsInactive.Include);
            _ui?.ShowConfirm(OnConfirmEscape, OnCancelEscape);
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
            DisableWorldForEnding();
            PlayEscapeCinematic();
        }

        private void DisableWorldForEnding()
        {
            LocationPressureSystem.Instance?.Halt();

            var timer = FindFirstObjectByType<LocationTimerUI>(FindObjectsInactive.Include);
            timer?.HideForEnding();

            var hud = FindFirstObjectByType<Game.Core.UI.GameplayHudController>(FindObjectsInactive.Include);
            hud?.HideForEnding();

            var characterUi = FindObjectsByType<CharacterUIController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < characterUi.Length; i++)
                characterUi[i].enabled = false;

            var statusHud = FindFirstObjectByType<HUDController>(FindObjectsInactive.Include);
            if (statusHud != null)
            {
                if (statusHud.healthStatusSlider != null)
                    statusHud.healthStatusSlider.gameObject.SetActive(false);
                if (statusHud.gameTimerTextComponent != null)
                    statusHud.gameTimerTextComponent.gameObject.SetActive(false);
                statusHud.gameObject.SetActive(false);
            }

            if (GameManager.Instance != null && GameManager.Instance.UI != null && GameManager.Instance.UI.statusBarsContainer != null)
                GameManager.Instance.UI.statusBarsContainer.gameObject.SetActive(false);

            if (SettingsManager.Instance != null && SettingsManager.Instance.gameplay != null)
                SettingsManager.Instance.gameplay.botsCount = 0;

            var userControl = GameManager.Instance != null ? GameManager.Instance.userControl : null;
            if (userControl == null)
                return;

            for (var i = userControl.aiSceneObjects.Count - 1; i >= 0; i--)
            {
                var ai = userControl.aiSceneObjects[i];
                if (ai != null)
                    ai.gameObject.SetActive(false);
            }
        }

        private void PlayEscapeCinematic()
        {
            var camera = _cinematicCamera != null ? _cinematicCamera : Camera.main;
            _ui?.PlayEndingOverlay();
            if (camera == null)
            {
                Tween.Delay(_endingHoldDuration + 2.4f).OnComplete(FinishMissionAndReturnToMenu);
                return;
            }

            _vehicle.SetGameplayLocked(true);
            _vehicle.SetCameraLocked(true);
            var cameraController = camera.GetComponent<GameCameraController>();
            if (cameraController != null)
                cameraController.enabled = false;

            var lookAt = _vehicle != null
                ? _vehicle.transform.position + Vector3.up * 2f
                : _cityCenter + Vector3.up * 8f;
            var away = camera.transform.position - lookAt;
            away.y = 0f;
            if (away.sqrMagnitude < 1f)
                away = -camera.transform.forward;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f)
                away = Vector3.back;
            away.Normalize();

            var targetPos = lookAt + away * _pullDistance + Vector3.up * _pullHeight;
            var lookRotation = Quaternion.LookRotation((lookAt - targetPos).normalized, Vector3.up);

            _flySequence.Stop();
            _flySequence = Sequence.Create()
                .Chain(Tween.Position(camera.transform, targetPos, _pullDuration, Ease.InOutSine))
                .Group(Tween.Rotation(camera.transform, lookRotation, _pullDuration, Ease.InOutSine))
                .ChainDelay(_endingHoldDuration)
                .OnComplete(FinishMissionAndReturnToMenu);
        }

        private void FinishMissionAndReturnToMenu()
        {
            SaveProgress();
            ReturnToMenu();
        }

        private static void SaveProgress()
        {
            var save = Resolve<IGameSaveService>();
            if (save != null && save.ActiveSlotIndex >= 0)
            {
                save.CaptureAndSaveActiveSlot();
                return;
            }

            var runtime = Object.FindFirstObjectByType<GameSaveRuntime>();
            runtime?.SaveNow();
        }

        private static void ReturnToMenu()
        {
            var stateMachine = Resolve<IStateMachine>();
            if (stateMachine != null)
            {
                stateMachine.EnterAsync<MenuState>().Forget();
                return;
            }

            SceneManager.LoadScene("MainMenu");
        }

        private static T Resolve<T>() where T : class
        {
            try
            {
                var project = Object.FindFirstObjectByType<ProjectInstaller>();
                if (project != null && project.Container != null)
                    return project.Container.Resolve(typeof(T)) as T;
            }
            catch
            {
                // Play-from-scene has no project scope.
            }

            return null;
        }

        private void OnDrawGizmos()
        {
            DrawArrivalGizmo();
        }

        private void OnDrawGizmosSelected()
        {
            DrawArrivalGizmo();
        }

        private void DrawArrivalGizmo()
        {
            if (!_drawArrivalGizmo)
                return;

            var point = _exitPoint != null ? _exitPoint : transform;
            if (point == null)
                return;

            var center = point.position + Vector3.up * 0.08f;
            Gizmos.color = _gizmoColor;
            Gizmos.DrawWireSphere(center, _arrivalRadius);
            DrawCircle(center, _arrivalRadius, 48);
            Gizmos.color = new Color(_gizmoColor.r, _gizmoColor.g, _gizmoColor.b, 0.12f);
            Gizmos.DrawSphere(center, _arrivalRadius);
        }

        private static void DrawCircle(Vector3 center, float radius, int segments)
        {
            var prev = center + new Vector3(radius, 0f, 0f);
            for (var i = 1; i <= segments; i++)
            {
                var angle = i * Mathf.PI * 2f / segments;
                var next = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
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
