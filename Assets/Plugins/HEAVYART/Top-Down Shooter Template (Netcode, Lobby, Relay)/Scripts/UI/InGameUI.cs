using System.Collections;
using System.Collections.Generic;
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
            if (countdownTextComponent != null)
                countdownTextComponent.gameObject.SetActive(false);
        }
    }
}
