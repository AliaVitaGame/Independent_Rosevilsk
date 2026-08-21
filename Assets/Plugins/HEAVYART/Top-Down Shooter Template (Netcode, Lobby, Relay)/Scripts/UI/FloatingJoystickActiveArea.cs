using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class FloatingJoystickActiveArea : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        public RectTransform joystick;
        private OnScreenStick onScreenStick;

        private Image handleImage;
        private Image backgroundImage;

        void Start()
        {
            onScreenStick = joystick.GetChild(0).GetComponent<OnScreenStick>();
            handleImage = onScreenStick.GetComponent<Image>();
            backgroundImage = joystick.GetComponent<Image>();

            HideJoystick();
        }
        public void OnDrag(PointerEventData eventData)
        {
            onScreenStick.OnDrag(eventData);
        }

        public void OnPointerDown(PointerEventData pointerEventData)
        {
            joystick.position = pointerEventData.position;
            onScreenStick.OnPointerDown(pointerEventData);
            ShowJoystick();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            onScreenStick.OnPointerUp(eventData);
            HideJoystick();
        }

        private void ShowJoystick()
        {
            Color handleImageColor = handleImage.color;
            Color backgroundImageColor = backgroundImage.color;

            handleImageColor.a = 1;
            backgroundImageColor.a = 1;

            handleImage.color = handleImageColor;
            backgroundImage.color = backgroundImageColor;
        }

        private void HideJoystick()
        {
            Color handleImageColor = handleImage.color;
            Color backgroundImageColor = backgroundImage.color;

            handleImageColor.a = 0;
            backgroundImageColor.a = 0;

            handleImage.color = handleImageColor;
            backgroundImage.color = backgroundImageColor;
        }

    }
}
