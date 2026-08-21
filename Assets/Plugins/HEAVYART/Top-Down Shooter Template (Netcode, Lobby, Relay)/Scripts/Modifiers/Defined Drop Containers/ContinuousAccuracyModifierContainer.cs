using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    [CreateAssetMenu(fileName = "ContinuousAccuracyModifierContainer", menuName = "Modifier Container/Continuous Accuracy Modifier")]
    public class ContinuousAccuracyModifierContainer : ModifierContainerBase
    {
        public float accuracyMultiplier;
        public float duration;

        public override ModifierBase GetConfig()
        {
            return new ContinuousAccuracyModifier
            {
                value = accuracyMultiplier,
                duration = duration
            };
        }
    }
}
