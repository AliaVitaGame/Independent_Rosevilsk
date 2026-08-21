using UnityEngine;
using UnityEngine.EventSystems;

namespace CC
{
    public class SetCursor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Texture2D cursorTexture;

        public void OnPointerEnter(PointerEventData eventData)
        {
            CameraController.instance.setCursor(cursorTexture);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            CameraController.instance.setDefaultCursor();
        }
    }
}