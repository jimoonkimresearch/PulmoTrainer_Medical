using UnityEngine;

namespace PulmoTrainer.UI
{
    /// <summary>Attaches a World Space Canvas to the shared camera pose without per-frame chasing.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public sealed class PulmoHeadLockedCanvas : MonoBehaviour
    {
        public bool Bind(Camera viewCamera, Vector3 cameraOffset)
        {
            Canvas canvas = GetComponent<Canvas>();
            if (viewCamera == null || canvas.renderMode != RenderMode.WorldSpace ||
                canvas.rootCanvas != canvas)
            {
                Debug.LogError("PulmoHeadLockedCanvas: bind a root World Space Canvas to the scene camera.", this);
                return false;
            }
            if (viewCamera.transform.IsChildOf(transform))
            {
                Debug.LogError("PulmoHeadLockedCanvas: camera cannot be a child of this Canvas.", this);
                return false;
            }

            RectTransform canvasRect = (RectTransform)transform;
            Vector2 layoutSize = canvasRect.rect.size;
            // Keep the authored world scale and layout while changing the parent.
            canvasRect.SetParent(viewCamera.transform, true);
            canvasRect.anchorMin = new Vector2(0.5f, 0.5f);
            canvasRect.anchorMax = new Vector2(0.5f, 0.5f);
            canvasRect.sizeDelta = layoutSize;
            canvasRect.localPosition = cameraOffset;
            canvasRect.localRotation = Quaternion.identity;
            canvas.worldCamera = viewCamera;
            return true;
        }
    }
}
