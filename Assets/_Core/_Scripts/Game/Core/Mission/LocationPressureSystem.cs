using System.Collections;
using System.Collections.Generic;
using HEAVYART.TopDownShooter.Netcode;
using UnityEngine;

namespace Game.Core.Mission
{
    /// <summary>
    /// Raises bot count, spawn frequency and bot stats the longer the player stays.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocationPressureSystem : MonoBehaviour
    {
        public static LocationPressureSystem Instance { get; private set; }

        [SerializeField] private float _durationSeconds = 300f;
        [SerializeField] private int _maxBotsCount = 18;
        [SerializeField] private float _minSpawnRate = 0.7f;
        [SerializeField] private float _maxHealthMultiplier = 3f;
        [SerializeField] private float _maxSpeedMultiplier = 1.7f;
        [SerializeField] private float _maxEngageDistanceMultiplier = 1.6f;

        private readonly List<AiBaseline> _aiBaselines = new();
        private int _baseBotsCount;
        private float _baseSpawnRate;
        private bool _captured;
        private Coroutine _extraSpawnRoutine;

        public float DurationSeconds => _durationSeconds;
        public float ElapsedSeconds { get; private set; }
        public float Progress => _durationSeconds <= 0.01f
            ? 1f
            : Mathf.Clamp01(ElapsedSeconds / _durationSeconds);

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            _extraSpawnRoutine = StartCoroutine(ExtraSpawnLoop());
        }

        private void OnDisable()
        {
            if (_extraSpawnRoutine != null)
            {
                StopCoroutine(_extraSpawnRoutine);
                _extraSpawnRoutine = null;
            }

            RestoreBaselines();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            RestoreBaselines();
        }

        private void Update()
        {
            ElapsedSeconds += Time.deltaTime;
            TryCaptureBaselines();
            ApplyPressure();
        }

        private void TryCaptureBaselines()
        {
            if (_captured || SettingsManager.Instance == null || SettingsManager.Instance.gameplay == null)
                return;

            var gameplay = SettingsManager.Instance.gameplay;
            _baseBotsCount = Mathf.Max(1, gameplay.botsCount);
            _baseSpawnRate = Mathf.Max(0.5f, gameplay.botsSpawnRate);

            _aiBaselines.Clear();
            var configs = SettingsManager.Instance.ai != null ? SettingsManager.Instance.ai.configs : null;
            if (configs != null)
            {
                for (var i = 0; i < configs.Count; i++)
                {
                    var config = configs[i];
                    if (config == null)
                        continue;

                    _aiBaselines.Add(new AiBaseline
                    {
                        Config = config,
                        Health = config.health,
                        MovementSpeed = config.movementSpeed,
                        DistanceToOpenFire = config.distanceToOpenFire
                    });
                }
            }

            _captured = true;
        }

        private void ApplyPressure()
        {
            if (!_captured || SettingsManager.Instance == null)
                return;

            var t = Progress;
            var gameplay = SettingsManager.Instance.gameplay;
            gameplay.botsCount = Mathf.RoundToInt(Mathf.Lerp(_baseBotsCount, _maxBotsCount, t));
            gameplay.botsSpawnRate = Mathf.Lerp(_baseSpawnRate, _minSpawnRate, t);

            var healthMul = Mathf.Lerp(1f, _maxHealthMultiplier, t);
            var speedMul = Mathf.Lerp(1f, _maxSpeedMultiplier, t);
            var engageMul = Mathf.Lerp(1f, _maxEngageDistanceMultiplier, t);

            for (var i = 0; i < _aiBaselines.Count; i++)
            {
                var baseline = _aiBaselines[i];
                var config = baseline.Config;
                if (config == null)
                    continue;

                config.health = baseline.Health * healthMul;
                config.movementSpeed = baseline.MovementSpeed * speedMul;
                config.distanceToOpenFire = baseline.DistanceToOpenFire * engageMul;
            }
        }

        private IEnumerator ExtraSpawnLoop()
        {
            yield return new WaitUntil(() =>
                GameManager.Instance != null &&
                GameManager.Instance.gameState == GameState.ActiveGame);

            while (enabled)
            {
                var wait = Mathf.Lerp(10f, 1.6f, Progress);
                yield return new WaitForSeconds(wait);
                TrySpawnExtraBots();
            }
        }

        private void TrySpawnExtraBots()
        {
            if (GameManager.Instance == null || SettingsManager.Instance == null)
                return;

            var spawn = GameManager.Instance.spawnControl;
            if (spawn == null || !spawn.IsServer)
                return;

            var configs = SettingsManager.Instance.ai.configs;
            if (configs == null || configs.Count == 0)
                return;

            var userControl = GameManager.Instance.userControl;
            if (userControl == null)
                return;

            var extraCount = Mathf.RoundToInt(Mathf.Lerp(0f, 3f, Progress));
            var cap = SettingsManager.Instance.gameplay.botsCount;
            for (var i = 0; i < extraCount; i++)
            {
                if (userControl.aiSceneObjects.Count >= cap)
                    break;

                spawn.SpawnAIServerRpc(Random.Range(0, configs.Count));
            }
        }

        private void RestoreBaselines()
        {
            if (!_captured || SettingsManager.Instance == null)
                return;

            if (SettingsManager.Instance.gameplay != null)
            {
                SettingsManager.Instance.gameplay.botsCount = _baseBotsCount;
                SettingsManager.Instance.gameplay.botsSpawnRate = _baseSpawnRate;
            }

            for (var i = 0; i < _aiBaselines.Count; i++)
            {
                var baseline = _aiBaselines[i];
                if (baseline.Config == null)
                    continue;

                baseline.Config.health = baseline.Health;
                baseline.Config.movementSpeed = baseline.MovementSpeed;
                baseline.Config.distanceToOpenFire = baseline.DistanceToOpenFire;
            }
        }

        private struct AiBaseline
        {
            public AIConfig Config;
            public float Health;
            public float MovementSpeed;
            public float DistanceToOpenFire;
        }
    }
}
