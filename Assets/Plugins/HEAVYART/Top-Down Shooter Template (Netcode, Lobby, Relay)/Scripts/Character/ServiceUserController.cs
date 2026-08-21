using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    //Session user object. While player objects on scene are destroyable, this one is not.
    //Could be used for any commands to communicate with other players, like voting, chat, etc.
    public class ServiceUserController : NetworkBehaviour
    {
        private NetworkVariable<FixedString64Bytes> synchronizedName = new NetworkVariable<FixedString64Bytes>(writePerm: NetworkVariableWritePermission.Owner);

        private void Awake()
        {
            //Keep object safe between main menu and game scene
            DontDestroyOnLoad(gameObject);
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
                synchronizedName.Value = PlayerDataKeeper.name;

            StartCoroutine(RegisterInGameManager());
        }

        private IEnumerator RegisterInGameManager()
        {
            // Wait until the scene NetworkObject is fully spawned. Subscribing to
            // OnNetworkReady here is racy: the event may fire before this persistent
            // service object resumes after the scene load.
            while (GameManager.Instance == null || !GameManager.Instance.IsSpawned)
                yield return null;

            //Wait for our synchronized name to initialize (on other clients side)
            while (synchronizedName.Value == default)
                yield return null;

            GameManager.Instance.userControl.AddUserServiceObject(NetworkObject, synchronizedName.Value.ToString());

            if (IsOwner)
                StartCoroutine(SpawnPlayerObject());

            gameObject.name = "ServiceUserObject: " + synchronizedName.Value;

            // Keep this spawned session object in DontDestroyOnLoad. Moving it into the
            // game scene while Unity is processing that scene makes Netcode treat the
            // already-spawned object as an in-scene placed NetworkObject.
        }

        private IEnumerator SpawnPlayerObject()
        {
            //Wait for scene to initialize and spawn player after short delay
            const int delay = 1;
            yield return new WaitForSeconds(delay);

            GameManager.Instance.spawnControl.SpawnPlayerServerRpc(GetLocalPlayerSpawnParameters());
        }

        public CharacterSpawnParameters GetLocalPlayerSpawnParameters()
        {
            return new CharacterSpawnParameters()
            {
                name = PlayerDataKeeper.name,
                color = SettingsManager.Instance.player.GetPlayerColor(),
                ownerID = NetworkManager.Singleton.LocalClientId,
                modelIndex = PlayerDataKeeper.selectedPrefab
            };
        }
    }
}
