using UnityEngine;
using UnityEngine.EventSystems;

namespace CC
{
    public class DragHandle : MonoBehaviour, IDragHandler, IPointerDownHandler
    {
        public GameObject WindowToDrag;
        private RectTransform rectTransform;
        private Canvas canvas;

        public void OnDrag(PointerEventData eventData)
        {
            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            rectTransform.SetAsLastSibling();
        }

        private void Start()
        {
            rectTransform = WindowToDrag.GetComponent<RectTransform>();
            Canvas[] c = GetComponentsInParent<Canvas>();
            canvas = c[c.Length - 1];
        }
    }
}