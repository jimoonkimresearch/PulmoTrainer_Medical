using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialSelectionStickCtrl : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log("충돌");
        if (other.gameObject.CompareTag("INTERACTABLE") && BreathTestGameManager.instance.currState3 != BreathTestGameManager.TestGameState.FINISH)
        {
            //Debug.Log("상호작용");
            other.gameObject.GetComponent<Outline>().enabled = true;
            //BreathTestGameManager.instance.EyesOnBall();
            //BreathTestGameManager.instance.currObjSeeing = other.gameObject;

        }
    }

    private void OnTriggerExit(Collider other)
    {

        if (other.gameObject.CompareTag("INTERACTABLE") && BreathTestGameManager.instance.currState3 != BreathTestGameManager.TestGameState.FINISH)
        {
            other.gameObject.GetComponent<Outline>().enabled = false;
            //BreathTestGameManager.instance.EyesOffBall();
            //BreathTestGameManager.instance.currObjSeeing = null;
        }
    }
}
