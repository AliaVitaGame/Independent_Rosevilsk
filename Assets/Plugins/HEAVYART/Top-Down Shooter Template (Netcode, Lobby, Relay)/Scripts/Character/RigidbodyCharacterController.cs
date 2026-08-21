using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class RigidbodyCharacterController : NetworkBehaviour
    {
        public float gravity = 15.0f;
        public float maxVelocityChange = 10.0f;
        public float groundCheckDistance = 0.2f;
        [Range(0f, 89f)] public float maxGroundAngle = 50f;

        private Rigidbody currentRigidbody;
        private Collider characterCollider;
        private readonly RaycastHit[] groundHits = new RaycastHit[16];

        void Awake()
        {
            currentRigidbody = GetComponent<Rigidbody>();
            characterCollider = GetComponent<Collider>();
            currentRigidbody.freezeRotation = true;
            currentRigidbody.useGravity = false;
        }

        private void Start()
        {
            if (IsOwner == false) currentRigidbody.isKinematic = true;
        }

        public void Move(Vector3 direction, float speed)
        {
            // Calculate how fast we should be moving
            Vector3 targetVelocity = direction;
            targetVelocity = transform.TransformDirection(targetVelocity);
            targetVelocity *= speed;

            if (TryGetSurfaceBelow(out var surfaceHit) && !IsWalkableSurface(surfaceHit.normal))
            {
                var horizontalSurfaceNormal = Vector3.ProjectOnPlane(surfaceHit.normal, Vector3.up).normalized;
                var velocityIntoSlope = Vector3.Dot(targetVelocity, -horizontalSurfaceNormal);

                if (velocityIntoSlope > 0f)
                    targetVelocity += horizontalSurfaceNormal * velocityIntoSlope;
            }

            // Apply a force that attempts to reach our target velocity
            Vector3 velocity = currentRigidbody.linearVelocity;
            Vector3 velocityChange = (targetVelocity - velocity);
            velocityChange.x = Mathf.Clamp(velocityChange.x, -maxVelocityChange, maxVelocityChange);
            velocityChange.z = Mathf.Clamp(velocityChange.z, -maxVelocityChange, maxVelocityChange);
            velocityChange.y = 0;
            currentRigidbody.AddForce(velocityChange, ForceMode.VelocityChange);


            // We apply gravity manually for more tuning control
            currentRigidbody.AddForce(new Vector3(0, -gravity * currentRigidbody.mass, 0));
        }

        public void Stop()
        {
            currentRigidbody.isKinematic = true;
        }

        public bool Jump(float jumpVelocity)
        {
            if (currentRigidbody.isKinematic || currentRigidbody.linearVelocity.y > 0.1f || !IsGrounded())
                return false;

            var velocity = currentRigidbody.linearVelocity;
            velocity.y = jumpVelocity;
            currentRigidbody.linearVelocity = velocity;
            return true;
        }

        public bool IsGrounded()
        {
            if (!TryGetSurfaceBelow(out var surfaceHit))
                return false;

            return IsWalkableSurface(surfaceHit.normal);
        }

        public bool IsWalkableSurface(Vector3 surfaceNormal)
        {
            var minimumGroundDot = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
            return Vector3.Dot(surfaceNormal.normalized, Vector3.up) >= minimumGroundDot;
        }

        private bool TryGetSurfaceBelow(out RaycastHit closestHit)
        {
            closestHit = default;
            if (characterCollider == null)
                return false;

            var bounds = characterCollider.bounds;
            var checkRadius = Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.75f;
            var castOrigin = new Vector3(
                bounds.center.x,
                bounds.min.y + checkRadius + groundCheckDistance,
                bounds.center.z);
            var hitCount = Physics.SphereCastNonAlloc(
                castOrigin,
                checkRadius,
                Vector3.down,
                groundHits,
                groundCheckDistance * 2f,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore);
            var closestDistance = float.PositiveInfinity;

            for (var i = 0; i < hitCount; i++)
            {
                var hit = groundHits[i];
                if (hit.collider == null || hit.transform.root == transform.root)
                    continue;

                if (hit.distance >= closestDistance)
                    continue;

                closestDistance = hit.distance;
                closestHit = hit;
            }

            return closestDistance < float.PositiveInfinity;
        }
    }
}
