using UnityEngine;
using TMPro;

namespace CC
{
    public class CC_InputField : MonoBehaviour, ICustomizerUI
    {
        private CharacterCustomization customizer;

        public void InitializeUIElement(CharacterCustomization customizerScript, CC_UI_Util parentUI)
        {
            customizer = customizerScript;

            RefreshUIElement();

            gameObject.GetComponent<TMP_InputField>().onValueChanged.AddListener(customizer.setCharacterName);
        }

        public void RefreshUIElement()
        {
            gameObject.GetComponent<TMP_InputField>().text = customizer.CharacterName;
        }
    }
}