using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace CC
{
    [System.Serializable]
    public class OnPickerDrag : UnityEvent<Vector2>
    {
    }

    public class Grid_Picker : MonoBehaviour, IDragHandler, IBeginDragHandler
    {
        private RectTransform rectTransform;
        private RectTransform rectTransformPicker;
        private Vector2 rectSize;
        public Vector2 imageSize;

        public Camera _camera;
        public bool UseCamera = false;

        public GameObject Picker;
        public GameObject Background;

        public OnPickerDrag m_onPickerDrag = new OnPickerDrag();

        public void Start()
        {
            rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransformPicker = Picker.GetComponent<RectTransform>();
            rectSize = rectTransform.sizeDelta;
            if (_camera == null && UseCamera) _camera = Camera.main;
        }

        public void UpdatePosition(PointerEventData eventData)
        {
            Vector2 coords;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, _camera, out coords);

            coords.x = Mathf.Clamp(coords.x, 0, rectSize.x);
            coords.y = Mathf.Clamp(coords.y, 0, rectSize.y);

            rectTransformPicker.anchoredPosition = new Vector2(Mathf.Clamp(coords.x, 10, rectSize.x - 10), Mathf.Clamp(coords.y, 10, rectSize.y - 10));

            coords /= rectSize;

            m_onPickerDrag.Invoke(coords);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdatePosition(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            rectSize = rectTransform.sizeDelta;
            UpdatePosition(eventData);
        }

        public void randomize()
        {
            float x = Random.Range(0f, 1f);
            float y = Random.Range(0f, 1f);
            m_onPickerDrag.Invoke(new Vector2(x, y));
            rectTransformPicker.anchoredPosition = new Vector2(Mathf.Clamp(x * rectSize.x, 10, rectSize.x - 10), Mathf.Clamp(y * rectSize.y, 10, rectSize.y - 10));
        }
    }
}