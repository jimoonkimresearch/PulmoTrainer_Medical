using System.Collections.Generic;
using UnityEngine;

// The existing trigger stick is shared by Tutorial and BreathTest scenes.
public class TutorialSelectionStickCtrl : MonoBehaviour
{
    private readonly HashSet<Collider> contacts = new HashSet<Collider>();
    private TutorialGameManager tutorial;
    private GameObject selectedObject;
    private TutorialObjectCtrl tutorialTarget;
    private Outline highlighted;

    private void OnTriggerEnter(Collider other) { TryTrack(other); }
    private void OnTriggerStay(Collider other) { TryTrack(other); }

    private void TryTrack(Collider other)
    {
        if (other == null || !other.CompareTag("INTERACTABLE")) return;
        if (tutorial == null) tutorial = TutorialGameManager.instance;
        GameObject target;
        TutorialObjectCtrl interaction = null;
        if (tutorial != null)
        {
            interaction = other.GetComponentInParent<TutorialObjectCtrl>();
            if (!tutorial.TrySelectTarget(interaction)) return;
            target = interaction.gameObject;
        }
        else
        {
            // BreathTest only used this legacy component to highlight the balloon.
            BreathTestGameManager test = BreathTestGameManager.instance;
            if (test == null || test.currState3 == BreathTestGameManager.TestGameState.FINISH) return;
            target = other.gameObject;
        }
        if (selectedObject != null && selectedObject != target) return;
        contacts.Add(other);
        selectedObject = target;
        tutorialTarget = interaction;
        if (highlighted == null) highlighted = target.GetComponent<Outline>();
        if (highlighted != null) highlighted.enabled = true;
    }

    private void OnTriggerExit(Collider other)
    {
        contacts.Remove(other);
        if (contacts.Count == 0) ClearSelection();
    }

    private void LateUpdate()
    {
        // Disabled/destroyed colliders do not reliably send OnTriggerExit.
        contacts.RemoveWhere(collider => collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy);
        if (contacts.Count == 0 || (tutorial != null && tutorial.currState2 == TutorialGameManager.GameState2.FINISH))
            ClearSelection();
    }

    private void ClearSelection()
    {
        if (highlighted != null) highlighted.enabled = false;
        if (tutorial != null && tutorialTarget != null) tutorial.ReleaseTarget(tutorialTarget);
        contacts.Clear();
        selectedObject = null;
        tutorialTarget = null;
        highlighted = null;
    }

    private void OnDisable() { ClearSelection(); }
}
