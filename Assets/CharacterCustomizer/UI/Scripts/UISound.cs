using UnityEngine;

namespace CC
{
    public class UISound : MonoBehaviour
    {
        public void playUISound(int index)
        {
            CC_UI_Manager.instance.playUIAudio(index);
        }
    }
}