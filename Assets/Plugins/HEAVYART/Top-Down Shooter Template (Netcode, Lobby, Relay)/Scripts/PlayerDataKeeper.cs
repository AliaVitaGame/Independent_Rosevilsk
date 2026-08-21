using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    public static class PlayerDataKeeper
    {
        //Unique authentication name
        public static string authProfileName { get; set; }

        public static string name
        {
            get
            {
                if (PlayerPrefs.GetString(authProfileName + "playerName", string.Empty) == string.Empty)
                    PlayerPrefs.SetString(authProfileName + "playerName", "Player" + UnityEngine.Random.Range(1, 9999));

                return PlayerPrefs.GetString(authProfileName + "playerName");
            }
            set => PlayerPrefs.SetString(authProfileName + "playerName", value);
        }

        public static string selectedRegion
        {
            get => PlayerPrefs.GetString(authProfileName + "selectedRegion", string.Empty);
            set => PlayerPrefs.SetString(authProfileName + "selectedRegion", value);
        }

        public static List<string> availableRegions
        {
            get => JsonConvert.DeserializeObject<List<string>>(PlayerPrefs.GetString(authProfileName + "availableRegions", "[]"));
            set => PlayerPrefs.SetString(authProfileName + "availableRegions", JsonConvert.SerializeObject(value));
        }
        public static DateTime lastRegionsUpdateTime
        {
            get => Convert.ToDateTime(PlayerPrefs.GetString(authProfileName + "lastRegionsUpdateTime", DateTime.MinValue.ToString()));
            set => PlayerPrefs.SetString(authProfileName + "lastRegionsUpdateTime", value.ToString());
        }

        public static int selectedColor
        {
            get => PlayerPrefs.GetInt(authProfileName + "selectedColor", -1);
            set => PlayerPrefs.SetInt(authProfileName + "selectedColor", value);
        }

        public static int selectedPrefab
        {
            get => PlayerPrefs.GetInt(authProfileName + "selectedPrefab", 0);
            set => PlayerPrefs.SetInt(authProfileName + "selectedPrefab", value);
        }

        public static string selectedScene
        {
            get => PlayerPrefs.GetString(authProfileName + "selectedScene", "none");
            set => PlayerPrefs.SetString(authProfileName + "selectedScene", value);
        }
    }
}
