using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class PickUpItemController : NetworkBehaviour
    {
        public ModifierContainerBase container;

        private void OnTriggerEnter(Collider other)
        {
            var networkManager = NetworkManager.Singleton;
            if (networkManager == null || !networkManager.IsListening || !networkManager.IsServer || !IsSpawned)
                return;

            CommandReceiver commandReceiver = other.GetComponent<CommandReceiver>();

            if (commandReceiver != null)
            {
                //Pick up drop element and broadcast message. It's server side.
                commandReceiver.ReceiveModifiersRpc(new ModifierBase[] { container.GetConfig() }, 0, networkManager.ServerTime.Time);
                NetworkObject.Despawn(true);
            }
        }
    }
}
