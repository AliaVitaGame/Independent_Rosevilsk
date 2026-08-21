using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    [CreateAssetMenu(fileName = "ContinuousFireRateModifierContainer", menuName = "Modifier Container/Continuous Fire Rate Modifier")]
    public class ContinuousFireRateModifierContainer : ModifierContainerBase
    {
        public float fireRateMultiplier;
        public float duration;

        public override ModifierBase GetConfig()
        {
            return new ContinuousFireRateModifier()
            {
                value = fireRateMultiplier,
                duration = duration
            };
        }
    }
}
