using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CC
{
    public class Option_Proportional_Sliders : MonoBehaviour, ICustomizerUI
    {
        private CharacterCustomization customizer;
        private CC_UI_Util parentUI;

        public List<string> Objects = new List<string>();
        public string MeshTag = "";
        public int MaterialIndex = -1;
        public float SliderHeight = 60;
        public string TextPrefix = "";
        public bool RemoveText = false;

        private float sliderSum;

        public GameObject SliderObject;

        private List<Slider> sliders = new List<Slider>();
        private List<Option_Slider> sliderObjects = new List<Option_Slider>();

        public void InitializeUIElement(CharacterCustomization customizerScript, CC_UI_Util ParentUI)
        {
            customizer = customizerScript;
            parentUI = ParentUI;

            for (int i = 0; i < Objects.Count; i++)
            {
                //Create sliders, assign to reference and add delegate
                Option_Slider sliderObject = Instantiate(SliderObject, this.transform).GetComponent<Option_Slider>();

                sliderObjects.Add(sliderObject);
                sliderObject.Property.propertyName = Objects[i];
                sliderObject.CustomizationType = Option_Slider.Type.Blendshape;
                sliderObject.InitializeUIElement(customizerScript, ParentUI);
                Slider slider = sliderObject.GetComponentInChildren<Slider>();
                sliders.Add(slider);
                slider.onValueChanged.AddListener(delegate { checkExcess(slider); });

                sliderObject.GetComponentInChildren<TextMeshProUGUI>().text = TextPrefix + (i + 1);

                sliderObject.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, SliderHeight);
                if (RemoveText) Destroy(sliderObject.GetComponentInChildren<TextMeshProUGUI>().gameObject);
            }

            gameObject.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (gameObject.GetComponent<RectTransform>().rect.height + (SliderHeight + gameObject.GetComponent<VerticalLayoutGroup>().spacing) * sliderObjects.Count));
        }

        public void RefreshUIElement()
        {
        }

        public void checkExcess(Slider mainSlider)
        {
            sliderSum = 0;

            foreach (Slider slider in sliders)
            {
                sliderSum = slider.value + sliderSum;
            }

            if (sliderSum > 1)
            {
                for (int i = 0; i < sliders.Count; i++)
                {
                    if (mainSlider != sliders[i])
                    {
                        distributeExcess(sliderSum - mainSlider.value, sliderSum - 1, i);
                    }
                }
            }
        }

        public void distributeExcess(float sum, float excess, int index)
        {
            sliders[index].SetValueWithoutNotify(sliders[index].value - (sliders[index].value / sum * excess));
            sliderObjects[index].setProperty(sliders[index].value);
        }
    }
}