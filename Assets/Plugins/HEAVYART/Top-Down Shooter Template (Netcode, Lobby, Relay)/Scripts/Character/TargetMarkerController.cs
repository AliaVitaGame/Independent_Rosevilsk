using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class TargetMarkerController : MonoBehaviour
    {
        public float activityDuration = 0.25f;
        public float fadeDuration = 0.5f;
        public float colorLerpFactor = 0.25f;
        public float aimingDistance = 10;
        public Renderer targetMarker;

        public bool isLocalPlayer => identityControl.IsLocalPlayer;

        private CharacterIdentityControl identityControl;
        private WeaponControlSystem weaponControlSystem;

        private float lastActivationTime = 0;

        private Color targetMarkerDefaultColor;
        private Color targetMarkerInactiveColor;

        private void Awake()
        {
            identityControl = GetComponent<CharacterIdentityControl>();
            weaponControlSystem = GetComponent<WeaponControlSystem>();

            targetMarker.gameObject.SetActive(true);

            targetMarkerDefaultColor = targetMarker.material.color;
            targetMarkerInactiveColor = targetMarkerDefaultColor;
            targetMarkerInactiveColor.a = 0;
        }

        private void FixedUpdate()
        {
            //Start fade
            if (Time.time > lastActivationTime + activityDuration)
            {
                //End fade and turn off marker
                if (Time.time > lastActivationTime + activityDuration + fadeDuration)
                    targetMarker.gameObject.SetActive(false);
                else
                    targetMarker.material.color = Color.Lerp(targetMarker.material.color, targetMarkerInactiveColor, colorLerpFactor); //Fade (lerp color)
            }

            //Check targets in line of sight
            if (Physics.Raycast(weaponControlSystem.lineOfSightTransform.position, weaponControlSystem.lineOfSightTransform.forward, out RaycastHit hit, aimingDistance))
            {
                TargetMarkerController otherCharacter = hit.transform.GetComponent<TargetMarkerController>();

                //Target found
                if (otherCharacter != null)
                {
                    //Marker could be enabled in two cases:
                    //1. We enable in on any other character (bot or player).
                    //2. Someone enabled it on us.
                    //Bots and other players can't enable it on each other, because it's suppose to be a local marker, related to current player only. 
                    if (identityControl.IsLocalPlayer || otherCharacter.isLocalPlayer)
                        otherCharacter.EnableTargetMarker(); //Enable marker 
                }
            }
        }

        public void EnableTargetMarker()
        {
            lastActivationTime = Time.time;

            targetMarker.gameObject.SetActive(true);
            targetMarker.material.color = targetMarkerDefaultColor;
        }
    }
}
