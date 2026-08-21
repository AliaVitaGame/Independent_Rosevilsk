using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    [CreateAssetMenu(fileName = "WeaponSwitchCommand", menuName = "Modifier Container/Weapon Switch Command")]
    public class WeaponSwitchCommandContainer : ModifierContainerBase
    {
        public WeaponType weapon;

        public override ModifierBase GetConfig()
        {
            return new WeaponSwitchCommand
            {
                weapon = weapon
            };
        }
    }
}
