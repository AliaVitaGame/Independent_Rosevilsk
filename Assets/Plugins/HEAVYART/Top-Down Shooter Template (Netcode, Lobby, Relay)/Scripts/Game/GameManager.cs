using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class GameManager : NetworkBehaviour
    {
        public static GameManager Instance { get; private set; }
        public NetworkObjectsControl userControl { get; private set; }
        public NetworkObjectsSpawner spawnControl { get; private set; }
        public InGameUI UI { get; private set; }

        public GameState gameState { get; private set; }

        public Action OnNetworkReady;
        public Action OnGameStart;
        public Action OnGameEnd;
        public Action OnDisconnect;

        public double gameStartTime { get; private set; }
        private NetworkManager networkManager;
        private UnityTransport networkTransport;

        private void Awake()
        {
            Instance = this;
            userControl = GetComponent<NetworkObjectsControl>();
            spawnControl = GetComponent<NetworkObjectsSpawner>();
            UI = FindFirstObjectByType<InGameUI>();

            gameState = GameState.WaitingForPlayers;

            if (!TryInitializeNetworkDependencies())
            {
                enabled = false;
                return;
            }

            networkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
            networkTransport.OnTransportEvent += OnTransportEvent;

            SceneLoadManager.Instance.UnsubscribeNetworkSceneUpdates();

            StartCoroutine(WaitForNetworkReady());

            if (LobbyManager.Instance.isOfflineMode)
                StartCoroutine(StartOfflineHostAfterSceneProcessing());
        }

        private bool TryInitializeNetworkDependencies()
        {
            networkManager = NetworkManager.Singleton;
            if (networkManager == null)
            {
                Debug.LogError(
                    $"{nameof(GameManager)} requires {nameof(NetworkManager)}. " +
                    "Start the game from the Bootstrap scene instead of opening the gameplay scene directly.",
                    this);
                return false;
            }

            if (!networkManager.TryGetComponent(out networkTransport))
            {
                Debug.LogError(
                    $"{nameof(GameManager)} requires {nameof(UnityTransport)} on {nameof(NetworkManager)}.",
                    networkManager);
                return false;
            }

            if (SceneLoadManager.Instance == null || LobbyManager.Instance == null)
            {
                Debug.LogError(
                    $"{nameof(GameManager)} requires {nameof(SceneLoadManager)} and {nameof(LobbyManager)}. " +
                    "Start the game from the Bootstrap scene.",
                    this);
                return false;
            }

            return true;
        }

        private IEnumerator StartOfflineHostAfterSceneProcessing()
        {
            // Let Unity finish marking the in-scene NetworkObject before Netcode
            // scans the loaded scene. Starting the host from Awake races that pass.
            yield return null;

            if (!networkManager.IsListening && !networkManager.StartHost())
                Debug.LogError("Unable to start the single-player host.");
        }

        IEnumerator WaitForNetworkReady()
        {
            //OnNetworkSpawn is not working if gameObject was placed on scene instead of Instantiate
            //Here is the way to avoid this problem
            while (IsSpawned == false) yield return 0;

            OnNetworkReady?.Invoke();
        }

        private new void OnDestroy()
        {
            if (networkManager != null)
                networkManager.OnClientDisconnectCallback -= OnClientDisconnectCallback;

            if (networkTransport != null)
                networkTransport.OnTransportEvent -= OnTransportEvent;
        }

        void FixedUpdate()
        {
            //Handle waiting for players
            if (gameState == GameState.WaitingForPlayers)
            {
                int connectedPlayersCount = userControl.playerSceneObjects.Count;
                int expectedPlayersCount = LobbyManager.Instance.isOfflineMode
                    ? 1
                    : LobbyManager.Instance.players.Count;

                //Room is full
                if (connectedPlayersCount >= expectedPlayersCount)
                {
                    if (IsServer)
                        StartSandboxRpc(NetworkManager.ServerTime.Time);
                }
            }
        }

        [Rpc(SendTo.Everyone)]
        private void StartSandboxRpc(double startTime)
        {
            gameStartTime = startTime;
            gameState = GameState.ActiveGame;
            OnGameStart?.Invoke();
        }

        public void RegisterCharacterDeath(ulong playerID)
        {
            // Sandbox gameplay has no score or win-condition tracking.
        }

        public void AddLeaderboardUser(LeaderboardUserProfile userProfile)
        {
            // Kept as a compatibility entry point for existing player setup.
        }

        private void OnClientDisconnectCallback(ulong clientID)
        {
            if (clientID == NetworkManager.Singleton.LocalClientId)
            {
                Debug.Log("Local client has been closed");
                OnDisconnect?.Invoke();

                gameState = GameState.GameIsOver;
                OnGameEnd?.Invoke();
            }

            if (clientID == 0)
            {
                Debug.Log("Server has been closed.");
                OnDisconnect?.Invoke();

                gameState = GameState.GameIsOver;
                OnGameEnd?.Invoke();
            }
        }

        public void QuitGame()
        {
            LobbyManager.Instance.QuitLobby();
            NetworkManager.Singleton.Shutdown();

            SceneLoadManager.Instance.LoadRegularScene("MainMenu", true);
        }

        public void RestartCurrentScene()
        {
            LobbyManager.Instance.QuitLobby();
            NetworkManager.Singleton.Shutdown();

            SceneLoadManager.Instance.LoadRegularScene(SceneManager.GetActiveScene().name, false);
        }

        private void OnTransportEvent(NetworkEvent eventType, ulong clientId, ArraySegment<byte> payload, float receiveTime)
        {
            //On connection lost (no internet)
            if (eventType == NetworkEvent.TransportFailure)
            {
                Debug.Log("Disconnected");
                LobbyManager.Instance.QuitLobby();
                SceneLoadManager.Instance.LoadRegularScene("MainMenu", true);
            }
        }

        void OnApplicationPause(bool pauseStatus)
        {
            //On hide application
            //Disconnects immediate
            if (pauseStatus == true) QuitGame();
        }
    }
}
