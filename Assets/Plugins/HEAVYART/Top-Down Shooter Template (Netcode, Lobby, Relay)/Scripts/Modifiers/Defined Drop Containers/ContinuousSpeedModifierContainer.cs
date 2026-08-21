using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    [CreateAssetMenu(fileName = "ContinuousSpeedModifier", menuName = "Modifier Container/Continuous Speed Modifier")]
    public class ContinuousSpeedModifierContainer : ModifierContainerBase
    {
        public float speedMultiplier;
        public float duration;

        public override ModifierBase GetConfig()
        {
            return new ContinuousSpeedModifier()
            {
                value = speedMultiplier,
                duration = duration
            };
        }
    }
}
