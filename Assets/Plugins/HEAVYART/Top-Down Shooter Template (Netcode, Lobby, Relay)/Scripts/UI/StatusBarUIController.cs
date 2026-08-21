using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class StatusBarUIController : MonoBehaviour
    {
        public Slider healthStatusSlider;
        public Gradient healthColorGradient;

        [Space]
        public Image accuracyPowerUpIndicator;
        public Image fireRatePowerUpIndicator;
        public Image speedPowerUpIndicator;

        public float verticalOffset = 1.5f;

        [Space]
        public Text userNameTextComponent;

        private Transform linkedTransform;
        private Camera mainCamera;

        private void Awake()
        {
            healthStatusSlider.value = healthStatusSlider.maxValue;

            mainCamera = Camera.main;
            transform.localScale = Vector3.one;

            if (linkedTransform != null)
                transform.position = mainCamera.WorldToScreenPoint(linkedTransform.position);
        }

        public void LinkTransform(Transform linkedTransform)
        {
            this.linkedTransform = linkedTransform;

            UpdatePosition();
        }

        public void UpdateHealthAmount(float currentHP, float maxHP)
        {
            healthStatusSlider.value = currentHP / maxHP;
        }

        public void UpdatePosition()
        {
            float fixedWorldSpaceVerticalOffset = 1.5f;
            transform.position = mainCamera.WorldToScreenPoint(linkedTransform.position + Vector3.up * fixedWorldSpaceVerticalOffset) + Vector3.up * verticalOffset;
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

        public void ShowUserName(string userName)
        {
            if (userNameTextComponent != null)
            {
                userNameTextComponent.gameObject.SetActive(true);
                userNameTextComponent.text = userName;
            }
        }
    }
}
