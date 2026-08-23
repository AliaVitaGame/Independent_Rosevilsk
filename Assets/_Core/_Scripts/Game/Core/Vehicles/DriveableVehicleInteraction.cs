using Game.Core.Audio;
using Game.Core.UI;
using HEAVYART.TopDownShooter.Netcode;
using Modules.TargetHints;
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
        private TargetHintTarget _targetHint;
        private Vector3 _cameraOffset;
        private Vector3 _cameraVelocity;
        private Quaternion _cameraRotation;
        private Quaternion _playerRotationBeforeDriving;
        private float _nextInputTime;
        private bool _isDriving;
        private bool _canEnter;
        private bool _inputLocked;
        private bool _cameraLocked;
        private ProjectMusicPlayer _radio;

        public bool IsDriving => _isDriving;
        public bool CanEnter => _canEnter;

        private void Awake()
        {
            _vehicleController = GetComponent<PrometeoCarController>();
            _vehicleRigidbody = GetComponent<Rigidbody>();
            _targetHint = GetComponent<TargetHintTarget>();

            if (!TryGetComponent<VehicleImpactDamage>(out _))
                gameObject.AddComponent<VehicleImpactDamage>();

            if (!TryGetComponent<VehicleHealth>(out _))
                gameObject.AddComponent<VehicleHealth>();

            if (!TryGetComponent<ProjectMusicPlayer>(out _radio))
                _radio = gameObject.AddComponent<ProjectMusicPlayer>();

            try
            {
                EnsurePrometeoSounds();
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"[Vehicle] Could not restore car sounds: {exception.Message}");
            }

            _vehicleController.enabled = false;
            _vehicleController.useTouchControls = false;
        }

        private void Update()
        {
            if (_inputLocked)
                return;

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

        public void SetGameplayLocked(bool locked)
        {
            _inputLocked = locked;
            if (locked)
            {
                if (_isDriving)
                    StopVehicleInput();
                _radio?.SetShouldPlay(false);
                PlayEngine(false);
                return;
            }

            if (_isDriving && _vehicleController != null)
                _vehicleController.enabled = true;
            if (_isDriving)
            {
                _radio?.SetShouldPlay(true);
                PlayEngine(true);
            }
        }

        public void SetCameraLocked(bool locked)
        {
            _cameraLocked = locked;
            if (locked)
                _gameCameraController?.StopCameraMovement();
        }

        private void LateUpdate()
        {
            if (_cameraLocked || !_isDriving || _mainCamera == null)
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
            _targetHint?.SetHintEnabled(false);
            _radio?.SetShouldPlay(true);
            PlayEngine(true);
            GameSfx.PlayEnterVehicle();
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
            _targetHint?.SetHintEnabled(true);
            _radio?.SetShouldPlay(false);
            PlayEngine(false);
            GameSfx.PlayExitVehicle();
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

        private void EnsurePrometeoSounds()
        {
            _vehicleController.useSounds = true;
            _vehicleController.carEngineSound = EnsureSoundSource(
                _vehicleController.carEngineSound,
                "CarEngineSound",
                GameplayUiStyle.LoadClip("Assets/Plugins/PROMETEO - Car Controller/Sounds/CarEngine.wav", "CarEngine"),
                loop: true,
                volume: 0.35f);
            _vehicleController.tireScreechSound = EnsureSoundSource(
                _vehicleController.tireScreechSound,
                "TireScreechSound",
                GameplayUiStyle.LoadClip("Assets/Plugins/PROMETEO - Car Controller/Sounds/TireScreech.wav", "TireScreech"),
                loop: true,
                volume: 0.28f);
        }

        private AudioSource EnsureSoundSource(AudioSource existing, string name, AudioClip clip, bool loop, float volume)
        {
            var source = existing;
            if (source == null)
            {
                var child = transform.Find(name);
                var go = child != null ? child.gameObject : null;
                if (go == null)
                {
                    go = new GameObject(name);
                    go.transform.SetParent(transform, false);
                }

                source = go.GetComponent<AudioSource>();
                if (source == null)
                    source = go.AddComponent<AudioSource>();
            }

            if (source == null)
                return null;

            if (clip != null)
                source.clip = clip;

            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = 0.35f;
            source.volume = volume;
            return source;
        }

        private void PlayEngine(bool play)
        {
            var engine = _vehicleController != null ? _vehicleController.carEngineSound : null;
            var tires = _vehicleController != null ? _vehicleController.tireScreechSound : null;
            if (play)
            {
                if (engine != null && engine.clip != null && !engine.isPlaying)
                    engine.Play();
                return;
            }

            if (engine != null && engine.isPlaying)
                engine.Stop();
            if (tires != null && tires.isPlaying)
                tires.Stop();
        }

        private static GameObject FindLocalPlayer()
        {
            var gameManager = GameManager.Instance;
            if (gameManager == null || gameManager.userControl == null)
                return null;

            var localPlayer = gameManager.userControl.localPlayer;
            return localPlayer != null ? localPlayer.gameObject : null;
        }
    }
}
