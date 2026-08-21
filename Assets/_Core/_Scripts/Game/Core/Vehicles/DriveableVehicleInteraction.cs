using HEAVYART.TopDownShooter.Netcode;
using UnityEngine;

namespace Game.Core.Vehicles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PrometeoCarController))]
    public sealed class DriveableVehicleInteraction : MonoBehaviour
    {
        private const float InteractionDistance = 4.5f;
        private const float InputCooldown = 0.25f;
        private const float CameraSmoothTime = 0.12f;
        private const float VehicleCameraDistanceMultiplier = 1.5f;
        private static readonly Vector3 ExitOffset = new(3f, 0.5f, 0f);

        private PrometeoCarController _vehicleController;
        private Rigidbody _vehicleRigidbody;
        private GameObject _localPlayer;
        private Rigidbody _playerRigidbody;
        private Camera _mainCamera;
        private GameCameraController _gameCameraController;
        private Vector3 _cameraOffset;
        private Vector3 _cameraVelocity;
        private Quaternion _cameraRotation;
        private Quaternion _playerRotationBeforeDriving;
        private float _nextInputTime;
        private bool _isDriving;
        private bool _canEnter;
        private GUIStyle _promptStyle;

        public bool IsDriving => _isDriving;

        private void Awake()
        {
            _vehicleController = GetComponent<PrometeoCarController>();
            _vehicleRigidbody = GetComponent<Rigidbody>();

            if (!TryGetComponent<VehicleImpactDamage>(out _))
                gameObject.AddComponent<VehicleImpactDamage>();

            _vehicleController.enabled = false;
            _vehicleController.useTouchControls = false;
        }

        private void Update()
        {
            if (_isDriving)
            {
                if (Time.unscaledTime >= _nextInputTime && Input.GetKeyDown(KeyCode.E))
                    ExitVehicle();

                return;
            }

            _localPlayer = FindLocalPlayer();
            _canEnter = _localPlayer != null
                && Vector3.Distance(_localPlayer.transform.position, transform.position) <= InteractionDistance;

            if (_canEnter && Time.unscaledTime >= _nextInputTime && Input.GetKeyDown(KeyCode.E))
                EnterVehicle();
        }

        private void LateUpdate()
        {
            if (!_isDriving || _mainCamera == null)
                return;

            var targetPosition = transform.position + _cameraOffset;
            _mainCamera.transform.position = Vector3.SmoothDamp(
                _mainCamera.transform.position,
                targetPosition,
                ref _cameraVelocity,
                CameraSmoothTime);
            _mainCamera.transform.rotation = _cameraRotation;
        }

        private void EnterVehicle()
        {
            _playerRotationBeforeDriving = _localPlayer.transform.rotation;
            _playerRigidbody = _localPlayer.GetComponent<Rigidbody>();
            StopPlayerMovement();

            _mainCamera = Camera.main;
            if (_mainCamera != null)
            {
                _gameCameraController = _mainCamera.GetComponent<GameCameraController>();
                _gameCameraController?.StopCameraMovement();
                var normalCameraOffset = _mainCamera.transform.position - _localPlayer.transform.position;
                _cameraOffset = normalCameraOffset * VehicleCameraDistanceMultiplier;
                _cameraRotation = _mainCamera.transform.rotation;
                _cameraVelocity = Vector3.zero;
            }

            _localPlayer.SetActive(false);
            _vehicleController.enabled = true;
            _isDriving = true;
            _canEnter = false;
            _nextInputTime = Time.unscaledTime + InputCooldown;
        }

        private void ExitVehicle()
        {
            StopVehicleInput();

            var exitPosition = transform.TransformPoint(ExitOffset);
            _localPlayer.transform.SetPositionAndRotation(exitPosition, _playerRotationBeforeDriving);
            _localPlayer.SetActive(true);

            if (_playerRigidbody != null)
            {
                _playerRigidbody.isKinematic = false;
                _playerRigidbody.position = exitPosition;
                _playerRigidbody.rotation = _playerRotationBeforeDriving;
                _playerRigidbody.linearVelocity = Vector3.zero;
                _playerRigidbody.angularVelocity = Vector3.zero;
            }

            _gameCameraController?.ActivateCameraMovement();
            _isDriving = false;
            _nextInputTime = Time.unscaledTime + InputCooldown;
        }

        private void StopPlayerMovement()
        {
            if (_playerRigidbody == null)
                return;

            _playerRigidbody.linearVelocity = Vector3.zero;
            _playerRigidbody.angularVelocity = Vector3.zero;
            _playerRigidbody.isKinematic = true;
        }

        private void StopVehicleInput()
        {
            _vehicleController.ThrottleOff();
            _vehicleController.Brakes();
            _vehicleController.enabled = false;

            if (_vehicleRigidbody != null)
                _vehicleRigidbody.angularVelocity = Vector3.zero;
        }

        private static GameObject FindLocalPlayer()
        {
            var gameManager = GameManager.Instance;
            if (gameManager == null || gameManager.userControl == null)
                return null;

            var localPlayer = gameManager.userControl.localPlayer;
            return localPlayer != null ? localPlayer.gameObject : null;
        }

        private void OnGUI()
        {
            if (!_isDriving && !_canEnter)
                return;

            _promptStyle ??= new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                normal = { textColor = Color.white }
            };

            var text = _isDriving ? "E — выйти из машины" : "E — сесть в машину";
            var rect = new Rect((Screen.width - 320f) * 0.5f, Screen.height - 100f, 320f, 42f);
            GUI.Label(rect, text, _promptStyle);
        }
    }
}
