using UnityEngine;

namespace PulmoTrainer.Gaze
{
    [CreateAssetMenu(menuName = "PulmoTrainer/Gaze Settings", fileName = "PulmoGazeSettings")]
    public sealed class PulmoGazeSettings : ScriptableObject
    {
        [SerializeField, Min(0.01f)] private float maxDistance = 100f;
        [Tooltip("0 = exact centre ray. A positive value enables a sphere cast in world units.")]
        [SerializeField, Min(0f)] private float castRadius = 0f;
        [Tooltip("Include targets AND objects that should block the gaze, such as walls and menus.")]
        [SerializeField] private LayerMask queryLayers = Physics.DefaultRaycastLayers;
        [SerializeField, Min(0.0001f)] private float beamDiameter = 0.008f;

        public float MaxDistance => Mathf.Max(0.01f, maxDistance);
        public float CastRadius => Mathf.Max(0f, castRadius);
        public int QueryLayers => queryLayers.value;
        public float BeamDiameter => Mathf.Max(0.0001f, beamDiameter);
    }
}
