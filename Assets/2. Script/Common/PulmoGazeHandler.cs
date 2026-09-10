using UnityEngine;

namespace PulmoTrainer.Gaze
{
    // Game-specific adapters implement this contract; Common never references a game manager.
    public abstract class PulmoGazeHandler : MonoBehaviour
    {
        public abstract bool CanTrack { get; }
        public abstract GameObject ResolveTarget(Collider hitCollider);
        public abstract void EnterTarget(GameObject target);
        public abstract void ExitTarget(GameObject target);
        public virtual void StayOnTarget(GameObject target) { }

        protected static GameObject FindTaggedAncestor(Collider collider, string targetTag)
        {
            if (collider == null) return null;
            for (Transform node = collider.transform; node != null; node = node.parent)
                if (node.CompareTag(targetTag)) return node.gameObject;
            return null;
        }
    }
}
