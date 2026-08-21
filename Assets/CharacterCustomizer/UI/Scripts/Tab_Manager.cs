using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CC
{
    public class Tab_Manager : MonoBehaviour
    {
        [Header("Button Active Colors")]
        public ColorBlock TabColorActive;

        [Header("Button Inactive Colors")]
        public ColorBlock TabColorInactive;

        public GameObject TabParent;
        private List<GameObject> tabs = new List<GameObject>();
        private List<GameObject> tabMenus = new List<GameObject>();

        private void Start()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                var tab = transform.GetChild(i).gameObject;
                var index = i;
                tabs.Add(tab);

                tab.GetComponentInChildren<Button>().onClick.AddListener(() => switchTab(index));
            }

            foreach (Transform child in TabParent.transform)
            {
                tabMenus.Add(child.gameObject);
            }

            switchTab(0);
        }

        public void switchTab(int tab)
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                //Set tab color
                tabs[i].GetComponentInChildren<Button>().colors = tab == i ? TabColorActive : TabColorInactive;

                //Set tab active state
                if (tabMenus.Count > i) tabMenus[i].SetActive(tab == i);
            }
        }
    }
}