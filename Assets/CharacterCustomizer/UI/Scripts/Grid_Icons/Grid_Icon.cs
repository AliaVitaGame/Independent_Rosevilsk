using UnityEngine;
using UnityEngine.UI;

namespace CC
{
    public class Grid_Icon : MonoBehaviour
    {
        public string Name;
        public int Slot;

        public GameObject Icon;
        public GameObject SelectedIcon;

        public Color ColorSelected;
        public Color ColorUnselected;

        public void setIcon(Sprite icon)
        {
            if (icon != null)
            {
                Icon.SetActive(true);
                Icon.GetComponent<Image>().sprite = icon;
            }
            else
            {
                Icon.SetActive(false);
            }
        }

        public void setSelected(bool selected)
        {
            gameObject.GetComponent<Image>().color = selected ? ColorSelected : ColorUnselected;
            SelectedIcon.SetActive(selected);
        }
    }
}