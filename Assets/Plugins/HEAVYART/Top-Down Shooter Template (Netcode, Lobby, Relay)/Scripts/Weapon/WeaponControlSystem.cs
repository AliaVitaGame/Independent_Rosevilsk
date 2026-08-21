using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class WeaponControlSystem : NetworkBehaviour
    {
        public Transform lineOfSightTransform;
        public List<Weapon> weapons = new List<Weapon>();

        private Weapon selectedWeapon;
        private CharacterAnimationController animationController;
        private HealthController healthController;
        private ModifiersControlSystem modifiersControlSystem;
        private CharacterIdentityControl identityControl;

        private void Awake()
        {
            animationController = GetComponent<CharacterAnimationController>();
            healthController = GetComponent<HealthController>();
            modifiersControlSystem = GetComponent<ModifiersControlSystem>();
            identityControl = GetComponent<CharacterIdentityControl>();

            for (int i = 0; i < weapons.Count; i++)
            {
                //Link fire animation to fire event
                weapons[i].OnFire += () => animationController.PlayFireAnimation();
            }
        }

        public override void OnNetworkSpawn()
        {
            //Set default weapon (locally). 
            ActivateWeapon(WeaponType.Pistol);
        }

        public void Fire()
        {
            if (healthController.isAlive)
                selectedWeapon.Fire();
        }

        private void Update()
        {
            //Easy weapon switch for debugging
            if (Application.isEditor == true && identityControl.IsLocalPlayer == true)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) ActivateWeaponRpc(WeaponType.Pistol);

                if (Input.GetKeyDown(KeyCode.Alpha2)) ActivateWeaponRpc(WeaponType.Rifle);

                if (Input.GetKeyDown(KeyCode.Alpha3)) ActivateWeaponRpc(WeaponType.Shotgun);
            }
        }

        private void FixedUpdate()
        {
            if (IsOwner == false) return;

            //Take and apply weapon switch command (modifier)
            modifiersControlSystem.HandleWeaponSwitchCommands((weaponType) =>
            {
                //Broadcast this message to clients
                ActivateWeaponRpc(weaponType);
            });
        }

        [Rpc(SendTo.Everyone)]
        public void ActivateWeaponRpc(WeaponType weaponType)
        {
            //Receive and apply
            ActivateWeapon(weaponType);
        }

        private void ActivateWeapon(WeaponType weaponType)
        {
            //Check if weapon is available
            bool isWeaponAvailable = false;
            for (int i = 0; i < weapons.Count; i++)
            {
                if (weapons[i].weaponType == weaponType)
                    isWeaponAvailable = true;
            }

            if (isWeaponAvailable == false)
            {
                Debug.Log($"Weapon {weaponType} in not available for this character.");
                return;
            }

            for (int i = 0; i < weapons.Count; i++)
            {
                //Hide all active weapons
                weapons[i].HideWeapon();

                //Find the right one
                if (weapons[i].weaponType == weaponType)
                {
                    //Activate it
                    weapons[i].ShowWeapon();
                    animationController.SetTargetingTransform(weapons[i].targetingTransform);
                    selectedWeapon = weapons[i];

                    //Handle weapon grip
                    WeaponGrip weaponGrip = weapons[i].weaponGrip;
                    Transform leftHandGripIKTransform = weapons[i].weaponModelTransformKeeper.leftHandGripIKTransform;
                    animationController.UpdateWeaponGrip(weaponGrip, leftHandGripIKTransform);
                }
            }
        }
    }
}
