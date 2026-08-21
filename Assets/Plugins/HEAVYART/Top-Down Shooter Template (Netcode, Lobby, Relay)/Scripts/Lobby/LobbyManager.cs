using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class LobbyManager : Singleton<LobbyManager>
    {
        private LobbyActivityControl activityControl;
        private LobbyDataControl dataControl;
        private LobbyGameHostingControl gameHostingControl;

        public List<Player> players => dataControl.players;
        public int maxPlayers => dataControl.maxPlayers;

        public bool isLobbyAvailable => dataControl.isLobbyAvailable;
        public bool isLobbyOwner => dataControl.isLobbyOwner;
        public string lobbyCode => dataControl.currentLobby.LobbyCode;

        public bool isOfflineMode { get; set; }

        public List<string> availableRegions => gameHostingControl.availableRegions;
        public GameLaunchStatus gameLaunchStatus => activityControl.gameLaunchStatus;

        public event Action<string> OnErrorOccurred;

        private void Start()
        {
            dataControl = new LobbyDataControl();
            gameHostingControl = new LobbyGameHostingControl(dataControl, GetComponent<UnityTransport>());
            activityControl = new LobbyActivityControl(dataControl, gameHostingControl);

            dataControl.OnLobbyNoLongerExistsError += OnLobbyNoLongerExistsError;
            dataControl.OnLocalPlayerRemovedError += OnLocalPlayerRemovedError;
            gameHostingControl.OnRelayAllocationError += OnRelayAllocationError;

            NetworkManager.Singleton.GetComponent<UnityTransport>().OnTransportEvent += OnTransportEvent;

            if (SceneManager.GetActiveScene().name == "Demo")
            {
                isOfflineMode = true;
                NetworkManager.Singleton.StartHost();
            }
        }

        public async void JoinOrCreateLobby(LobbyParameters lobbyParameters, Action onSuccess = null, Action<string> onFail = null)
        {
            //Search options
            QuickJoinLobbyOptions options = new QuickJoinLobbyOptions();
            options.Filter = new List<QueryFilter>();

            //Game mode filter
            QueryFilter gameModeFilter = new QueryFilter(QueryFilter.FieldOptions.S1, lobbyParameters.mode, QueryFilter.OpOptions.EQ);
            options.Filter.Add(gameModeFilter);

            //Region filter
            QueryFilter regionFilter = new QueryFilter(QueryFilter.FieldOptions.S2, PlayerDataKeeper.selectedRegion, QueryFilter.OpOptions.EQ);
            options.Filter.Add(regionFilter);

            //Version filter
            QueryFilter versionFilter = new QueryFilter(QueryFilter.FieldOptions.S3, lobbyParameters.version, QueryFilter.OpOptions.EQ);
            options.Filter.Add(versionFilter);

            //Player (lobby user)
            options.Player = dataControl.GeneratePlayerObject(PlayerDataKeeper.name);

            try
            {
                //Try join any free lobby using search parameters
                dataControl.currentLobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);

                //Subscribe to lobby updates
                dataControl.InitializeLobbyCallbacks();

                onSuccess?.Invoke();

                Debug.Log("Successfully connected.");
                Debug.Log("Status: " + gameLaunchStatus);

                //Joined player will wait for lobby updates
                //Check LobbyActivityControl.OnLobbyUpdated method for more details
            }
            catch (Exception exception)
            {
                onFail?.Invoke(exception.Message);
                Debug.Log("No free lobby found. Creating new one.");

                //It seems there are no appropriate lobbies, so we got to create new one
                CreateLobby(lobbyParameters, onSuccess);
            }

            isOfflineMode = false;
        }

        public async void CreateLobby(LobbyParameters lobbyParameters, Action onSuccess = null)
        {
            string lobbyName = "lobby"; //default name.

            CreateLobbyOptions options = new CreateLobbyOptions();
            options.IsPrivate = !lobbyParameters.isPublic; //Is it free to join?
            options.Player = dataControl.GeneratePlayerObject(PlayerDataKeeper.name);

            //Add search parameters, for other players to find this lobby
            options.Data = new Dictionary<string, DataObject>
        {
               {
                   "mode",new DataObject(
                    visibility: DataObject.VisibilityOptions.Public,
                    value: lobbyParameters.mode, //game mode
                    index: DataObject.IndexOptions.S1) //search parameters suppose to be indexed
               },
               {
                   "region",new DataObject(
                    visibility: DataObject.VisibilityOptions.Public,
                    value: PlayerDataKeeper.selectedRegion, //game region
                    index: DataObject.IndexOptions.S2) //indexed
               },
               {
                   "version",new DataObject(
                    visibility: DataObject.VisibilityOptions.Public,
                    value: lobbyParameters.version, //project version
                    index: DataObject.IndexOptions.S3) //indexed
               }
        };

            //Create (and join) new lobby
            dataControl.currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, lobbyParameters.playersCount, options);

            //Subscribe to lobby updates
            dataControl.InitializeLobbyCallbacks();

            isOfflineMode = false;

            //Send callback
            onSuccess?.Invoke();
            Debug.Log("Lobby created. Access code: " + dataControl.currentLobby.LobbyCode);
            Debug.Log("Status: " + gameLaunchStatus);

            //Lobby owner will run StartNewGame scenario after lobby becomes full
            //Check FixedUpdate method below
        }

        public async void JoinLobbyWithAccessCode(string code, Action onSuccess = null, Action<string> onFail = null)
        {
            try
            {
                //Find and join lobby by access code
                dataControl.currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code);

                //Subscribe to lobby updates
                dataControl.InitializeLobbyCallbacks();

                //Send callback
                onSuccess?.Invoke();

            }
            catch (Exception exception)
            {
                //Send callback with error info
                onFail?.Invoke(exception.Message);
            }

            isOfflineMode = false;
        }

        public void StartSinglePlayer()
        {
            isOfflineMode = true;

            SceneLoadManager.Instance.LoadRegularScene(PlayerDataKeeper.selectedScene);
        }

        private void FixedUpdate()
        {
            //Wait until lobby becomes full and ready to start game
            if (isLobbyOwner == true && players.Count == maxPlayers)
            {
                activityControl.StartNewGame();
            }
        }

        public async void QuitLobby()
        {
            if (isLobbyAvailable == false)
                return;

            string playerId = AuthenticationService.Instance.PlayerId;
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(dataControl.currentLobby.Id, playerId);
            }
            catch
            {
                Debug.Log("Unable to remove player. It seems it's already removed.");
            }

            dataControl.currentLobby = null;
            activityControl.ResetGameLaunchStatus();

            Debug.Log("Quit lobby");
        }

        private void OnLobbyNoLongerExistsError()
        {
            OnErrorOccurred?.Invoke("Lobby is no longer exists.");
            QuitLobby();
            //Custom logic
        }

        private void OnLocalPlayerRemovedError()
        {
            OnErrorOccurred?.Invoke("Local player was removed.");
            QuitLobby();
            //Custom logic
        }

        private void OnRelayAllocationError()
        {
            OnErrorOccurred?.Invoke("Error on Relay service.");
            QuitLobby();
            //Custom logic
        }

        private void OnTransportEvent(NetworkEvent eventType, ulong clientId, ArraySegment<byte> payload, float receiveTime)
        {
            if (eventType == NetworkEvent.Disconnect && GameManager.Instance == null)
            {
                OnErrorOccurred?.Invoke("Failed to connect to server.");
                QuitLobby();
            }
        }
    }
}
