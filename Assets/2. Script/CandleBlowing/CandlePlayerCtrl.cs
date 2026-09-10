using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CandlePlayerCtrl : MonoBehaviour
{
    public Transform tr = null;
    public VRUIManager vrUIManager;
    
    private RaycastHit hit;
    private GameObject prevHit = null;
    private Ray ray;
    private LineRenderer line;

    private int effectTurn = 0;

    private bool isResetting = false;


    // Start is called before the first frame update
    void Start()
    {
        tr = GetComponent<Transform>();
        line = GetComponent<LineRenderer>();
        line.enabled = false;
        line.startWidth = 0.05f;
        line.endWidth = 0.05f;


        //StartCoroutine(ShowLaserBeam());
        //StartCoroutine(PrevHighlightOff());
    }

    public void SeekingCandle()
    {
        ray = new Ray(tr.position, tr.forward);


        line.SetPosition(0, tr.position);
        if (Physics.Raycast(tr.position, tr.forward, out hit, 100f))
        {
            line.SetPosition(1, hit.point);
            if (hit.collider.gameObject.CompareTag("INTERACTABLE"))
            {
                
                if (CandleGameManager2.instance.currState == CandleGameManager2.GameState.SEEKINGCANDLE && isResetting == false)
                {
                    //CandleGameManager2.instance.EyesOnCandle();
                    //hit.collider.gameObject.SendMessage("HighlightOn");
                    //prevHit = hit.collider.gameObject;
                }
                
            }
            else
            {
                //CandleGameManager2.instance.EyesOffCandle();
            }

        }
        else line.SetPosition(1, ray.GetPoint(100.0f));
    }

    //IEnumerator PrevHighlightOff()
    //{
    //    while (true)
    //    {
    //        if (prevHit != null && hit.collider.gameObject != prevHit)
    //        {
    //            yield return null;
    //            prevHit.GetComponent<ObjectCtrl>().LightOff();
    //        }
    //    }
    //}

    public void ExhaleCandle()
    {
        isResetting = true;
        hit = new RaycastHit();
        //CandleGameManager2.instance.EyesOffCandle();
        Invoke("RestStateAfterInhale", 2f);
    }

    private void RestStateAfterInhale()
    {
        isResetting = false;
    }

    IEnumerator ShowLaserBeam()
    {
        line.enabled = true;
        yield return new WaitForSeconds(Random.Range(0.01f, 0.2f));
        line.enabled = false;
    }
}
