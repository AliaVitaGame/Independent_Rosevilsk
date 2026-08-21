using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class HealthController : NetworkBehaviour
    {
        public float currentHealth { get; private set; }
        public float maxHealth { get; private set; }

        public bool isAlive => currentHealth > 0;

        public Action OnDeath;

        private ModifiersControlSystem modifiersControlSystem;
        private CharacterIdentityControl identityControl;

        public void Awake()
        {
            modifiersControlSystem = GetComponent<ModifiersControlSystem>();
            identityControl = GetComponent<CharacterIdentityControl>();
        }

        public void Initialize(float maxHealth)
        {
            currentHealth = maxHealth;
            this.maxHealth = maxHealth;
        }

        private void FixedUpdate()
        {
            if (isAlive == false) return;

            //Update health
            float updatedHealth = modifiersControlSystem.HandleHealthModifiers(currentHealth, OnDeathEvent);
            currentHealth = Mathf.Clamp(updatedHealth, 0, maxHealth);
        }

        private void OnDeathEvent(ActiveModifierData activeModifier)
        {
            //Broadcast death event
            if (identityControl.IsOwner == true)
                ConfirmCharacterDeathRpc(activeModifier.ownerID);
        }

        [Rpc(SendTo.Everyone)]
        private void ConfirmCharacterDeathRpc(ulong killerID)
        {
            currentHealth = 0;
            OnDeath?.Invoke();

            //Register bot death (from player)
            if (identityControl.isBot == true && killerID != SettingsManager.Instance.ai.defaultOwnerID)
                GameManager.Instance.RegisterCharacterDeath(killerID);
        }
    }
}
