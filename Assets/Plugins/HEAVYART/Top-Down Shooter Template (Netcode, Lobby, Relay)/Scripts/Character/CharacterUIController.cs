using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class CharacterUIController : NetworkBehaviour
    {
        public Transform statusBarPrefab;
        private StatusBarUIController statusBarUIController;
        private HealthController healthController;
        private CharacterIdentityControl identityControl;
        private ModifiersControlSystem modifiersControlSystem;

        private void Awake()
        {
            healthController = GetComponent<HealthController>();
            identityControl = GetComponent<CharacterIdentityControl>();
            modifiersControlSystem = GetComponent<ModifiersControlSystem>();

            healthController.OnDeath += DestroyStatusBar;
        }

        public override void OnNetworkSpawn()
        {
            //Instantiate status bar, if it's not our player
            if (identityControl.IsLocalPlayer == false)
            {
                Transform statusBar = Instantiate(statusBarPrefab, GameManager.Instance.UI.statusBarsContainer.transform);
                statusBarUIController = statusBar.GetComponent<StatusBarUIController>();

                //Link status bar to current transform (it will move with character)
                statusBarUIController.LinkTransform(transform);

                if (identityControl.isPlayer == true)
                    statusBarUIController.ShowUserName(identityControl.spawnParameters.Value.name);
            }
        }

        new private void OnDestroy()
        {
            DestroyStatusBar();
        }

        private void DestroyStatusBar()
        {
            if (statusBarUIController != null)
                Destroy(statusBarUIController.gameObject);
        }

        private void FixedUpdate()
        {
            if (IsSpawned == false) return;

            if (identityControl.IsLocalPlayer == true) //Use HUD
            {
                GameManager.Instance.UI.ShowHUD();
                GameManager.Instance.UI.hudStatusBar.UpdateHealthAmount(healthController.currentHealth, healthController.maxHealth);
                GameManager.Instance.UI.hudStatusBar.UpdateHealthBarColor();
                GameManager.Instance.UI.hudStatusBar.UpdatePowerUpIndicators(modifiersControlSystem, true);
            }
            else //Use status bar (above head)
            {
                if (healthController.isAlive == false) return;

                //Update status bar (every frame)
                statusBarUIController.UpdateHealthAmount(healthController.currentHealth, healthController.maxHealth);
                statusBarUIController.UpdatePosition();
                statusBarUIController.UpdatePowerUpIndicators(modifiersControlSystem, true);
            }
        }
    }
}
