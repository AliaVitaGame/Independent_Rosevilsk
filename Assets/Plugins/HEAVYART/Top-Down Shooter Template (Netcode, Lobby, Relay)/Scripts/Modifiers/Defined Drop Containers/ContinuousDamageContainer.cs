using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    [CreateAssetMenu(fileName = "ContinuousDamageContainer", menuName = "Modifier Container/Continuous Damage")]
    public class ContinuousDamageContainer : ModifierContainerBase
    {
        public float damage;
        public float duration;

        public override ModifierBase GetConfig()
        {
            return new ContinuousDamage()
            {
                damage = damage,
                duration = duration
            };
        }
    }
}
