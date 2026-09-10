using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectCtrl : MonoBehaviour
{
    public Outline outliner = null;
    public bool isTarget = false;
    public Transform playerTr = null;

    private Transform tr = null;
    public float spdPerDist = 0.003f;

    // Start is called before the first frame update
    void Start()
    {
        
        outliner = this.GetComponent<Outline>();
        
        tr = this.GetComponent<Transform>();
        playerTr = GameObject.Find("Player").GetComponent<Transform>();
        spdPerDist = 0.003f;
    }

    // Update is called once per frame
    void Update()
    {
        //if(!isTarget)
        //{
        //    outliner.enabled = false;
        //}
        //else if(isTarget)
        //{
        //    outliner.enabled = true;
        //}
    }

    //private void OnTriggerEnter(Collider other)
    //{
    //    outliner.enabled = true;
    //}

    //private void OnTriggerExit(Collider other)
    //{
    //    outliner.enabled = false;
    //}

    //public void HighlightOn()
    //{
    //    //isTarget = true;
    //    outliner.enabled = true;
    //    //StartCoroutine(HighlightOff());
    //}

    //IEnumerator HighlightOff()
    //{
    //    yield return new WaitForSeconds(0.5f);
    //    isTarget = false;
    //}

    //public void LightOff()
    //{
    //    outliner.enabled = false;
    //}

    public void Inhaled()
    {
        this.GetComponent<Rigidbody>().useGravity = false;
        this.GetComponent<Rigidbody>().isKinematic = true;
        this.GetComponent<BoxCollider>().enabled = false;
        StartCoroutine("ObjectInhale");
        StartCoroutine("ObjectRotate");
        StartCoroutine("ObjectShrink");
    }

    IEnumerator ObjectInhale()
    {
        //플레이어 입 위치
        Vector3 mouthPos = playerTr.position;
        Vector3 targetDist = mouthPos - tr.position;
        float inhaleSpeed = targetDist.magnitude * spdPerDist;
        while ((tr.position - mouthPos).magnitude > 0.1f)
        {
            tr.position += (targetDist * inhaleSpeed);
            yield return 0.01f;
        }
        this.gameObject.SetActive(false);

    }

    IEnumerator ObjectRotate()
    {
        float angle = 1.8f;
        Vector3 rotateAxis = playerTr.position - tr.position;
        while ((tr.position - playerTr.position).magnitude > 0.1f)
        {
            tr.Rotate(rotateAxis, angle);
            angle+=1.8f;
            yield return 0.2f;
        }
        this.gameObject.SetActive(false);

    }
    /// <summary>
    /// range = 현재 플레이어와 거리
    /// initialRange = 초기 플레이어와 거리
    /// scaleFactor = 초기 스케일에서 줄어든 후 스케일비율
    /// 날아올 때 작아지는 효과 담당
    /// </summary>
    /// <returns></returns>
    IEnumerator ObjectShrink()
    {
        float range = (playerTr.position - tr.position).magnitude;
        float initialRange = range;
        float initialScaleFactor = this.transform.localScale.x;
        float scaleFactor;
        while(range >0.2f)
        {
            range = (playerTr.position - tr.position).magnitude;
            scaleFactor = initialScaleFactor * (range / initialRange)  + 0.5f * (1 - (range / initialRange)) ;
            tr.localScale = Vector3.one * scaleFactor;
            yield return 0.2f;
        }

        this.gameObject.SetActive(false);
    }
}
