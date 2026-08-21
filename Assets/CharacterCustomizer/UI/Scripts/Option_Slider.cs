using UnityEngine;
using UnityEngine.UI;

namespace CC
{
    public class Option_Slider : MonoBehaviour, ICustomizerUI
    {
        public enum Type
        { Blendshape, Scalar, };

        public Type CustomizationType;

        public CC_Property Property;

        public Slider slider;

        private float defaultValue;

        private CharacterCustomization customizer;
        private CC_UI_Util parentUI;

        public void InitializeUIElement(CharacterCustomization customizerScript, CC_UI_Util ParentUI)
        {
            defaultValue = slider.value;
            customizer = customizerScript;
            parentUI = ParentUI;
            slider = GetComponentInChildren<Slider>();

            RefreshUIElement();
        }

        public void RefreshUIElement()
        {
            //Get saved value
            switch (CustomizationType)
            {
                case Type.Blendshape:
                    {
                        int savedIndex = customizer.StoredCharacterData.Blendshapes.FindIndex(t => t.propertyName == Property.propertyName);
                        if (savedIndex != -1)
                        {
                            slider.SetValueWithoutNotify(customizer.StoredCharacterData.Blendshapes[savedIndex].floatValue);
                        }
                        else
                        {
                            slider.SetValueWithoutNotify(defaultValue);
                        }
                        break;
                    }
                case Type.Scalar:
                    {
                        int savedIndex = customizer.StoredCharacterData.FloatProperties.FindIndex(t => t.propertyName == Property.propertyName && t.materialIndex == Property.materialIndex && t.meshTag == Property.meshTag);
                        if (savedIndex != -1)
                        {
                            slider.SetValueWithoutNotify(customizer.StoredCharacterData.FloatProperties[savedIndex].floatValue);
                        }
                        else
                        {
                            slider.SetValueWithoutNotify(defaultValue);
                        }
                        break;
                    }
            }
        }

        public void setProperty(float value)
        {
            Property.floatValue = value;

            switch (CustomizationType)
            {
                case Type.Blendshape:
                    {
                        customizer.setBlendshapeByName(Property.propertyName, value);
                        break;
                    }
                case Type.Scalar:
                    {
                        Debug.Log(value);
                        customizer.setFloatProperty(Property, true);
                        break;
                    }
            }
        }

        public void randomize()
        {
            slider.value = Random.Range(slider.minValue, slider.maxValue);
        }
    }
}