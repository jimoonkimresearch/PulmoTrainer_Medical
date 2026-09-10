using UnityEngine;

namespace PulmoTrainer.Gaze
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class PulmoGazeSelector : MonoBehaviour
    {
        [SerializeField] private Camera gazeCamera;
        [SerializeField] private PulmoGazeSettings settings;
        [SerializeField] private PulmoGazeHandler handler;

        private GameObject currentTarget;
        private PulmoGazeHandler selectionOwner;
        private bool hasSelection;
        private bool changingSelection;
        private bool warned;
        private Collider[] legacyColliders;

        public GameObject CurrentTarget => currentTarget;
        public bool HasHit { get; private set; }
        public float HitDistance { get; private set; }

        private void Awake()
        {
            if (handler == null) handler = GetComponent<PulmoGazeHandler>();
            legacyColliders = GetComponents<Collider>();
        }

        private void LateUpdate()
        {
            if (!ValidateConfiguration())
            {
                ClearSelection();
                return;
            }

            if (!handler.isActiveAndEnabled || !handler.CanTrack)
            {
                HasHit = false;
                ClearSelection();
                return;
            }

            // The old games also move objects using Transform; make those changes visible to physics.
            Physics.SyncTransforms();
            Ray ray = new Ray(gazeCamera.transform.position, gazeCamera.transform.forward);
            RaycastHit hit;
            HasHit = settings.CastRadius > 0f
                ? Physics.SphereCast(ray, settings.CastRadius, out hit, settings.MaxDistance,
                    settings.QueryLayers, QueryTriggerInteraction.Collide)
                : Physics.Raycast(ray, out hit, settings.MaxDistance,
                    settings.QueryLayers, QueryTriggerInteraction.Collide);
            HitDistance = HasHit ? hit.distance : settings.MaxDistance;

            // A non-target first hit blocks selection. Never search through a wall or another object.
            GameObject candidate = HasHit ? handler.ResolveTarget(hit.collider) : null;
            if (candidate != null && !candidate.activeInHierarchy) candidate = null;
            ChangeSelection(candidate);
        }

        private bool ValidateConfiguration()
        {
            if (legacyColliders != null)
            {
                foreach (Collider legacyCollider in legacyColliders)
                {
                    if (legacyCollider != null && legacyCollider.enabled)
                    {
                        if (!warned)
                            Debug.LogError("PulmoGazeSelector: disable the old selection-stick Collider. " +
                                "The cylinder is visual only; target Colliders stay enabled.", this);
                        warned = true;
                        return false;
                    }
                }
            }
            if (gazeCamera != null && settings != null && handler != null &&
                handler.gameObject == gameObject)
            {
                warned = false;
                return true;
            }
            if (!warned)
            {
                Debug.LogError("PulmoGazeSelector: assign the scene Main Camera, shared Gaze Settings, " +
                    "and a game handler on this same selection-stick object.", this);
                warned = true;
            }
            return false;
        }

        private void ChangeSelection(GameObject candidate)
        {
            if (changingSelection) return;
            if (hasSelection && currentTarget != null && candidate == currentTarget && selectionOwner == handler)
            {
                selectionOwner.StayOnTarget(currentTarget);
                return;
            }

            ClearSelection();
            // Exit callbacks may change game state or disable this object.
            if (!isActiveAndEnabled || candidate == null || !candidate.activeInHierarchy ||
                handler == null || !handler.isActiveAndEnabled || !handler.CanTrack) return;

            currentTarget = candidate;
            selectionOwner = handler;
            hasSelection = true;
            selectionOwner.EnterTarget(candidate);
        }

        private void ClearSelection()
        {
            if (!hasSelection || changingSelection) return;
            GameObject previous = currentTarget;
            PulmoGazeHandler previousOwner = selectionOwner;
            currentTarget = null;
            selectionOwner = null;
            hasSelection = false;
            changingSelection = true;
            try
            {
                // Even a destroyed Unity object needs one exit notification to clear game state.
                if (previousOwner != null) previousOwner.ExitTarget(previous);
            }
            finally { changingSelection = false; }
        }

        private void OnDisable()
        {
            HasHit = false;
            ClearSelection();
        }

        private void OnDrawGizmosSelected()
        {
            if (gazeCamera == null || settings == null) return;
            Gizmos.color = Color.cyan;
            Vector3 start = gazeCamera.transform.position;
            Gizmos.DrawLine(start, start + gazeCamera.transform.forward * settings.MaxDistance);
        }
    }
}
