using System.Collections.Generic;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    public interface IBulletHitReceiver
    {
        Transform AimTransform { get; }
        bool IsValidTarget { get; }
        void ReceiveBulletDamage(float damage);
    }

    public static class ExtraCombatTargets
    {
        private static readonly List<IBulletHitReceiver> Targets = new();

        public static IReadOnlyList<IBulletHitReceiver> All => Targets;

        public static void Register(IBulletHitReceiver target)
        {
            if (target != null && !Targets.Contains(target))
                Targets.Add(target);
        }

        public static void Unregister(IBulletHitReceiver target)
        {
            Targets.Remove(target);
        }
    }
}
