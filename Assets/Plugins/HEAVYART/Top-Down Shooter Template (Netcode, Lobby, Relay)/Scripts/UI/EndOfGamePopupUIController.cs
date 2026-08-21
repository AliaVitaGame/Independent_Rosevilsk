using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class EndOfGamePopupUIController : MonoBehaviour
    {
        public Button respawnButton;
        public Button quitButton;
        public Text leaderboardTextComponent;

        private void OnEnable()
        {
            if (GameManager.Instance.gameState == GameState.GameIsOver)
                respawnButton.gameObject.SetActive(false);
        }

        private void FixedUpdate()
        {
            if (leaderboardTextComponent != null)
                leaderboardTextComponent.gameObject.SetActive(false);
        }

        public void OnRespawnButton()
        {
            GameManager.Instance.spawnControl.RespawnLocalPlayer();
            GameManager.Instance.UI.HidePopups();
        }
    }
}
