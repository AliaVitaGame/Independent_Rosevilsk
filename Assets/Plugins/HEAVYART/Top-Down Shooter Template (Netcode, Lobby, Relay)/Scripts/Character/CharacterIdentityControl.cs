using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class CharacterIdentityControl : NetworkBehaviour
    {
        //Since we use scene objects as players, we need some tool to recognize if its player or bot 

        public bool isPlayer { get; private set; }
        public bool isBot { get; private set; }

        new public bool IsLocalPlayer => isPlayer && IsOwner;
        new public bool IsOwner => spawnParameters.Value.ownerID == NetworkManager.Singleton.LocalClientId;
        new public ulong OwnerClientId => spawnParameters.Value.ownerID;

        [HideInInspector]
        public NetworkVariable<CharacterSpawnParameters> spawnParameters = new NetworkVariable<CharacterSpawnParameters>();

        private CharacterSpawnParameters serverBufferedSpawnParameters;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
                spawnParameters.Value = serverBufferedSpawnParameters;
        }

        public void SetSpawnParameters(CharacterSpawnParameters spawnParameters)
        {
            serverBufferedSpawnParameters = spawnParameters;
        }

        private void Awake()
        {

            isPlayer = GetComponent<PlayerBehaviour>() != null;
            isBot = GetComponent<AIBehaviour>() != null;
        }
    }
}
