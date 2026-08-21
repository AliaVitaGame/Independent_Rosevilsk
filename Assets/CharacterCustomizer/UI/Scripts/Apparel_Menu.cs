using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CC
{
    public class Apparel_Menu : MonoBehaviour, ICustomizerUI
    {
        public GameObject ButtonPrefab;
        public GameObject Container;

        public TextMeshProUGUI OptionText;

        private CharacterCustomization customizer;

        private int navIndex = 0;
        private int optionsCount = 0;

        public void InitializeUIElement(CharacterCustomization customizerScript, CC_UI_Util parentUI)
        {
            customizer = customizerScript;
            RefreshUIElement();
        }

        public void setOption(int i)
        {
            navIndex = i;
            OptionText.text = customizer.ApparelTables[i].Label;

            foreach (Transform child in Container.transform)
            {
                Destroy(child.gameObject);
            }

            foreach (var item in customizer.ApparelTables[i].Items)
            {
                int Slot = i;
                for (int j = 0; j < item.Materials.Count; j++)
                {
                    string name = item.Name;
                    int matIndex = j;

                    GameObject Button = Instantiate(ButtonPrefab, Container.transform).gameObject;
                    Button.GetComponentInChildren<Button>().onClick.AddListener(delegate { customizer.setApparelByName(name, Slot, matIndex); });
                    Button.GetComponentInChildren<TextMeshProUGUI>().text = item.Materials[j].Label + " " + item.DisplayName;
                }

                if (item.Materials.Count == 0)
                {
                    string name = item.Name;

                    GameObject Button = Instantiate(ButtonPrefab, Container.transform).gameObject;
                    Button.GetComponentInChildren<Button>().onClick.AddListener(delegate { customizer.setApparelByName(name, Slot, 0); });
                    Button.GetComponentInChildren<TextMeshProUGUI>().text = item.DisplayName;
                }
            }
        }

        public void navLeft()
        {
            setOption(navIndex == 0 ? optionsCount - 1 : navIndex - 1);
        }

        public void navRight()
        {
            setOption(navIndex == optionsCount - 1 ? 0 : navIndex + 1);
        }

        public void RefreshUIElement()
        {
            optionsCount = customizer.ApparelTables.Count;
            setOption(0);
        }
    }
}