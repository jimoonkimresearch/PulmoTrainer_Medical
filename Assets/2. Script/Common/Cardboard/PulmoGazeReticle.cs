using UnityEngine;
using UnityEngine.UI;

namespace PulmoTrainer.Cardboard
{
    // Visual only: no collider, raycaster, SendMessage, or selection events.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class PulmoGazeReticle : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float distance = 2f;
        [SerializeField, Range(0.05f, 3f)] private float angularDiameter = 0.6f;
        [SerializeField] private Color color = Color.white;

        private GameObject visual;
        private RectTransform visualTransform;
        private Texture2D ring;
        private RawImage graphic;
        private Camera viewCamera;

        private void Awake()
        {
            viewCamera = GetComponent<Camera>();
            // A Canvas without GraphicRaycaster cannot intercept UI input.
            visual = new GameObject("Pulmo Reticle (Runtime)", typeof(RectTransform), typeof(Canvas));
            visual.layer = gameObject.layer;
            visualTransform = visual.GetComponent<RectTransform>();
            visualTransform.SetParent(transform, false);
            visualTransform.sizeDelta = Vector2.one;
            Canvas canvas = visual.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = viewCamera;

            GameObject imageObject = new GameObject("Ring", typeof(RectTransform), typeof(RawImage));
            imageObject.layer = gameObject.layer;
            RectTransform imageTransform = imageObject.GetComponent<RectTransform>();
            imageTransform.SetParent(visualTransform, false);
            imageTransform.anchorMin = Vector2.zero;
            imageTransform.anchorMax = Vector2.one;
            imageTransform.offsetMin = imageTransform.offsetMax = Vector2.zero;
            graphic = imageObject.GetComponent<RawImage>();
            graphic.raycastTarget = false;
            ring = CreateRing();
            graphic.texture = ring;
            UpdateVisual();
        }

        private void OnEnable()
        {
            if (visual != null) visual.SetActive(true);
        }

        private void OnDisable()
        {
            if (visual != null) visual.SetActive(false);
        }

        private void LateUpdate() { UpdateVisual(); }

        private void UpdateVisual()
        {
            float desiredDepth = distance;
            // Match CamRayCast's nearest-hit query, without dispatching any events.
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 100f))
                desiredDepth = Mathf.Min(desiredDepth, hit.distance * 0.95f);
            float depth = Mathf.Clamp(desiredDepth, viewCamera.nearClipPlane + 0.05f,
                Mathf.Max(viewCamera.nearClipPlane + 0.05f, viewCamera.farClipPlane - 0.05f));
            float diameter = 2f * depth * Mathf.Tan(angularDiameter * Mathf.Deg2Rad * 0.5f);
            Vector3 scale = transform.lossyScale;
            // Use world distance/size even if the existing Player hierarchy is scaled.
            visualTransform.localPosition = new Vector3(0f, 0f, depth / SafeScale(scale.z));
            visualTransform.localRotation = Quaternion.identity;
            visualTransform.localScale = new Vector3(diameter / SafeScale(scale.x),
                diameter / SafeScale(scale.y), 1f / SafeScale(scale.z));
            graphic.color = color;
        }

        private static float SafeScale(float value)
        {
            return Mathf.Abs(value) < 0.00001f ? 0.00001f : value;
        }

        private static Texture2D CreateRing()
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "Pulmo Reticle Ring";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float radius = new Vector2(x + 0.5f - size * 0.5f,
                    y + 0.5f - size * 0.5f).magnitude / (size * 0.5f);
                float alpha = Mathf.Clamp01((radius - 0.62f) * 64f) *
                    Mathf.Clamp01((0.94f - radius) * 64f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private void OnDestroy()
        {
            if (visual != null) Destroy(visual);
            if (ring != null) Destroy(ring);
        }
    }
}
