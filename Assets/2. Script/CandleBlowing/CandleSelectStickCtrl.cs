using UnityEngine;
using PulmoTrainer.Gaze;

public sealed class CandleSelectStickCtrl : PulmoGazeHandler
{
    [SerializeField] private CandleGameManager2 gameManager;
    private CandleControl2 selectedInteraction;

    private CandleGameManager2 Manager
    {
        get
        {
            if (gameManager == null) gameManager = CandleGameManager2.instance;
            return gameManager;
        }
    }

    public override bool CanTrack => Manager != null && Manager.CanTrackCandleGaze;

    public override GameObject ResolveTarget(Collider hitCollider)
    {
        if (hitCollider == null || Manager == null) return null;
        CandleControl2 interaction = hitCollider.GetComponentInParent<CandleControl2>();
        // During a breath only the active set can resolve; looking elsewhere produces an exit.
        return Manager.CanSelectCandle(interaction) ? interaction.gameObject : null;
    }

    public override void EnterTarget(GameObject target)
    {
        if (!CanTrack || target == null) return;
        CandleControl2 interaction = target.GetComponent<CandleControl2>();
        if (!Manager.TrySelectCandle(interaction)) return;
        selectedInteraction = interaction;
        interaction.SetHighlighted(true);
    }

    public override void StayOnTarget(GameObject target)
    {
        // A round can return to SEEKING while the gaze is still on the same object.
        if (Manager != null && Manager.currState == CandleGameManager2.GameState.SEEKINGCANDLE)
            EnterTarget(target);
    }

    public override void ExitTarget(GameObject target)
    {
        CandleControl2 previous = selectedInteraction;
        selectedInteraction = null;
        if (previous == null) return;
        previous.SetHighlighted(false);
        if (Manager != null) Manager.NotifyCandleGazeLost(previous);
    }

    private void OnDisable() { ExitTarget(null); }
}
