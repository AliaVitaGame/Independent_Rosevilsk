using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class HUDController : MonoBehaviour
    {
        public Slider healthStatusSlider;
        public Gradient healthColorGradient;

        [Space]
        public Text gameTimerTextComponent;
        public Gradient timeColor;

        [Space]
        public Image accuracyPowerUpIndicator;
        public Image fireRatePowerUpIndicator;
        public Image speedPowerUpIndicator;

        private Image healthFillImage;

        private void Awake()
        {
            healthStatusSlider.value = healthStatusSlider.maxValue;
            healthFillImage = healthStatusSlider.fillRect.GetComponent<Image>();
        }

        public void UpdateHealthAmount(float currentHP, float maxHP)
        {
            healthStatusSlider.value = currentHP / maxHP;
        }

        public void UpdateHealthBarColor()
        {
            healthFillImage.color = healthColorGradient.Evaluate(healthStatusSlider.value);
        }

        public void UpdatePowerUpIndicators(ModifiersControlSystem modifiersControlSystem, bool updateProgress)
        {
            //Accuracy powerup indicator
            ActiveModifierData accuracyModifier = modifiersControlSystem.GetModifier<ContinuousAccuracyModifier>();
            UpdateIndicator(accuracyPowerUpIndicator, accuracyModifier, updateProgress);

            //FireRate powerup indicator
            ActiveModifierData fireRateModifier = modifiersControlSystem.GetModifier<ContinuousFireRateModifier>();
            UpdateIndicator(fireRatePowerUpIndicator, fireRateModifier, updateProgress);

            //Speed powerup indicator
            ActiveModifierData speedModifier = modifiersControlSystem.GetModifier<ContinuousSpeedModifier>();
            UpdateIndicator(speedPowerUpIndicator, speedModifier, updateProgress);
        }

        private void UpdateIndicator(Image indicator, ActiveModifierData activeModifierData, bool updateProgress)
        {
            indicator.gameObject.SetActive(false);

            if (activeModifierData != null)
            {
                //Show circle progressbar
                indicator.gameObject.SetActive(true);

                //Set circle progressbar state
                if (updateProgress)
                    indicator.fillAmount = 1 - activeModifierData.GetCurrentProgress();
            }
        }

        private void FixedUpdate()
        {
            if (gameTimerTextComponent == null) return;

            //Game timer
            if (GameManager.Instance.gameState == GameState.ActiveGame)
            {
                TimeSpan timeSpan = TimeSpan.FromSeconds(GameManager.Instance.gameEndTime - NetworkManager.Singleton.ServerTime.Time);
                double gameDuration = GameManager.Instance.gameEndTime - GameManager.Instance.gameStartTime;
                double timeLeft = timeSpan.TotalSeconds;

                //Change text color
                gameTimerTextComponent.color = timeColor.Evaluate(1 - (float)(timeLeft / gameDuration));

                //Show how much time left
                gameTimerTextComponent.text = timeSpan.ToString(@"mm\:ss"); //Format 00:00
            }
            else
                gameTimerTextComponent.text = string.Empty;
        }
    }
}
