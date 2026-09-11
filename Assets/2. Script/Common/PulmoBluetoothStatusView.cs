using UnityEngine;
using UnityEngine.UI;

namespace PulmoTrainer.Bluetooth
{
    // Presentation only: no scanning, permission requests, or game-state changes.
    [DisallowMultipleComponent]
    public sealed class PulmoBluetoothStatusView : MonoBehaviour
    {
        [SerializeField] private Vector3 cameraOffset = new Vector3(-0.16f, 0.18f, 0.8f);
        private Text label;
        private Camera targetCamera;
        private RectTransform canvasTransform;

        public void Initialize(Text text, Camera camera)
        {
            label = text;
            targetCamera = camera;
            if (label == null) return;
            label.raycastTarget = false;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 18;
            label.resizeTextMaxSize = 28;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.alignment = TextAnchor.MiddleLeft;
            if (targetCamera != null) AttachToCamera();
        }

        private void AttachToCamera()
        {
            if (canvasTransform == null)
            {
                var canvasObject = new GameObject("BluetoothStatusCanvas", typeof(RectTransform), typeof(Canvas));
                canvasObject.layer = gameObject.layer;
                canvasTransform = canvasObject.GetComponent<RectTransform>();
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.sortingOrder = 100;
                canvas.worldCamera = targetCamera;
                canvasTransform.SetParent(targetCamera.transform, false);
                canvasTransform.localRotation = Quaternion.identity;
                canvasTransform.localScale = Vector3.one * 0.001f;
                canvasTransform.sizeDelta = new Vector2(400f, 70f);

                // The original object/name/font are kept, but game HUD visibility no longer controls it.
                RectTransform textTransform = label.rectTransform;
                textTransform.SetParent(canvasTransform, false);
                textTransform.localRotation = Quaternion.identity;
                textTransform.localScale = Vector3.one;
                textTransform.anchorMin = Vector2.zero;
                textTransform.anchorMax = Vector2.one;
                textTransform.pivot = new Vector2(0.5f, 0.5f);
                textTransform.offsetMin = Vector2.zero;
                textTransform.offsetMax = Vector2.zero;
                textTransform.anchoredPosition3D = Vector3.zero;
            }
            UpdatePosition();
        }

        private void LateUpdate()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null || label == null) return;
            if (canvasTransform == null) AttachToCamera();
            else UpdatePosition();
        }

        private void UpdatePosition()
        {
            Vector3 offset = cameraOffset;
            offset.z = Mathf.Max(offset.z, targetCamera.nearClipPlane + 0.25f);
            canvasTransform.localPosition = offset;
            // Parenting also follows XR's final before-render pose, without a second pose writer.
        }

        public void Show(string status)
        {
            if (label != null && label.text != status) label.text = status;
        }
    }
}
