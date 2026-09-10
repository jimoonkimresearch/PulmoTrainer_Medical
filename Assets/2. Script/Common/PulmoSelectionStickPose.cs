using UnityEngine;

namespace PulmoTrainer.Gaze
{
    // Visual geometry only. No hit testing or game-state changes.
    [DefaultExecutionOrder(-110)]
    [DisallowMultipleComponent]
    public sealed class PulmoSelectionStickPose : MonoBehaviour
    {
        [SerializeField] private Camera gazeCamera;
        [SerializeField] private PulmoGazeSettings settings;
        private bool warned;

        private void LateUpdate()
        {
            if (gazeCamera == null || settings == null) return;
            Transform parent = transform.parent;
            Vector3 parentScale = parent != null ? parent.lossyScale : Vector3.one;
            if (parentScale.x <= 0f ||
                !Mathf.Approximately(parentScale.x, parentScale.y) ||
                !Mathf.Approximately(parentScale.x, parentScale.z))
            {
                if (!warned)
                {
                    Debug.LogError("PulmoSelectionStickPose: use a parent with positive uniform scale " +
                        "(normally Main Camera, scale 1/1/1).", this);
                    warned = true;
                }
                return;
            }

            float halfLength = settings.MaxDistance * 0.5f;
            Transform cameraTransform = gazeCamera.transform;
            transform.SetPositionAndRotation(
                cameraTransform.position + cameraTransform.forward * halfLength,
                cameraTransform.rotation * Quaternion.Euler(90f, 0f, 0f));
            transform.localScale = new Vector3(settings.BeamDiameter, halfLength,
                settings.BeamDiameter) / parentScale.x;
        }
    }
}
