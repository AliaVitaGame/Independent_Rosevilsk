using System;
using Game.Core.Audio;
using HEAVYART.TopDownShooter.Netcode;
using UnityEngine;

namespace Game.Core.Vehicles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DriveableVehicleInteraction))]
    public sealed class VehicleHealth : MonoBehaviour, IBulletHitReceiver
    {
        public static VehicleHealth Instance { get; private set; }

        [SerializeField] private float _maxHealth = 400f;
        [SerializeField] private float _currentHealth = 400f;

        private DriveableVehicleInteraction _interaction;

        public float MaxHealth => _maxHealth;
        public float CurrentHealth => _currentHealth;
        public float Normalized => _maxHealth <= 0.01f ? 0f : Mathf.Clamp01(_currentHealth / _maxHealth);
        public bool IsDestroyed => _currentHealth <= 0.01f;
        public Transform AimTransform => transform;
        public bool IsValidTarget => !IsDestroyed && _interaction != null && _interaction.IsDriving;
        public Transform AimPoint => transform;

        public event Action<float, float> HealthChanged;
        public event Action Destroyed;

        private void Awake()
        {
            Instance = this;
            _interaction = GetComponent<DriveableVehicleInteraction>();
            _currentHealth = Mathf.Clamp(_currentHealth, 0f, _maxHealth);
            ExtraCombatTargets.Register(this);
        }

        private void OnDestroy()
        {
            ExtraCombatTargets.Unregister(this);
            if (Instance == this)
                Instance = null;
        }

        public void ReceiveBulletDamage(float damage)
        {
            ApplyDamage(damage);
        }

        public void ApplyDamage(float damage)
        {
            if (IsDestroyed || damage <= 0f)
                return;

            _currentHealth = Mathf.Max(0f, _currentHealth - damage);
            HealthChanged?.Invoke(_currentHealth, _maxHealth);
            GameSfx.PlayVehicleHit();

            if (!IsDestroyed)
                return;

            _interaction?.SetGameplayLocked(true);
            Destroyed?.Invoke();
        }
    }
}
