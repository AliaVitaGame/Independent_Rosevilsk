using System.Collections.Generic;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;
using Unity.Services.Relay.Models;
using UnityEngine;
using Unity.Services.Relay;
using Unity.Services.Qos;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using System;
using Unity.Services.Core;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class LobbyGameHostingControl
    {
        private LobbyDataControl dataControl;
        private UnityTransport unityTransport;

        public event Action OnRelayAllocationError;

        public List<string> availableRegions { get; private set; } = new List<string>();

        public LobbyGameHostingControl(LobbyDataControl dataControl, UnityTransport unityTransport)
        {
            this.dataControl = dataControl;
            this.unityTransport = unityTransport;
            UpdateRegions();
        }

        public async Task HostGame(int playersCount)
        {
            string selectedRegion = PlayerDataKeeper.selectedRegion;
            string joinCode;

            try
            {
                //Allocate game session in Relay service
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(playersCount, selectedRegion);

                //Get join code for other players to connect this game
                joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                //Link current network client to allocated game
                unityTransport.SetRelayServerData(allocation.ToRelayServerData("dtls"));
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                OnRelayAllocationError?.Invoke();
                return;
            }

            //Start game session
            NetworkManager.Singleton.StartHost();

            //Load game scene
            SceneLoadManager.Instance.LoadNetworkScene(PlayerDataKeeper.selectedScene);

            //Make sure host is on game scene before we allow other users to connect
            int delayBeforePublishingJoinCode = 1000;
            await Task.Delay(delayBeforePublishingJoinCode);

            //Store join code in lobby
            UpdateLobbyOptions options = new UpdateLobbyOptions();
            options.Data = new Dictionary<string, DataObject>()
        {
            {
              "joinCode", new DataObject(
              visibility: DataObject.VisibilityOptions.Member,
              value: joinCode)
            }
        };

            //Send join code
            dataControl.currentLobby = await LobbyService.Instance.UpdateLobbyAsync(dataControl.currentLobby.Id, options);
        }

        public async void JoinHostedGame()
        {
            //Get join code from lobby
            string joinCode = dataControl.currentLobby.Data["joinCode"].Value;

            try
            {
                //Find and join game session
                JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

                //Link current network client to allocated game
                unityTransport.SetRelayServerData(allocation.ToRelayServerData("dtls"));
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                OnRelayAllocationError?.Invoke();
                return;
            }

            //Join game session as client
            NetworkManager.Singleton.StartClient();

            //In this case we don't load scene manually.
            //Server automatically synchronizes active scene with all connected clients instead.
            SceneLoadManager.Instance.SubscribeOnNetworkSceneUpdates();
        }

        private async void UpdateRegions()
        {
            //Update regions once in few days
            if (PlayerDataKeeper.lastRegionsUpdateTime + new TimeSpan(SettingsManager.Instance.lobby.regionsUpdateRateHours, 0, 0) > DateTime.Now)
            {
                availableRegions = PlayerDataKeeper.availableRegions;
                return;
            }

            int fixedDeltaTime = (int)(Time.fixedDeltaTime * 1000);

            //Wait for initialization
            while (UnityServices.State != ServicesInitializationState.Initialized)
                await Task.Delay(fixedDeltaTime);

            //Wait for sign in
            while (AuthenticationService.Instance.IsSignedIn == false)
                await Task.Delay(fixedDeltaTime);

            //Get regions
            var regionSearchResult = await QosService.Instance.GetSortedQosResultsAsync("relay", null);

            foreach (var result in regionSearchResult)
            {
                Debug.Log("Add region: " + result.Region);
                availableRegions.Add(result.Region);
            }

            //Save regions
            PlayerDataKeeper.availableRegions = availableRegions;
            PlayerDataKeeper.selectedRegion = availableRegions[0];
            PlayerDataKeeper.lastRegionsUpdateTime = DateTime.Now;
        }
    }
}
