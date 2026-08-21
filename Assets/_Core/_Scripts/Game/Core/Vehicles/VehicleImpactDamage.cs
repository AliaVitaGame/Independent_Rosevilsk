using System.Collections.Generic;
using HEAVYART.TopDownShooter.Netcode;
using Unity.Netcode;
using UnityEngine;

namespace Game.Core.Vehicles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(DriveableVehicleInteraction))]
    public sealed class VehicleImpactDamage : MonoBehaviour
    {
        private const float MinimumImpactSpeed = 4f;
        private const float MinimumDamage = 35f;
        private const float DamagePerMeterPerSecond = 8f;
        private const float MaximumDamage = 250f;
        private const float TargetHitCooldown = 0.75f;

        private readonly Dictionary<CharacterIdentityControl, float> _nextAllowedHitTimes = new();

        private DriveableVehicleInteraction _interaction;
        private Rigidbody _vehicleRigidbody;

        private void Awake()
        {
            _interaction = GetComponent<DriveableVehicleInteraction>();
            _vehicleRigidbody = GetComponent<Rigidbody>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_interaction.IsDriving || collision.contactCount == 0)
                return;

            var networkManager = NetworkManager.Singleton;
            if (networkManager == null || !networkManager.IsListening)
                return;

            var identity = collision.collider.GetComponentInParent<CharacterIdentityControl>();
            if (identity == null || !identity.isBot)
                return;

            var health = identity.GetComponent<HealthController>();
            var commandReceiver = identity.GetComponent<CommandReceiver>();
            if (health == null || !health.isAlive || commandReceiver == null)
                return;

            var contact = collision.GetContact(0);
            var vehicleVelocityAtImpact = _vehicleRigidbody.GetPointVelocity(contact.point);
            var impactSpeed = Mathf.Max(0f, Vector3.Dot(vehicleVelocityAtImpact, -contact.normal));
            if (impactSpeed < MinimumImpactSpeed)
                return;

            if (_nextAllowedHitTimes.TryGetValue(identity, out var nextHitTime) && Time.time < nextHitTime)
                return;

            _nextAllowedHitTimes[identity] = Time.time + TargetHitCooldown;

            var damage = Mathf.Clamp(
                impactSpeed * DamagePerMeterPerSecond,
                MinimumDamage,
                MaximumDamage);

            var modifiers = new ModifierBase[]
            {
                new InstantDamage { damage = damage }
            };

            commandReceiver.ReceiveModifiersRpc(
                modifiers,
                networkManager.LocalClientId,
                networkManager.ServerTime.Time);
        }

        private void OnDisable()
        {
            _nextAllowedHitTimes.Clear();
        }
    }
}
