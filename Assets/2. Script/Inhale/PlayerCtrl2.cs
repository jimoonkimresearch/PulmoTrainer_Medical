using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCtrl2 : MonoBehaviour
{
    public Transform tr = null;
    public VRUIManager vrUIManager;
    public InhaledFoodsControl inhaledFoodsControl = null;

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
        line.startWidth = 0.1f;
        line.endWidth = 0.05f;


        //StartCoroutine(ShowLaserBeam());
        //StartCoroutine(PrevHighlightOff());
    }

    public void SeekingFood()
    {
        ray = new Ray(tr.position, tr.forward);

        
        line.SetPosition(0, tr.position);
        if (Physics.Raycast(tr.position, tr.forward, out hit, 100f))
        {
            line.SetPosition(1, hit.point);
            if (hit.collider.gameObject.CompareTag("INTERACTABLE"))
            {
                if (InhaleGameManager.instance.currState == InhaleGameManager.GameState.SEEKINGFOOD && isResetting == false)
                {
                    //InhaleGameManager.instance.EyesOnFood();
                    //hit.collider.gameObject.SendMessage("HighlightOn");
                    //prevHit = hit.collider.gameObject;
                    //Debug.Log(hit.collider.gameObject.name);
                }
            }
            else
            {
                //InhaleGameManager.instance.EyesOffFood();
            }

        }
        else line.SetPosition(1, ray.GetPoint(100.0f));
    }

    //IEnumerator PrevHighlightOff()
    //{
    //    while(true)
    //    {
    //        if (prevHit != null && hit.collider.gameObject != prevHit)
    //        {
    //            yield return null;
    //            prevHit.SendMessage("HighlightOff");
    //        }
    //    }
    //}

    public void InhaleFood()
    {
        isResetting = true;
        //inhaledFoodsControl.SetInhaledFood(hit.collider.gameObject);
        InhaleGameManager.instance.currFoodeat++;
        InhaleGameManager.instance.inhaleEffectPool[effectTurn % 3].SetActive(true);
        effectTurn++;
        //hit.collider.gameObject.SendMessage("Inhaled");
        //hit = new RaycastHit();
        InhaleGameManager.instance.EyesOffFood();
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