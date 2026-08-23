using System;
using System.Collections.Generic;
using Game.Core.Map;
using Game.Core.Mission;
using Game.Core.Vehicles;
using Modules.TargetHints;
using UnityEngine;

namespace Game.Core.Passengers
{
    /// <summary>
    /// Spawns waiting passengers, retargets the hint arrow, and tracks boarded count.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PassengerPickupSystem : MonoBehaviour
    {
        public const int MaxPassengers = 5;

        [Header("Spawn")]
        [SerializeField] private PassengerNpc _passengerPrefab;
        [SerializeField] private Transform[] _spawnPoints;
        [SerializeField] private Transform _npcSpawnRoot;
        [SerializeField] private bool _autoCollectNpcSpawnPoints = true;
        [SerializeField] private int _initialPassengerCount = 8;
        [SerializeField] private float _groundRaycastHeight = 50f;
        [SerializeField] private float _groundRaycastDistance = 200f;
        [SerializeField] private LayerMask _groundMask = ~0;

        [Header("Pickup")]
        [SerializeField] private float _approachTriggerDistance = 10f;
        [SerializeField] private float _maxVehicleSpeedToApproach = 6f;

        [Header("Hints")]
        [SerializeField] private TargetHintArrow _hintArrow;
        [SerializeField] private TargetHintTarget _vehicleHint;
        [SerializeField] private TargetHintTarget _exitHint;

        private readonly List<Transform> _spawnPointBuffer = new();
        private readonly List<PassengerNpc> _waitingPassengers = new();
        private DriveableVehicleInteraction _vehicle;
        private int _boardedCount;
        private readonly HashSet<int> _usedSpawnIndices = new();

        public int BoardedCount => _boardedCount;
        public int MaxCount => MaxPassengers;
        public bool IsFull => _boardedCount >= MaxPassengers;

        public event Action<int, int> BoardedCountChanged;

        public void RestoreBoardedCount(int count)
        {
            _boardedCount = Mathf.Clamp(count, 0, MaxPassengers);
            BoardedCountChanged?.Invoke(_boardedCount, MaxPassengers);

            if (IsFull)
            {
                ClearWaitingPassengers();
                RefreshHintTarget();
                return;
            }

            if (_waitingPassengers.Count == 0)
                SpawnInitialPassengers();

            RefreshHintTarget();
        }

        private void Awake()
        {
            if (_vehicle == null)
                _vehicle = FindFirstObjectByType<DriveableVehicleInteraction>();

            if (_vehicleHint == null && _vehicle != null)
                _vehicleHint = _vehicle.GetComponent<TargetHintTarget>();

            _vehicleHint?.SetKind(TargetHintKind.Vehicle);

            if (_hintArrow == null)
                _hintArrow = FindFirstObjectByType<TargetHintArrow>(FindObjectsInactive.Include);

            EnsureHintArrowPool();
            EnsurePressureSystems();
            EnsureRoadGuideLines();
            EnsureCityMap();

            if (_exitHint == null)
            {
                var exitPoint = GameObject.Find("ExitPoint");
                if (exitPoint != null)
                {
                    _exitHint = exitPoint.GetComponent<TargetHintTarget>();
                    if (_exitHint == null)
                        _exitHint = exitPoint.AddComponent<TargetHintTarget>();
                }
            }

            _exitHint?.SetKind(TargetHintKind.Exit);
            _exitHint?.SetHintEnabled(false);
            _vehicleHint?.SetHintEnabled(false);
            ResolveSpawnPoints();
        }

        private void Start()
        {
            BoardedCountChanged?.Invoke(_boardedCount, MaxPassengers);
            if (_waitingPassengers.Count == 0 && !IsFull)
                SpawnInitialPassengers();

            RefreshHintTarget();
        }

        private void Update()
        {
            RefreshHintTarget();

            if (_vehicle == null || !_vehicle.IsDriving)
                return;

            var vehicleTransform = _vehicle.transform;
            if (!IsVehicleSlowEnough(vehicleTransform))
                return;

            for (var i = 0; i < _waitingPassengers.Count; i++)
            {
                var passenger = _waitingPassengers[i];
                if (passenger == null || !passenger.IsWaiting)
                    continue;

                var distance = Vector3.Distance(vehicleTransform.position, passenger.transform.position);
                if (distance > _approachTriggerDistance)
                    continue;

                passenger.BeginApproach(vehicleTransform);
            }
        }

        private void OnDestroy()
        {
            for (var i = 0; i < _waitingPassengers.Count; i++)
            {
                if (_waitingPassengers[i] != null)
                    _waitingPassengers[i].Boarded -= OnPassengerBoarded;
            }
        }

        private void ResolveSpawnPoints()
        {
            _spawnPointBuffer.Clear();

            if (_spawnPoints != null)
            {
                for (var i = 0; i < _spawnPoints.Length; i++)
                {
                    if (_spawnPoints[i] != null)
                        _spawnPointBuffer.Add(_spawnPoints[i]);
                }
            }

            if (_spawnPointBuffer.Count > 0 || !_autoCollectNpcSpawnPoints)
                return;

            var npcRoot = _npcSpawnRoot;
            if (npcRoot == null)
                npcRoot = FindNpcSpawnRoot();

            if (npcRoot == null)
                return;

            for (var i = 0; i < npcRoot.childCount; i++)
                _spawnPointBuffer.Add(npcRoot.GetChild(i));
        }

        private static Transform FindNpcSpawnRoot()
        {
            var byPath = GameObject.Find("Points/NPC");
            if (byPath != null)
                return byPath.transform;

            var byName = GameObject.Find("NPC");
            return byName != null ? byName.transform : null;
        }

        private void SpawnInitialPassengers()
        {
            if (_passengerPrefab == null)
                return;

            if (_spawnPointBuffer.Count == 0)
            {
                Debug.LogWarning("[PassengerPickupSystem] No NPC spawn points found.");
                return;
            }

            var spawnCount = Mathf.Min(_initialPassengerCount, _spawnPointBuffer.Count);
            var order = BuildShuffledIndices(_spawnPointBuffer.Count);
            for (var i = 0; i < spawnCount; i++)
                SpawnPassengerAt(_spawnPointBuffer[order[i]], order[i]);
        }

        private void SpawnPassengerAt(Transform spawnPoint, int spawnIndex)
        {
            var spawnPosition = SnapToGround(spawnPoint.position);
            var passenger = Instantiate(
                _passengerPrefab,
                spawnPosition,
                spawnPoint.rotation);
            passenger.name = $"Passenger_{_waitingPassengers.Count + 1}";
            passenger.HintTarget?.SetKind(TargetHintKind.Passenger);
            passenger.HintTarget?.SetHintEnabled(true);
            passenger.Boarded += OnPassengerBoarded;
            _waitingPassengers.Add(passenger);
            _usedSpawnIndices.Add(spawnIndex);
        }

        private static int[] BuildShuffledIndices(int count)
        {
            var indices = new int[count];
            for (var i = 0; i < count; i++)
                indices[i] = i;

            for (var i = count - 1; i > 0; i--)
            {
                var j = UnityEngine.Random.Range(0, i + 1);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }

            return indices;
        }

        private Vector3 SnapToGround(Vector3 position)
        {
            var origin = position + Vector3.up * _groundRaycastHeight;
            if (Physics.Raycast(origin, Vector3.down, out var hit, _groundRaycastDistance, _groundMask, QueryTriggerInteraction.Ignore))
                return hit.point;

            return new Vector3(position.x, 0f, position.z);
        }

        private void OnPassengerBoarded(PassengerNpc passenger)
        {
            passenger.Boarded -= OnPassengerBoarded;
            _waitingPassengers.Remove(passenger);

            _boardedCount = Mathf.Min(_boardedCount + 1, MaxPassengers);
            BoardedCountChanged?.Invoke(_boardedCount, MaxPassengers);

            Destroy(passenger.gameObject);
            RefreshHintTarget();
        }

        private void ClearWaitingPassengers()
        {
            for (var i = 0; i < _waitingPassengers.Count; i++)
            {
                var passenger = _waitingPassengers[i];
                if (passenger == null)
                    continue;

                passenger.Boarded -= OnPassengerBoarded;
                Destroy(passenger.gameObject);
            }

            _waitingPassengers.Clear();
        }

        private void RefreshHintTarget()
        {
            var isDriving = _vehicle != null && _vehicle.IsDriving;
            _vehicleHint?.SetHintEnabled(!isDriving);
            _exitHint?.SetHintEnabled(true);

            for (var i = 0; i < _waitingPassengers.Count; i++)
            {
                var passenger = _waitingPassengers[i];
                if (passenger == null)
                    continue;

                passenger.HintTarget?.SetHintEnabled(passenger.IsWaiting);
            }
        }

        private void EnsureHintArrowPool()
        {
            if (_hintArrow == null)
                return;

            var host = _hintArrow.transform.parent != null
                ? _hintArrow.transform.parent.gameObject
                : _hintArrow.gameObject;
            if (host.GetComponent<TargetHintArrowPool>() == null)
                host.AddComponent<TargetHintArrowPool>();
        }

        private void EnsurePressureSystems()
        {
            if (GetComponent<LocationPressureSystem>() == null)
                gameObject.AddComponent<LocationPressureSystem>();

            if (FindFirstObjectByType<LocationTimerUI>(FindObjectsInactive.Include) != null)
                return;

            var canvas = GameObject.Find("InGameUI");
            var host = canvas != null ? canvas : gameObject;
            if (host.GetComponent<LocationTimerUI>() == null)
                host.AddComponent<LocationTimerUI>();
        }

        private void EnsureRoadGuideLines()
        {
            if (FindFirstObjectByType<RoadGuideLineSystem>(FindObjectsInactive.Include) != null)
                return;

            var host = new GameObject("RoadGuideLines");
            host.AddComponent<RoadGuideLineSystem>();
        }

        private void EnsureCityMap()
        {
            if (FindFirstObjectByType<CityMapController>(FindObjectsInactive.Include) != null)
                return;

            var host = new GameObject("CityMap");
            host.AddComponent<CityMapController>();
        }

        private bool IsVehicleSlowEnough(Transform vehicleTransform)
        {
            var rigidbody = vehicleTransform.GetComponent<Rigidbody>();
            if (rigidbody == null)
                return true;

            return rigidbody.linearVelocity.magnitude <= _maxVehicleSpeedToApproach;
        }
    }
}
