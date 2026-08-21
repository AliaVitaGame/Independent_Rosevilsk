using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class WaitingForPlayersPopupUIController : MonoBehaviour
    {
        public Text playersCountTextComponent;
        public RectTransform quitButton;
        public InputField accessCodeTextComponent;

        private void FixedUpdate()
        {
            int playersCount = LobbyManager.Instance.players.Count;
            int maxPlayers = LobbyManager.Instance.maxPlayers;

            //For private game window
            if (accessCodeTextComponent != null)
            {
                //Show access code when it's available
                if (LobbyManager.Instance.isLobbyAvailable && LobbyManager.Instance.lobbyCode.Length > 0)
                    accessCodeTextComponent.text = LobbyManager.Instance.lobbyCode;
            }

            //Show status text
            if (LobbyManager.Instance.gameLaunchStatus == GameLaunchStatus.WaitingForPlayersResponses)
            {
                playersCountTextComponent.text = "Checking...";
                quitButton.gameObject.SetActive(false);
            }
            else if (LobbyManager.Instance.gameLaunchStatus == GameLaunchStatus.ReadyToLaunch)
            {
                playersCountTextComponent.text = "Launching...";
                quitButton.gameObject.SetActive(false);
            }
            else
            {
                if (playersCount == 0) //Not initialized yet
                    playersCountTextComponent.text = "Loading...";
                else
                    playersCountTextComponent.text = playersCount + " / " + maxPlayers;

                quitButton.gameObject.SetActive(true);
            }
        }

        public void QuitLobby()
        {
            LobbyManager.Instance.QuitLobby();
            MainMenuUIManager.Instance.ShowMainGamePanel();
            SceneLoadManager.Instance.UnsubscribeNetworkSceneUpdates();
        }
    }
}
