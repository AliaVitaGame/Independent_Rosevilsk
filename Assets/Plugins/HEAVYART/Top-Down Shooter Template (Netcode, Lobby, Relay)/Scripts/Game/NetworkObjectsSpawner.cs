using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class NetworkObjectsSpawner : NetworkBehaviour
    {
        public List<Transform> playerSpawnPoints;
        public List<Transform> aiSpawnPoints;

        private NetworkObjectsControl userControl;

        private void Start()
        {
            userControl = GetComponent<NetworkObjectsControl>();
            StartCoroutine(RunBotSpawnLoop());
        }

        IEnumerator RunBotSpawnLoop()
        {
            //Wait for game to start
            while (GameManager.Instance.gameState != GameState.ActiveGame) yield return 0;

            //While game is active
            while (GameManager.Instance.gameState != GameState.GameIsOver)
            {
                //Wait for spawn
                yield return new WaitForSeconds(SettingsManager.Instance.gameplay.botsSpawnRate);

                if (userControl.aiSceneObjects.Count < SettingsManager.Instance.gameplay.botsCount)
                    if (IsServer)
                    {
                        int randomBotIndex = Random.Range(0, SettingsManager.Instance.ai.configs.Count);

                        //Spawn bot
                        SpawnAIServerRpc(randomBotIndex);
                    }
            }
        }

        public void RespawnLocalPlayer()
        {
            if (userControl.localPlayer == null)
            {
                ServiceUserController serviceUserController = userControl.userServiceObject.GetComponent<ServiceUserController>();

                //Custom spawn parameters
                CharacterSpawnParameters characterSpawnParameters = serviceUserController.GetLocalPlayerSpawnParameters();
                SpawnPlayerServerRpc(characterSpawnParameters);
            }
        }

        [Rpc(SendTo.Server)]
        public void SpawnPlayerServerRpc(CharacterSpawnParameters characterSpawnParameters)
        {
            Vector3 spawnPoint = GetRandomPlayerSpawnPoint();
            GameObject player = Instantiate(SettingsManager.Instance.player.configs[characterSpawnParameters.modelIndex].playerPrefab, spawnPoint, Quaternion.identity);

            player.GetComponent<CharacterIdentityControl>().SetSpawnParameters(characterSpawnParameters);

            //Spawn player with server ownership
            player.GetComponent<NetworkObject>().Spawn(true);

            //Change ownership
            //It's possible to spawn through SpawnWithOwnership() but it always spawns in (0,0,0)
            player.GetComponent<NetworkObject>().ChangeOwnership(characterSpawnParameters.ownerID);
        }

        [Rpc(SendTo.Server)]
        public void SpawnAIServerRpc(int modelIndex)
        {
            Vector3 spawnPoint = GetRandomAISpawnPoint();
            GameObject ai = Instantiate(SettingsManager.Instance.ai.configs[modelIndex].botPrefab, spawnPoint, Quaternion.identity);

            //Custom spawn parameters
            ai.GetComponent<CharacterIdentityControl>().SetSpawnParameters(new CharacterSpawnParameters()
            {
                ownerID = NetworkManager.Singleton.LocalClientId,
                modelIndex = modelIndex
            });

            //Spawn bot
            ai.GetComponent<NetworkObject>().Spawn(true);
        }

        public Vector3 GetRandomPlayerSpawnPoint()
        {
            return GetFreeSpawnPoint(userControl.playerSceneObjects, playerSpawnPoints);
        }

        public Vector3 GetRandomAISpawnPoint()
        {
            return GetFreeSpawnPoint(userControl.allCharacters, aiSpawnPoints);
        }

        private Vector3 GetFreeSpawnPoint(List<NetworkObject> characters, List<Transform> spawnPoints)
        {
            if (characters.Count == 0)
            {
                return spawnPoints[Random.Range(0, spawnPoints.Count)].position;
            }

            Transform freePoint = null;
            float freeSpaceRadiusAroundPoint = 0;

            //Check all spawn points for selected character type
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                Transform currenPoint = spawnPoints[0];
                float minDistanceToNearestPlayer = float.MaxValue;

                for (int j = 0; j < characters.Count; j++)
                {
                    if (characters[j] == null) continue;

                    float distanceToCharacter = (characters[j].transform.position - spawnPoints[i].position).magnitude;

                    //Check free space around point 
                    if (distanceToCharacter < minDistanceToNearestPlayer)
                    {
                        currenPoint = spawnPoints[i];
                        minDistanceToNearestPlayer = distanceToCharacter;
                    }
                }

                //Find one with maximum free space around it
                if (minDistanceToNearestPlayer > freeSpaceRadiusAroundPoint)
                {
                    freePoint = currenPoint;
                    freeSpaceRadiusAroundPoint = minDistanceToNearestPlayer;
                }
            }

            return freePoint.position;
        }
    }
}
