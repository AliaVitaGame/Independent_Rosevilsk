using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class JoinGamePopupUIController : MonoBehaviour
    {
        public string accessCode { get; set; } = "";

        public Button joinButton;
        public Text processingTextComponent;
        public Text errorTextComponent;
        public RectTransform startGamePanel;

        public void OnJoinButtonClicked()
        {
            if (accessCode.Length < 6)
            {
                OnFail("Code suppose to contain at least 6 symbols.");
                return;
            }

            joinButton.interactable = false;
            processingTextComponent.gameObject.SetActive(true);
            errorTextComponent.gameObject.SetActive(false);

            LobbyManager.Instance.JoinLobbyWithAccessCode(accessCode, OnSuccess, OnFail);
        }

        private void OnSuccess()
        {
            MainMenuUIManager.Instance.ShowWaitingForPublicGamePopup();
        }

        private void OnFail(string reason)
        {
            errorTextComponent.gameObject.SetActive(true);
            processingTextComponent.gameObject.SetActive(false);
            joinButton.interactable = true;
            errorTextComponent.text = reason;
        }
    }
}
