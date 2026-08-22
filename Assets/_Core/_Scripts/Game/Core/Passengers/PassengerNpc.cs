using System;
using Modules.TargetHints;
using UnityEngine;

namespace Game.Core.Passengers
{
    /// <summary>
    /// World NPC passenger: waits for a pickup, walks to the vehicle, then boards.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TargetHintTarget))]
    public sealed class PassengerNpc : MonoBehaviour
    {
        private static readonly int MovementHash = Animator.StringToHash("Movement");
        private static readonly int MovementSpeedMultiplierHash = Animator.StringToHash("MovementSpeedMultiplier");

        [Header("Locomotion")]
        [SerializeField] private float _walkSpeed = 1.5f;
        [SerializeField] private float _rotationSpeed = 8f;
        [SerializeField] private float _boardDistance = 3.5f;
        [SerializeField] private float _animationSpeed = 0.85f;
        [SerializeField] private float _groundRaycastHeight = 2f;
        [SerializeField] private float _groundRaycastDistance = 8f;
        [SerializeField] private float _soleOffset = 0.02f;
        [SerializeField] private LayerMask _groundMask = ~0;

        [Header("Bindings")]
        [SerializeField] private Animator _animator;
        [SerializeField] private TargetHintTarget _hintTarget;

        private Transform _vehicleTarget;
        private bool _isWalking;
        private bool _isBoarded;

        public TargetHintTarget HintTarget => _hintTarget;
        public bool IsBoarded => _isBoarded;
        public bool IsWaiting => !_isBoarded && !_isWalking;

        public event Action<PassengerNpc> Boarded;

        private void Awake()
        {
            if (_hintTarget == null)
                _hintTarget = GetComponent<TargetHintTarget>();

            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();

            DisableCombatLayers();
            SetWalkingAnimation(false);
        }

        private void Start()
        {
            AlignFeetToGround();
        }

        private void LateUpdate()
        {
            if (_isBoarded)
                return;

            if (_isWalking && _vehicleTarget != null)
                StepTowardVehicle();

            AlignFeetToGround();
        }

        public void BeginApproach(Transform vehicle)
        {
            if (_isBoarded || vehicle == null)
                return;

            _vehicleTarget = vehicle;
            _isWalking = true;
            _hintTarget?.SetHintEnabled(false);
            SetWalkingAnimation(true);
        }

        private void StepTowardVehicle()
        {
            var toTarget = _vehicleTarget.position - transform.position;
            toTarget.y = 0f;
            var distance = toTarget.magnitude;

            if (distance <= _boardDistance)
            {
                CompleteBoarding();
                return;
            }

            var step = Mathf.Min(_walkSpeed * Time.deltaTime, distance);
            var nextPosition = transform.position + toTarget.normalized * step;
            nextPosition.y = SampleGroundHeight(nextPosition, transform.position.y);
            transform.position = nextPosition;

            if (toTarget.sqrMagnitude > 0.001f)
            {
                var lookRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    lookRotation,
                    _rotationSpeed * Time.deltaTime);
            }

            SetWalkingAnimation(true);
        }

        private void CompleteBoarding()
        {
            if (_isBoarded)
                return;

            _isBoarded = true;
            _isWalking = false;
            _vehicleTarget = null;
            SetWalkingAnimation(false);
            _hintTarget?.SetHintEnabled(false);
            Boarded?.Invoke(this);
            gameObject.SetActive(false);
        }

        private void AlignFeetToGround()
        {
            var groundY = SampleGroundHeight(transform.position, transform.position.y);
            var lowestPointY = GetLowestContactY();
            var sink = lowestPointY - groundY + _soleOffset;

            var position = transform.position;
            position.y -= sink;
            transform.position = position;
        }

        private float GetLowestContactY()
        {
            var lowest = float.MaxValue;
            var found = false;

            if (_animator != null && _animator.isHuman)
            {
                TryIncludeBone(HumanBodyBones.LeftFoot, ref lowest, ref found);
                TryIncludeBone(HumanBodyBones.RightFoot, ref lowest, ref found);
                TryIncludeBone(HumanBodyBones.LeftToes, ref lowest, ref found);
                TryIncludeBone(HumanBodyBones.RightToes, ref lowest, ref found);
            }

            if (!found)
            {
                var renderers = GetComponentsInChildren<Renderer>();
                for (var i = 0; i < renderers.Length; i++)
                {
                    lowest = Mathf.Min(lowest, renderers[i].bounds.min.y);
                    found = true;
                }
            }

            return found ? lowest : transform.position.y;
        }

        private void TryIncludeBone(HumanBodyBones bone, ref float lowest, ref bool found)
        {
            var boneTransform = _animator.GetBoneTransform(bone);
            if (boneTransform == null)
                return;

            lowest = Mathf.Min(lowest, boneTransform.position.y);
            found = true;
        }

        private float SampleGroundHeight(Vector3 position, float fallbackY)
        {
            var origin = new Vector3(position.x, position.y + _groundRaycastHeight, position.z);
            if (Physics.Raycast(
                    origin,
                    Vector3.down,
                    out var hit,
                    _groundRaycastDistance,
                    _groundMask,
                    QueryTriggerInteraction.Ignore))
            {
                return hit.point.y;
            }

            origin = new Vector3(position.x, position.y + 50f, position.z);
            if (Physics.Raycast(
                    origin,
                    Vector3.down,
                    out hit,
                    200f,
                    _groundMask,
                    QueryTriggerInteraction.Ignore))
            {
                return hit.point.y;
            }

            return fallbackY;
        }

        private void DisableCombatLayers()
        {
            if (_animator == null)
                return;

            for (var i = 1; i < _animator.layerCount; i++)
                _animator.SetLayerWeight(i, 0f);
        }

        private void SetWalkingAnimation(bool isWalking)
        {
            if (_animator == null)
                return;

            _animator.SetFloat(MovementHash, isWalking ? 1f : 0f);
            _animator.SetFloat(MovementSpeedMultiplierHash, _animationSpeed);
        }
    }
}
