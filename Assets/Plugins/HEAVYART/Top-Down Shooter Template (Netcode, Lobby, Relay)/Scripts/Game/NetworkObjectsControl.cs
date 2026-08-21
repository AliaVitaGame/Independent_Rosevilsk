using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class NetworkObjectsControl : MonoBehaviour
    {
        public List<NetworkObject> userServiceObjects = new List<NetworkObject>();

        //Characters in scene stored in different collection for more convenient access
        public List<NetworkObject> playerSceneObjects = new List<NetworkObject>();
        public List<NetworkObject> aiSceneObjects = new List<NetworkObject>();
        public List<NetworkObject> allCharacters = new List<NetworkObject>();

        public NetworkObject userServiceObject { get; private set; }
        public NetworkObject localPlayer => playerSceneObjects.Find(x => x.IsOwner);

        public void AddUserServiceObject(NetworkObject networkObject, string userName)
        {
            if (networkObject.IsOwner)
                userServiceObject = networkObject;

            userServiceObjects.Add(networkObject);

            //Register current session user in leaderboard
            LeaderboardUserProfile userProfile = new LeaderboardUserProfile();
            userProfile.id = networkObject.OwnerClientId;
            userProfile.userName = userName;

            GameManager.Instance.AddLeaderboardUser(userProfile);
        }

        public void AddPlayerObject(NetworkObject networkObject)
        {
            playerSceneObjects.Add(networkObject);
            allCharacters.Add(networkObject);
        }
        public void AddAIObject(NetworkObject networkObject)
        {
            aiSceneObjects.Add(networkObject);
            allCharacters.Add(networkObject);
        }

        public void RemoveNetworkObject(NetworkObject networkObject)
        {
            userServiceObjects.Remove(networkObject);
            playerSceneObjects.Remove(networkObject);
            aiSceneObjects.Remove(networkObject);
            allCharacters.Remove(networkObject);
        }

        public NetworkObject FindCharacterByID(ulong id)
        {
            NetworkObject result = playerSceneObjects.Find(player => player.NetworkObjectId == id);

            if (result == null)
                result = aiSceneObjects.Find(ai => ai.NetworkObjectId == id);

            if (result == null)
                result = userServiceObjects.Find(so => so.NetworkObjectId == id);

            return result;
        }

        public NetworkObject FindCharacterByOwnerID(ulong id)
        {
            return playerSceneObjects.Find(player => player.OwnerClientId == id);
        }
    }
}