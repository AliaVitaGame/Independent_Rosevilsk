using UnityEngine;

namespace CC
{
    public class Option_Grid_Picker : MonoBehaviour, ICustomizerUI
    {
        private CharacterCustomization customizer;

        public string[] ShapesYNeg;
        public string[] ShapesYPos;
        public string[] ShapesXNeg;
        public string[] ShapesXPos;

        public bool save = false;

        public Texture2D colorTexture;

        public enum Type
        {
            Blendshape, Color
        }

        public Type CustomizationType;

        public CC_Property ColorProperty;

        public void InitializeUIElement(CharacterCustomization customizerScript, CC_UI_Util parentUI)
        {
            customizer = customizerScript;

            //Get saved value
            switch (CustomizationType)
            {
                case Type.Blendshape:
                    {
                        break;
                    }
                case Type.Color:
                    {
                        break;
                    }
            }
        }

        public void RefreshUIElement()
        {
        }

        public void updateValue(Vector2 coords)
        {
            switch (CustomizationType)
            {
                case Type.Blendshape:
                    {
                        var newCoords = new Vector2(coords.x * 2 - 1, coords.y * 2 - 1);

                        foreach (string shape in ShapesYNeg)
                        {
                            customizer.setBlendshapeByName(shape, Mathf.Abs(Mathf.Clamp(newCoords.y, -1, 0)), save);
                        }

                        foreach (string shape in ShapesYPos)
                        {
                            customizer.setBlendshapeByName(shape, Mathf.Clamp(newCoords.y, 0, 1), save);
                        }

                        foreach (string shape in ShapesXPos)
                        {
                            customizer.setBlendshapeByName(shape, Mathf.Clamp(newCoords.x, 0, 1), save);
                        }

                        foreach (string shape in ShapesXNeg)
                        {
                            customizer.setBlendshapeByName(shape, Mathf.Abs(Mathf.Clamp(newCoords.x, -1, 0)), save);
                        }
                        break;
                    }
                case Type.Color:
                    {
                        int x = Mathf.RoundToInt(colorTexture.width * coords.x);
                        int y = Mathf.RoundToInt(colorTexture.height * coords.y);

                        Color color = colorTexture.GetPixel(x, y);
                        customizer.setColorProperty(ColorProperty, color, true);
                        break;
                    }
            }
        }
    }
}