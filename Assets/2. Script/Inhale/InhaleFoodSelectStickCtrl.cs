using UnityEngine;
using PulmoTrainer.Gaze;

public sealed class InhaleFoodSelectStickCtrl : PulmoGazeHandler
{
    [SerializeField] private InhaleGameManager gameManager;
    private GameObject selectedFood;
    private Outline highlighted;
    private bool ownsSelection;

    private InhaleGameManager Manager
    {
        get
        {
            if (gameManager == null) gameManager = InhaleGameManager.instance;
            return gameManager;
        }
    }

    public override bool CanTrack
    {
        get
        {
            if (Manager == null) return false;
            var state = Manager.currState;
            return state == InhaleGameManager.GameState.SEEKINGFOOD ||
                state == InhaleGameManager.GameState.INHALEREADY ||
                state == InhaleGameManager.GameState.INHALE;
        }
    }

    public override GameObject ResolveTarget(Collider hitCollider)
    {
        if (hitCollider == null || !hitCollider.enabled) return null;
        // The manager calls Inhaled on ObjectCtrl's GameObject. Resolve child colliders to it.
        ObjectCtrl food = hitCollider.GetComponentInParent<ObjectCtrl>();
        return food != null && food.isActiveAndEnabled && food.gameObject.activeInHierarchy &&
            food.CompareTag("INTERACTABLE") ? food.gameObject : null;
    }

    public override void EnterTarget(GameObject target)
    {
        if (!CanTrack || target == null) return;
        selectedFood = target;
        ownsSelection = true;
        Manager.currFoodSeeing = target;
        highlighted = target.GetComponent<Outline>();
        if (highlighted != null) highlighted.enabled = true;
        Manager.EyesOnFood();
    }

    public override void StayOnTarget(GameObject target)
    {
        if (Manager != null && Manager.currState == InhaleGameManager.GameState.SEEKINGFOOD)
            EnterTarget(target);
    }

    public override void ExitTarget(GameObject target)
    {
        if (highlighted != null) highlighted.enabled = false;
        highlighted = null;
        if (!ownsSelection) return;
        ownsSelection = false;
        if (Manager != null && Manager.currFoodSeeing == selectedFood)
        {
            Manager.currFoodSeeing = null;
            if (CanTrack && Manager.currState != InhaleGameManager.GameState.SEEKINGFOOD)
                Manager.EyesOffFood();
        }
        selectedFood = null;
    }

    private void OnDisable() { ExitTarget(null); }
}
