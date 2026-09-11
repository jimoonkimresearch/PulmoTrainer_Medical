using UnityEngine;
using PulmoTrainer.Gaze;

public sealed class CandleSelectStickCtrl : PulmoGazeHandler
{
    [SerializeField] private CandleGameManager2 gameManager;
    private Outline highlighted;
    private bool ownsSelection;

    private CandleGameManager2 Manager
    {
        get
        {
            if (gameManager == null) gameManager = CandleGameManager2.instance;
            return gameManager;
        }
    }

    public override bool CanTrack
    {
        get
        {
            if (Manager == null) return false;
            var state = Manager.currState;
            return state == CandleGameManager2.GameState.SEEKINGCANDLE ||
                state == CandleGameManager2.GameState.INHALEREADY;
        }
    }

    public override GameObject ResolveTarget(Collider hitCollider)
    {
        if (hitCollider == null || Manager == null || Manager.candleControl == null) return null;
        // Use the manager's ordered references instead of depending on Candles1..5 object names.
        foreach (GameObject candle in Manager.candleControl)
        {
            if (candle != null && candle.activeInHierarchy &&
                (hitCollider.gameObject == candle || hitCollider.transform.IsChildOf(candle.transform)))
            {
                CandleControl2 interaction = candle.GetComponent<CandleControl2>();
                if (interaction != null && interaction.isActiveAndEnabled && !interaction.IsCompleted) return candle;
            }
        }
        return null;
    }

    public override void EnterTarget(GameObject target)
    {
        if (!CanTrack || target == null) return;
        CandleControl2 interaction = target.GetComponent<CandleControl2>();
        if (interaction == null || interaction.IsCompleted) return;
        int index = System.Array.IndexOf(Manager.candleControl, target);
        if (index < 0) return;
        Manager.currCandleSeeing = index;
        ownsSelection = true;
        highlighted = target.GetComponent<Outline>();
        if (highlighted != null) highlighted.enabled = true;
        Manager.EyesOnCandle();
    }

    public override void StayOnTarget(GameObject target)
    {
        // A round can return to SEEKING while the gaze is still on the same object.
        if (Manager != null && Manager.currState == CandleGameManager2.GameState.SEEKINGCANDLE)
            EnterTarget(target);
    }

    public override void ExitTarget(GameObject target)
    {
        if (highlighted != null) highlighted.enabled = false;
        highlighted = null;
        if (!ownsSelection) return;
        ownsSelection = false;
        // Cleanup must not change FINISH/GUIDE back into a running game.
        if (CanTrack && Manager.currState != CandleGameManager2.GameState.SEEKINGCANDLE)
            Manager.EyesOffCandle();
    }

    private void OnDisable() { ExitTarget(null); }
}
