using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    [CreateAssetMenu(fileName = "InstantDamage", menuName = "Modifier Container/Instant Damage")]
    public class InstantDamageContainer : ModifierContainerBase
    {
        public float damage;

        public override ModifierBase GetConfig()
        {
            return new InstantDamage()
            {
                damage = damage
            };
        }
    }
}
