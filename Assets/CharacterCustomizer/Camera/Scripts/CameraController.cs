using UnityEngine;
using UnityEngine.EventSystems;

namespace CC
{
    public class CameraController : MonoBehaviour
    {
        public static CameraController instance;
        public float ZoomMin = -0.6f;
        public float ZoomMax = -3.1f;
        public float ZoomPanScale = 0.5f;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private Camera _camera;
        private Transform cameraRoot;

        private float zoomTarget = 1;

        private Vector3 mouseOldPos;
        private Vector3 mouseDelta;

        private Vector3 cameraRotationTarget = new Vector3(10, -5, 0);
        private Vector3 cameraRotationDefault;
        private float rotateSpeed = 5;
        private bool dragging = false;

        private Vector3 cameraOffset;
        private Vector3 cameraOffsetDefault;
        private float panSpeed = 3;
        private bool panning = false;

        public Texture2D cursorTexture;
        private Vector2 hotSpot;

        private void Start()
        {
            _camera = Camera.main;
            cameraRoot = gameObject.transform;

            if (_camera == null)
            {
                Debug.LogError("CameraController requires a camera tagged MainCamera.", this);
                enabled = false;
                return;
            }

            if (cursorTexture != null)
            {
                hotSpot = new Vector2(cursorTexture.width / 2, cursorTexture.height / 2);
                setDefaultCursor();
            }

            cameraOffsetDefault = _camera.transform.localPosition;
            cameraOffset = cameraOffsetDefault;

            cameraRotationDefault = cameraRoot.localRotation.eulerAngles;
            cameraRotationTarget = cameraRotationDefault;
        }

        public void setCursor(Texture2D texture)
        {
            Cursor.SetCursor(texture, hotSpot, CursorMode.Auto);
        }

        public void setDefaultCursor()
        {
            Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
        }

        private void Update()
        {
            //Set dragging/panning when we're not hovering over anything
            if ((EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                && _camera.pixelWidth > 0
                && _camera.pixelHeight > 0
                && IsFinite(Input.mousePosition))
            {
                //Pan when zooming
                Vector3 mousePos = Camera.main.ScreenToViewportPoint(Input.mousePosition) - new Vector3(0.5f, 0.5f, 0.0f);
                if (IsFinite(mousePos))
                {
                    cameraOffset += mousePos * Mathf.Abs(Input.mouseScrollDelta.y) * ZoomPanScale * Mathf.Lerp(0.2f, 1f, zoomTarget);
                }

                //Set zoom target
                zoomTarget = Mathf.Clamp((zoomTarget * (1 - Input.mouseScrollDelta.y / 10)), 0, 1);

                if (Input.GetMouseButtonDown(1))
                {
                    mouseOldPos = Input.mousePosition;
                    dragging = true;
                }

                if (Input.GetMouseButtonDown(2))
                {
                    mouseOldPos = Input.mousePosition;
                    panning = true;
                }
            }

            //Rotation
            if (Input.GetMouseButton(1) && dragging)
            {
                mouseDelta = mouseOldPos - Input.mousePosition;

                cameraRotationTarget.x += mouseDelta.y / 5;
                cameraRotationTarget.y -= mouseDelta.x / 5;

                mouseOldPos = Input.mousePosition;
            }

            if (Input.GetMouseButtonUp(1))

                dragging = false;

            //Panning
            if (Input.GetMouseButton(2) && panning)
            {
                mouseDelta = mouseOldPos - Input.mousePosition;
                cameraOffset -= mouseDelta / 500;
                mouseOldPos = Input.mousePosition;
            }

            if (Input.GetMouseButtonUp(2))

                panning = false;

            //Reset camera with F
            if (Input.GetKeyDown(KeyCode.F))
            {
                cameraRotationTarget = cameraRotationDefault;
                cameraOffset = cameraOffsetDefault;
                zoomTarget = 1;
            }

            //Camera Z offset i.e zoom
            cameraOffset.z = Mathf.Lerp(ZoomMin, ZoomMax, zoomTarget);

            if (!IsFinite(cameraOffset))
            {
                cameraOffset = cameraOffsetDefault;
            }

            //Camera offset interp
            var currentPosition = IsFinite(_camera.transform.localPosition)
                ? _camera.transform.localPosition
                : cameraOffsetDefault;
            _camera.transform.localPosition = Vector3.Lerp(currentPosition, cameraOffset, Time.deltaTime * panSpeed);

            //Rotation interp
            cameraRoot.transform.localRotation = Quaternion.Slerp(cameraRoot.transform.localRotation, Quaternion.Euler(cameraRotationTarget), Time.deltaTime * rotateSpeed);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
