using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class MainGamePanelUIController : MonoBehaviour
    {
        private bool isStartingSoloGame;

        public void StartQuickGame()
        {
            LobbyParameters lobbyParameters = new LobbyParameters();
            lobbyParameters.playersCount = SettingsManager.Instance.lobby.defaultPlayerCount;
            lobbyParameters.version = SettingsManager.Instance.common.projectVersion;

            LobbyManager.Instance.JoinOrCreateLobby(lobbyParameters);
            MainMenuUIManager.Instance.ShowWaitingForPublicGamePopup();
        }

        public void StartSoloGame()
        {
            if (isStartingSoloGame)
                return;

            isStartingSoloGame = true;

            if (PlayerDataKeeper.selectedScene == "none")
                PlayerDataKeeper.selectedScene = SettingsManager.Instance.gameplay.defaultGameSceneName;

            LobbyManager.Instance.StartSinglePlayer();
        }
    }
}
