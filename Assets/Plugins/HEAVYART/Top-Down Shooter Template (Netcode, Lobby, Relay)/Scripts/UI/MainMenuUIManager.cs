using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class MainMenuUIManager : Singleton<MainMenuUIManager>
    {
        public RectTransform mainGamePanel;
        public RectTransform settingsPopup;
        public RectTransform joinGamePopup;
        public RectTransform startGamePopup;
        public RectTransform errorPopup;

        [Space]
        public RectTransform waitForPublicGamePopup;
        public RectTransform waitForPrivateGamePopup;

        private void Start()
        {
            LobbyManager.Instance.OnErrorOccurred += OnErrorOccurred;
        }

        private void OnDestroy()
        {
            if (LobbyManager.Instance != null)
                LobbyManager.Instance.OnErrorOccurred -= OnErrorOccurred;
        }

        public void ShowMainGamePanel()
        {
            HideAll();
            mainGamePanel.gameObject.SetActive(true);
        }

        public void ShowSettingsPopup()
        {
            HideAll();
            settingsPopup.gameObject.SetActive(true);
        }

        public void ShowWaitingForPublicGamePopup()
        {
            HideAll();
            waitForPublicGamePopup.gameObject.SetActive(true);
        }

        public void ShowWaitingForPrivateGamePopup()
        {
            HideAll();
            waitForPrivateGamePopup.gameObject.SetActive(true);
        }

        public void ShowJoinGamePopup()
        {
            HideAll();
            joinGamePopup.gameObject.SetActive(true);
        }

        public void ShowStartGamePopup()
        {
            HideAll();
            startGamePopup.gameObject.SetActive(true);
        }

        private void HideAll()
        {
            mainGamePanel.gameObject.SetActive(false);
            settingsPopup.gameObject.SetActive(false);
            joinGamePopup.gameObject.SetActive(false);
            startGamePopup.gameObject.SetActive(false);
            errorPopup.gameObject.SetActive(false);
            waitForPublicGamePopup.gameObject.SetActive(false);
            waitForPrivateGamePopup.gameObject.SetActive(false);
        }

        private void OnErrorOccurred(string errorText)
        {
            HideAll();
            errorPopup.gameObject.SetActive(true);
        }
    }
}
