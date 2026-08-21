using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class WeaponSettings : MonoBehaviour
    {
        public List<WeaponConfig> weapons = new List<WeaponConfig>();

        public WeaponConfig GetWeaponConfig(WeaponType weaponType)
        {
            return weapons.Find(weapon => weapon.weaponType == weaponType);
        }
    }
}
