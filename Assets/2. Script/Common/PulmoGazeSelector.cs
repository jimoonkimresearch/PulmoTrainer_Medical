using UnityEngine;
using System.Collections.Generic;

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
        private RaycastHit[] hitBuffer = new RaycastHit[32];
        private readonly HitComparer hitComparer = new HitComparer();

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

            if (PulmoTrainer.Training.TrainingEndSequence.IsInputBlocked ||
                !handler.isActiveAndEnabled || !handler.CanTrack)
            {
                HasHit = false;
                ClearSelection();
                return;
            }

            // The old games also move objects using Transform; make those changes visible to physics.
            Physics.SyncTransforms();
            Ray ray = new Ray(gazeCamera.transform.position, gazeCamera.transform.forward);
            int hitCount = CollectHits(ray);
            hitComparer.SetOrigin(ray.origin);
            System.Array.Sort(hitBuffer, 0, hitCount, hitComparer);
            HasHit = hitCount > 0;
            HitDistance = HasHit ? hitBuffer[0].distance : settings.MaxDistance;
            GameObject candidate = null;
            float candidateDistance = float.PositiveInfinity;
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = hitBuffer[index];
                if (hit.collider == null || !hit.collider.enabled) continue;
                if (candidate != null && hit.distance > candidateDistance + 0.0001f) break;
                GameObject resolved = handler.ResolveTarget(hit.collider);
                if (resolved == null || !resolved.activeInHierarchy)
                {
                    // Preserve occlusion, including a blocker tied with a target at the nearest distance.
                    candidate = null;
                    break;
                }
                if (candidate == null)
                {
                    candidate = resolved;
                    candidateDistance = hit.distance;
                }
            }
            ChangeSelection(candidate);
        }

        private int CollectHits(Ray ray)
        {
            while (true)
            {
                int count = settings.CastRadius > 0f
                    ? Physics.SphereCastNonAlloc(ray, settings.CastRadius, hitBuffer, settings.MaxDistance,
                        settings.QueryLayers, QueryTriggerInteraction.Collide)
                    : Physics.RaycastNonAlloc(ray, hitBuffer, settings.MaxDistance,
                        settings.QueryLayers, QueryTriggerInteraction.Collide);
                if (count < hitBuffer.Length) return count;
                // NonAlloc results are unordered; a full buffer may omit the nearest wall/food.
                if (hitBuffer.Length < 512) System.Array.Resize(ref hitBuffer, hitBuffer.Length * 2);
                else
                {
                    RaycastHit[] all = settings.CastRadius > 0f
                        ? Physics.SphereCastAll(ray, settings.CastRadius, settings.MaxDistance,
                            settings.QueryLayers, QueryTriggerInteraction.Collide)
                        : Physics.RaycastAll(ray, settings.MaxDistance,
                            settings.QueryLayers, QueryTriggerInteraction.Collide);
                    hitBuffer = new RaycastHit[all.Length + 1];
                    System.Array.Copy(all, hitBuffer, all.Length);
                    return all.Length;
                }
            }
        }

        private sealed class HitComparer : IComparer<RaycastHit>
        {
            private Vector3 origin;
            public void SetOrigin(Vector3 value) { origin = value; }
            public int Compare(RaycastHit first, RaycastHit second)
            {
                int distance = first.distance.CompareTo(second.distance);
                if (distance != 0) return distance;
                if (first.collider == null) return second.collider == null ? 0 : 1;
                if (second.collider == null) return -1;
                distance = (first.collider.ClosestPoint(origin) - origin).sqrMagnitude.CompareTo(
                    (second.collider.ClosestPoint(origin) - origin).sqrMagnitude);
                return distance != 0 ? distance : first.collider.GetInstanceID().CompareTo(second.collider.GetInstanceID());
            }
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
            if (PulmoTrainer.Training.TrainingEndSequence.IsInputBlocked ||
                !isActiveAndEnabled || candidate == null || !candidate.activeInHierarchy ||
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
