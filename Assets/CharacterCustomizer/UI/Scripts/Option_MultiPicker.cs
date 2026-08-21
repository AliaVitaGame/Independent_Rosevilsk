using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// This is a special picker for setting multiple properties (usually textures) in one swoop
/// For example a character might have different textures for different parts of the body that should be set by a single picker
/// Every element in Properties should have a property (_Head_Texture for example) and then all of the different head textures
/// </summary>

namespace CC
{
    [System.Serializable]
    public class MultiPicker
    {
        public CC_Property Property;
        public List<string> Objects = new List<string>();
    }

    public class Option_MultiPicker : MonoBehaviour, ICustomizerUI
    {
        private CharacterCustomization customizer;
        private CC_UI_Util parentUI;

        public List<MultiPicker> Properties = new List<MultiPicker>();

        public enum Type
        { Texture };

        public Type CustomizationType;

        public int Slot = 0;

        public TextMeshProUGUI OptionText;

        private int navIndex = 0;
        private int optionsCount = 0;

        public void InitializeUIElement(CharacterCustomization customizerScript, CC_UI_Util ParentUI)
        {
            customizer = customizerScript;
            parentUI = ParentUI;

            RefreshUIElement();
        }

        public void RefreshUIElement()
        {
            switch (CustomizationType)
            {
                case Type.Texture:
                    {
                        optionsCount = Properties[0].Objects.Count;
                        getSavedOption(customizer.StoredCharacterData.TextureProperties);
                        break;
                    }
            }
        }

        public void getSavedOption(List<CC_Property> savedProps)
        {
            var property0 = Properties[0].Property;
            int savedIndex = savedProps.FindIndex(t => t.propertyName == property0.propertyName && t.materialIndex == property0.materialIndex && t.meshTag == property0.meshTag);
            if (savedIndex != -1)
            {
                navIndex = Properties[0].Objects.FindIndex(t => t == savedProps[savedIndex].stringValue);
            }
            else navIndex = 0;
            updateOptionText();
        }

        public void updateOptionText()
        {
            OptionText.SetText((navIndex + 1) + "/" + optionsCount);
        }

        public void setOption(int j)
        {
            navIndex = j;

            updateOptionText();

            switch (CustomizationType)
            {
                case Type.Texture:
                    {
                        for (int i = 0; i < Properties.Count; i++)
                        {
                            Properties[i].Property.stringValue = Properties[i].Objects[j];
                            customizer.setTextureProperty(Properties[i].Property, true);
                        }
                        break;
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

        public void randomize()
        {
            setOption(Random.Range(0, optionsCount));
        }
    }
}