using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class PlayerSettings : MonoBehaviour
    {
        public List<PlayerConfig> configs = new List<PlayerConfig>();

        [Space()]
        public List<Color> availableColors;

        private IEnumerator Start()
        {
            //Wait for initialize
            while (UnityServices.State != ServicesInitializationState.Initialized)
                yield return 0;

            //Wait for sign in
            while (AuthenticationService.Instance.IsSignedIn == false)
                yield return 0;

            if (PlayerDataKeeper.selectedColor == -1)
                PlayerDataKeeper.selectedColor = Random.Range(0, availableColors.Count);
        }

        public Color GetPlayerColor()
        {
            if (PlayerDataKeeper.selectedColor == -1)
                PlayerDataKeeper.selectedColor = Random.Range(0, availableColors.Count);

            return availableColors[PlayerDataKeeper.selectedColor];
        }
    }
}
