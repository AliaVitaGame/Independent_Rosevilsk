using Game.Core.Audio;
using System.Collections.Generic;
using FCG;
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
        private const float MinimumCrashSoundSpeed = 2.2f;
        private const float MinimumDamage = 35f;
        private const float DamagePerMeterPerSecond = 8f;
        private const float MaximumDamage = 250f;
        private const float TargetHitCooldown = 0.75f;
        private const float CrashSoundCooldown = 0.2f;
        private const float HornCooldown = 0.8f;

        private readonly Dictionary<CharacterIdentityControl, float> _nextAllowedHitTimes = new();

        private DriveableVehicleInteraction _interaction;
        private Rigidbody _vehicleRigidbody;
        private float _nextCrashSoundTime;
        private float _nextHornTime;

        private void Awake()
        {
            _interaction = GetComponent<DriveableVehicleInteraction>();
            _vehicleRigidbody = GetComponent<Rigidbody>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_interaction.IsDriving || collision.contactCount == 0)
                return;

            var contact = collision.GetContact(0);
            var vehicleVelocityAtImpact = _vehicleRigidbody.GetPointVelocity(contact.point);
            var impactSpeed = Mathf.Max(
                collision.relativeVelocity.magnitude,
                Mathf.Max(0f, Vector3.Dot(vehicleVelocityAtImpact, -contact.normal)));

            if (impactSpeed >= MinimumCrashSoundSpeed && Time.time >= _nextCrashSoundTime)
            {
                _nextCrashSoundTime = Time.time + CrashSoundCooldown;
                GameSfx.PlayVehicleCrash(impactSpeed);
            }

            if (IsOtherCar(collision.collider) && impactSpeed >= MinimumCrashSoundSpeed && Time.time >= _nextHornTime)
            {
                _nextHornTime = Time.time + HornCooldown;
                GameSfx.PlayCarHorn();
            }

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

        private bool IsOtherCar(Collider other)
        {
            if (other == null)
                return false;

            if (other.transform.root == transform.root)
                return false;

            if (other.GetComponentInParent<TrafficCar>() != null)
                return true;

            if (other.GetComponentInParent<PrometeoCarController>() != null)
                return true;

            if (other.GetComponentInParent<DriveableVehicleInteraction>() != null)
                return true;

            var name = other.transform.root.name;
            return name.IndexOf("Car", System.StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("Vehicle", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void OnDisable()
        {
            _nextAllowedHitTimes.Clear();
        }
    }
}
