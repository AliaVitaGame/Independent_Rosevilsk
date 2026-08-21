using System;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;
using UnityEngine;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class LobbyActivityControl
    {
        public GameLaunchStatus gameLaunchStatus { get; private set; }

        private LobbyDataControl dataControl;
        private LobbyGameHostingControl gameHostingControl;

        private int lobbyHeartbeatRate;

        private int waitForPlayersToInitializeDelay;
        private int waitForPlayersReadyResponseDelay;
        private int waitForPlayersToRemoveDelay;

        private int fixedDeltaTime = (int)(Time.fixedDeltaTime * 1000);

        public LobbyActivityControl(LobbyDataControl dataControl, LobbyGameHostingControl gameHostingControl)
        {
            this.dataControl = dataControl;
            this.gameHostingControl = gameHostingControl;

            dataControl.OnLobbyUpdated += OnLobbyUpdated;

            dataControl.OnPlayerJoined += OnPlayerJoined;
            dataControl.OnPlayerLeft += OnPlayerLeft;

            Application.wantsToQuit += OnApplicationQuitAttempt;

            lobbyHeartbeatRate = SettingsManager.Instance.lobby.lobbyHeartbeatRate;
            waitForPlayersToInitializeDelay = SettingsManager.Instance.lobby.waitForPlayersToInitializeDelay;
            waitForPlayersReadyResponseDelay = SettingsManager.Instance.lobby.waitForPlayersReadyResponseDelay;
            waitForPlayersToRemoveDelay = SettingsManager.Instance.lobby.waitForPlayersToRemoveDelay;

            RunLobbyHeartbeatLoop();
        }

        public async void StartNewGame()
        {
            if (gameLaunchStatus != GameLaunchStatus.WaitingForPlayersToConnect) return;

            gameLaunchStatus = GameLaunchStatus.WaitingForPlayersToInitialize;

            //Wait for last joined player to initialize
            await Task.Delay(waitForPlayersToInitializeDelay);

            //"N0" means 0 numbers after floating point
            string checkNumber = Time.time.ToString("N0");

            //Start status check. Every user suppose to confirm it's readiness.
            SendStatusCheckRequest(checkNumber);

            //Wait for other users to confirm
            await Task.Delay(waitForPlayersReadyResponseDelay);

            //Final update 
            //Sometimes lobby misses events, so we update it manually
            dataControl.RefreshLobby();

            //Wait for lobby final update
            int finalUpdateDelay = 2000;
            await Task.Delay(finalUpdateDelay);

            //Check if local player didn't quit before
            if (dataControl.currentLobby == null) return;

            bool isReadyForGameStart = true;
            foreach (Player player in dataControl.currentLobby.Players)
            {
                //Remove users who didn't confirm (if there are some)
                if (player.Data.ContainsKey("ready") == false || player.Data["ready"].Value != checkNumber)
                {
                    Debug.Log("Player: " + player.Id + " has been removed.");
                    isReadyForGameStart = false;
                    await LobbyService.Instance.RemovePlayerAsync(dataControl.currentLobby.Id, player.Id);
                }
            }

            //Check if lobby is full
            if (dataControl.players.Count != dataControl.maxPlayers)
                isReadyForGameStart = false;

            if (isReadyForGameStart)
            {
                Debug.Log("Ready to host.");
                gameLaunchStatus = GameLaunchStatus.ReadyToLaunch;

                //Start game
                await gameHostingControl.HostGame(dataControl.currentLobby.MaxPlayers);

                Debug.Log("Game has been hosted.");

                //Hide lobby
                HideLobby();
            }
            else
            {
                gameLaunchStatus = GameLaunchStatus.UnableToLaunch;
                await Task.Delay(waitForPlayersToRemoveDelay);

                //Try again
                gameLaunchStatus = GameLaunchStatus.WaitingForPlayersToConnect;
            }
        }

        private void SendStatusCheckRequest(string checkNumber)
        {
            dataControl.UpdateLobbyParameters(new Dictionary<string, DataObject>()
            {
                {
                    "statusCheck", new DataObject(
                     visibility: DataObject.VisibilityOptions.Public,
                     value: checkNumber)
                },
            });
        }

        private void ResponseToStatusCheckRequest(string statusCheckNumber)
        {
            dataControl.UpdatePlayerParameters(new Dictionary<string, PlayerDataObject>()
            {
                {
                    "ready", new PlayerDataObject(
                     visibility: PlayerDataObject.VisibilityOptions.Member,
                     value: "" + statusCheckNumber)
                }
            });

            gameLaunchStatus = GameLaunchStatus.WaitingForPlayersResponses;
        }

        private void OnLobbyUpdated(Dictionary<string, string> lobbyChanges)
        {
            if (lobbyChanges.ContainsKey("statusCheck"))
            {
                //Response to status check automatically
                string updatedStatusCheck = lobbyChanges["statusCheck"];
                ResponseToStatusCheckRequest(updatedStatusCheck);
            }

            if (lobbyChanges.ContainsKey("joinCode"))
            {
                //Join code is stored. Connection accepted.
                gameLaunchStatus = GameLaunchStatus.ReadyToLaunch;

                if (dataControl.isLobbyOwner == false) //isClient
                {
                    Debug.Log("Join hosted game.");

                    //Join
                    gameHostingControl.JoinHostedGame();
                }
            }
        }

        private void OnPlayerJoined(Player player)
        {
            //Custom logic
        }

        private void OnPlayerLeft(int playerId)
        {
            if (dataControl.players.Count < dataControl.maxPlayers)
                if (gameLaunchStatus != GameLaunchStatus.ReadyToLaunch)
                    ResetGameLaunchStatus();
        }

        public async void RunLobbyHeartbeatLoop()
        {
            while (Application.isPlaying)
            {
                if (LobbyManager.Instance.isLobbyOwner == true)
                {
                    //Let lobby service know, current lobby still exists
                    await LobbyService.Instance.SendHeartbeatPingAsync(dataControl.currentLobby.Id);
                }

                await Task.Delay(lobbyHeartbeatRate);
            }
        }

        private async void HideLobby()
        {
            if (LobbyManager.Instance.isLobbyOwner)
            {
                UpdateLobbyOptions options = new UpdateLobbyOptions();
                options.IsLocked = true;
                options.IsPrivate = true;
                dataControl.currentLobby = await LobbyService.Instance.UpdateLobbyAsync(dataControl.currentLobby.Id, options);
            }
        }

        private async void ShowLobby()
        {
            if (LobbyManager.Instance.isLobbyOwner)
            {
                UpdateLobbyOptions options = new UpdateLobbyOptions();
                options.IsLocked = false;
                options.IsPrivate = false;
                dataControl.currentLobby = await LobbyService.Instance.UpdateLobbyAsync(dataControl.currentLobby.Id, options);
            }
        }

        public void ResetGameLaunchStatus()
        {
            gameLaunchStatus = GameLaunchStatus.WaitingForPlayersToConnect;
        }

        public bool OnApplicationQuitAttempt()
        {
            bool canQuit = dataControl.isLobbyAvailable == false;
            LeaveLobbyBeforeQuit();
            return canQuit;
        }

        private async void LeaveLobbyBeforeQuit()
        {
            //Quit lobby before closing application
            LobbyManager.Instance.QuitLobby();

            await Task.Delay(fixedDeltaTime);

            Application.Quit();
        }
    }
}
