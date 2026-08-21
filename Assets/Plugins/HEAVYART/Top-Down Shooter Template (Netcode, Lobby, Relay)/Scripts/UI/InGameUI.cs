using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class InGameUI : MonoBehaviour
    {
        public RectTransform statusBarsContainer;
        public HUDController hudStatusBar;
        public EndOfGamePopupUIController endOfGamePopup;
        public RectTransform quitGamePopup;
        public Text countdownTextComponent;
        public RectTransform screenJoystick;

        private void Start()
        {
            GameManager.Instance.OnGameEnd += ShowEndOfGamePopup;

            if (Application.isMobilePlatform)
                screenJoystick.gameObject.SetActive(true);
        }

        public void ShowEndOfGamePopup()
        {
            HidePopups();
            endOfGamePopup.gameObject.SetActive(true);
        }

        public void ShowQuitGamePopup()
        {
            HidePopups();
            quitGamePopup.gameObject.SetActive(true);
        }

        public void HidePopups()
        {
            endOfGamePopup.gameObject.SetActive(false);
            quitGamePopup.gameObject.SetActive(false);
        }

        public void OnQuitButtonPressed()
        {
            GameManager.Instance.QuitGame();
        }

        public void OnRestartButtonPressed()
        {
            GameManager.Instance.RestartCurrentScene();
        }

        public void ShowHUD()
        {
            hudStatusBar.gameObject.SetActive(true);
        }

        public void HideHUD()
        {
            hudStatusBar.gameObject.SetActive(true);
        }

        private void FixedUpdate()
        {
            //Countdown
            if (GameManager.Instance.gameState == GameState.WaitingForCountdown)
            {
                TimeSpan timeSpan = TimeSpan.FromSeconds(GameManager.Instance.gameStartTime - NetworkManager.Singleton.ServerTime.Time);

                //Show seconds
                if (timeSpan.Seconds <= 3)
                    countdownTextComponent.text = "" + timeSpan.Seconds;

                //But if seconds are less than one, show "GO!" text
                if (timeSpan.Seconds == 0)
                    countdownTextComponent.text = "GO!";
            }
            else
                countdownTextComponent.text = string.Empty;
        }
    }
}
