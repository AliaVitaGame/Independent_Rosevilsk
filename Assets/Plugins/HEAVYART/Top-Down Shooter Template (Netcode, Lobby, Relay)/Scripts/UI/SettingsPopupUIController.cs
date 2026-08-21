using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class SettingsPopupUIController : MonoBehaviour
    {
        public InputField nameField;
        public Dropdown selectedServerDropdown;
        public List<Button> colorButtons;

        public string nameInput
        {
            get => PlayerDataKeeper.name;
            set => PlayerDataKeeper.name = value;
        }

        private IEnumerator Start()
        {
            nameField.text = PlayerDataKeeper.name;

            InitializeColors();

            //Wait for available regions to update
            yield return new WaitUntil(() => LobbyManager.Instance.availableRegions.Count > 0);

            InitializeRegions();
        }

        private void InitializeColors()
        {
            //Set saved color as currently selected
            PickUpColor(PlayerDataKeeper.selectedColor);

            for (int i = 0; i < colorButtons.Count; i++)
            {
                colorButtons[i].GetComponent<Image>().color = SettingsManager.Instance.player.availableColors[i];

                //Avoiding lambda closure
                //https://en.wikipedia.org/wiki/Closure_(computer_programming)
                int buttonIndex = i;

                //Set color buttons callbacks
                colorButtons[i].onClick.AddListener(() =>
                {
                    PickUpColor(buttonIndex);
                });
            }
        }

        private void InitializeRegions()
        {
            //Convert regions list to dropdown menu elements
            selectedServerDropdown.options = LobbyManager.Instance.availableRegions.ConvertAll(x => new Dropdown.OptionData(x));

            //Set selected region
            selectedServerDropdown.value = ConvertSelectedRegionNameToIndex(LobbyManager.Instance.availableRegions);

            //Set region change callback
            selectedServerDropdown.onValueChanged.AddListener((updatedValue) =>
            {
                //Save changes to cache
                PlayerDataKeeper.selectedRegion = LobbyManager.Instance.availableRegions[updatedValue];
            });
        }

        private void PickUpColor(int index)
        {
            //Turn off all selection markers
            for (int i = 0; i < colorButtons.Count; i++)
                colorButtons[i].transform.GetChild(0).gameObject.SetActive(false);

            //Turn on selection marker on selected color button
            colorButtons[index].transform.GetChild(0).gameObject.SetActive(true);

            //Save changes to cache
            PlayerDataKeeper.selectedColor = index;
        }

        private int ConvertSelectedRegionNameToIndex(List<string> regions)
        {
            string currentlySelectedRegion = PlayerDataKeeper.selectedRegion;

            //Get region index (for dropdown menu)
            for (int i = 0; i < regions.Count; i++)
            {
                if (regions[i] == currentlySelectedRegion)
                {
                    return i;
                }
            }

            return 0;
        }

        public void CloseWindow()
        {
            MainMenuUIManager.Instance.ShowMainGamePanel();
        }
    }
}
