using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class CommandReceiver : NetworkBehaviour
    {
        private ModifiersControlSystem modifiersControlSystem;

        private void Awake()
        {
            modifiersControlSystem = GetComponent<ModifiersControlSystem>();
        }

        [Rpc(SendTo.Everyone)]
        public void ReceiveBulletHitRpc(ModifierBase[] modifiers, ulong ownerID, double startTime)
        {
            //Receive
            for (int i = 0; i < modifiers.Length; i++)
            {
                modifiersControlSystem.AddModifier(modifiers[i], ownerID, startTime);
            }
        }

        [Rpc(SendTo.Everyone)]
        public void ReceiveModifiersRpc(ModifierBase[] modifiers, ulong ownerID, double startTime)
        {
            //Receive
            for (int i = 0; i < modifiers.Length; i++)
            {
                modifiersControlSystem.AddModifier(modifiers[i], ownerID, startTime);
            }
        }
    }
}
