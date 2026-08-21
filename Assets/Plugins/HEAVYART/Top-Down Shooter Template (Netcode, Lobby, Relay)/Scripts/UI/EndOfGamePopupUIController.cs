using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            UpdateLeaderboard();
        }

        public void OnRespawnButton()
        {
            GameManager.Instance.spawnControl.RespawnLocalPlayer();
            GameManager.Instance.UI.HidePopups();
        }


        private void UpdateLeaderboard()
        {
            string outputText = "";

            //Get user scores
            var sortedLeaderboard = GameManager.Instance.leaderboard.ToList();

            //Sort users by score
            sortedLeaderboard.Sort((a, b) => b.Value.score.CompareTo(a.Value.score));

            //Pack leaderboard in one string 
            foreach (var user in sortedLeaderboard)
            {
                outputText += user.Value.userName + " : " + user.Value.score + "\n";
            }

            //Show it on a screen
            leaderboardTextComponent.text = outputText;
        }
    }
}
