using System;
using System.Collections.Generic;
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
        [SerializeField] private float _groundRaycastHeight = 50f;
        [SerializeField] private float _groundRaycastDistance = 200f;
        [SerializeField] private LayerMask _groundMask = ~0;

        [Header("Pickup")]
        [SerializeField] private float _approachTriggerDistance = 10f;
        [SerializeField] private float _maxVehicleSpeedToApproach = 6f;

        [Header("Hints")]
        [SerializeField] private TargetHintArrow _hintArrow;
        [SerializeField] private TargetHintTarget _vehicleHint;

        private readonly List<Transform> _spawnPointBuffer = new();
        private DriveableVehicleInteraction _vehicle;
        private PassengerNpc _activePassenger;
        private int _boardedCount;
        private int _lastSpawnIndex = -1;

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
                if (_activePassenger != null)
                {
                    _activePassenger.Boarded -= OnPassengerBoarded;
                    Destroy(_activePassenger.gameObject);
                    _activePassenger = null;
                }

                RefreshHintTarget();
                return;
            }

            if (_activePassenger == null)
                TrySpawnNextPassenger();

            RefreshHintTarget();
        }

        private void Awake()
        {
            if (_vehicle == null)
                _vehicle = FindFirstObjectByType<DriveableVehicleInteraction>();

            if (_vehicleHint == null && _vehicle != null)
                _vehicleHint = _vehicle.GetComponent<TargetHintTarget>();

            if (_hintArrow == null)
                _hintArrow = FindFirstObjectByType<TargetHintArrow>(FindObjectsInactive.Include);

            ResolveSpawnPoints();
        }

        private void Start()
        {
            BoardedCountChanged?.Invoke(_boardedCount, MaxPassengers);
            TrySpawnNextPassenger();
            RefreshHintTarget();
        }

        private void Update()
        {
            RefreshHintTarget();

            if (_activePassenger == null || _vehicle == null || !_vehicle.IsDriving)
                return;

            if (!_activePassenger.IsWaiting)
                return;

            var vehicleTransform = _vehicle.transform;
            var distance = Vector3.Distance(vehicleTransform.position, _activePassenger.transform.position);
            if (distance > _approachTriggerDistance)
                return;

            if (!IsVehicleSlowEnough(vehicleTransform))
                return;

            _activePassenger.BeginApproach(vehicleTransform);
            RefreshHintTarget();
        }

        private void OnDestroy()
        {
            if (_activePassenger != null)
                _activePassenger.Boarded -= OnPassengerBoarded;
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

        private void TrySpawnNextPassenger()
        {
            if (IsFull || _activePassenger != null || _passengerPrefab == null)
                return;

            if (_spawnPointBuffer.Count == 0)
            {
                Debug.LogWarning("[PassengerPickupSystem] No NPC spawn points found.");
                return;
            }

            var spawnPoint = PickSpawnPoint();
            var spawnPosition = SnapToGround(spawnPoint.position);
            _activePassenger = Instantiate(
                _passengerPrefab,
                spawnPosition,
                spawnPoint.rotation);
            _activePassenger.name = $"Passenger_{_boardedCount + 1}";
            _activePassenger.Boarded += OnPassengerBoarded;
            _activePassenger.HintTarget?.SetHintEnabled(true);
        }

        private Transform PickSpawnPoint()
        {
            if (_spawnPointBuffer.Count == 1)
                return _spawnPointBuffer[0];

            var index = UnityEngine.Random.Range(0, _spawnPointBuffer.Count);
            if (index == _lastSpawnIndex)
                index = (index + 1) % _spawnPointBuffer.Count;

            _lastSpawnIndex = index;
            return _spawnPointBuffer[index];
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

            if (_activePassenger == passenger)
                _activePassenger = null;

            _boardedCount = Mathf.Min(_boardedCount + 1, MaxPassengers);
            BoardedCountChanged?.Invoke(_boardedCount, MaxPassengers);

            Destroy(passenger.gameObject);
            TrySpawnNextPassenger();
            RefreshHintTarget();
        }

        private void RefreshHintTarget()
        {
            if (_hintArrow == null)
                return;

            var isDriving = _vehicle != null && _vehicle.IsDriving;
            var hasWaitingPassenger = _activePassenger != null && _activePassenger.IsWaiting;

            if (isDriving && hasWaitingPassenger)
            {
                _vehicleHint?.SetHintEnabled(false);
                _activePassenger.HintTarget?.SetHintEnabled(true);
                _hintArrow.gameObject.SetActive(true);
                _hintArrow.SetTarget(_activePassenger.HintTarget);
                return;
            }

            if (!isDriving && !IsFull && _vehicleHint != null)
            {
                _activePassenger?.HintTarget?.SetHintEnabled(false);
                _vehicleHint.SetHintEnabled(true);
                _hintArrow.gameObject.SetActive(true);
                _hintArrow.SetTarget(_vehicleHint);
                return;
            }

            _vehicleHint?.SetHintEnabled(false);
            _activePassenger?.HintTarget?.SetHintEnabled(false);
            _hintArrow.SetTarget(null);
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
