using UnityEngine;
using PulmoTrainer.Gaze;

public sealed class RocketSelectionStickCtrl : PulmoGazeHandler
{
    [SerializeField] private RocketGameManager gameManager;
    private Outline highlighted;

    private RocketGameManager Manager
    {
        get
        {
            if (gameManager == null) gameManager = RocketGameManager.instance;
            return gameManager;
        }
    }

    public override bool CanTrack => Manager != null &&
        Manager.currState != RocketGameManager.RocketState.FINISH;

    public override GameObject ResolveTarget(Collider hitCollider) =>
        FindTaggedAncestor(hitCollider, "INTERACTABLE");

    public override void EnterTarget(GameObject target)
    {
        if (target == null) return;
        highlighted = target.GetComponent<Outline>();
        if (highlighted != null) highlighted.enabled = true;
    }

    public override void ExitTarget(GameObject target)
    {
        if (highlighted != null) highlighted.enabled = false;
        highlighted = null;
    }

    private void OnDisable() { ExitTarget(null); }
}
