using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class StartNewGamePopupUIController : MonoBehaviour
    {
        public Slider playersCountSlider;
        public Text PlayersCountText;

        public float playersCount { get; set; }

        void Start()
        {
            playersCount = SettingsManager.Instance.lobby.defaultPlayerCount;

            playersCountSlider.minValue = SettingsManager.Instance.lobby.minPlayers;
            playersCountSlider.maxValue = SettingsManager.Instance.lobby.maxPlayers;

            playersCountSlider.value = playersCount;
        }

        void FixedUpdate()
        {
            PlayersCountText.text = "NUMBER OF PLAYERS: " + playersCount;
        }

        public void OnStartPublicGame()
        {
            LobbyParameters lobbyParameters = new LobbyParameters();
            lobbyParameters.playersCount = (int)playersCount;
            lobbyParameters.isPublic = true; //public
            lobbyParameters.version = SettingsManager.Instance.common.projectVersion;

            LobbyManager.Instance.CreateLobby(lobbyParameters);
            MainMenuUIManager.Instance.ShowWaitingForPublicGamePopup();
        }

        public void OnStartPrivateGame()
        {
            LobbyParameters lobbyParameters = new LobbyParameters();
            lobbyParameters.playersCount = (int)playersCount;
            lobbyParameters.isPublic = false; //private
            lobbyParameters.version = SettingsManager.Instance.common.projectVersion;

            LobbyManager.Instance.CreateLobby(lobbyParameters);
            MainMenuUIManager.Instance.ShowWaitingForPrivateGamePopup();
        }

        public void CloseWindow()
        {
            gameObject.SetActive(false);
            MainMenuUIManager.Instance.ShowMainGamePanel();
        }
    }
}
