using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CC
{
    public class Grid_Icon_Generator : MonoBehaviour, ICustomizerUI
    {
        public enum Type
        { Apparel, Hair };

        public Type CustomizationType;

        public int Slot = 0;

        public int MinIcons = 9;

        public GameObject Prefab;

        public List<string> Objects = new List<string>();

        private CharacterCustomization customizer;

        private List<Grid_Icon> gridIcons = new List<Grid_Icon>();

        public void InitializeUIElement(CharacterCustomization customizerScript, CC_UI_Util parentUI)
        {
            customizer = customizerScript;

            switch (CustomizationType)
            {
                case Type.Apparel:
                    {
                        var items = customizer.ApparelTables[Slot].Items;
                        for (int i = 0; i < items.Count; i++)
                        {
                            string name = items[i].Name;
                            int index = i;
                            Grid_Icon gridIcon = Instantiate(Prefab, this.transform).GetComponent<Grid_Icon>();
                            gridIcon.GetComponent<Button>().onClick.AddListener(delegate { customizer.setApparelByName(name, Slot, 0); });
                            gridIcon.GetComponent<Button>().onClick.AddListener(delegate { updateSelectedState(index); });
                            gridIcon.name = name;
                            gridIcons.Add(gridIcon);

                            //Set icon
                            gridIcon.setIcon(items[i].Icon);
                        }
                        break;
                    }
                case Type.Hair:
                    {
                        var items = customizer.HairTables[Slot].Hairstyles;
                        for (int i = 0; i < items.Count; i++)
                        {
                            string name = items[i].Name;
                            int index = i;
                            Grid_Icon gridIcon = Instantiate(Prefab, this.transform).GetComponent<Grid_Icon>();
                            gridIcon.GetComponent<Button>().onClick.AddListener(delegate { customizer.setHairByName(name, Slot); });
                            gridIcon.GetComponent<Button>().onClick.AddListener(delegate { updateSelectedState(index); });
                            gridIcon.name = name;
                            gridIcons.Add(gridIcon);

                            //Set icon
                            gridIcon.setIcon(items[i].Icon);
                        }
                        break;
                    }
            }

            RefreshUIElement();

            while (transform.childCount < 9)
            {
                Grid_Icon gridIcon = Instantiate(Prefab, this.transform).GetComponent<Grid_Icon>();
                gridIcon.GetComponent<Button>().interactable = false;
            }
        }

        public void RefreshUIElement()
        {
            //Get saved value
            string savedName = "";

            switch (CustomizationType)
            {
                case Type.Apparel:
                    {
                        savedName = customizer.StoredCharacterData.ApparelNames[Slot];
                        break;
                    }
                case Type.Hair:
                    {
                        savedName = customizer.StoredCharacterData.HairNames[Slot];
                        break;
                    }
            }
            foreach (Grid_Icon gridIcon in gridIcons)
            {
                gridIcon.setSelected(savedName == gridIcon.name);
            }
        }

        public void updateSelectedState(int index)
        {
            for (int i = 0; i < gridIcons.Count; i++)
            {
                gridIcons[i].setSelected(i == index);
            }
        }
    }
}