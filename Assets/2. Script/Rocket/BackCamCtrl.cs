using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackCamCtrl : MonoBehaviour
{
    public Transform thisTr = null;
    public Transform RocketTr = null;
    public RocketBehavior rocketInstance = null;
    public GameObject camTexture = null;

    private void Update()
    {
        if(rocketInstance.rocketRb.linearVelocity.magnitude>=0.3f)
        {
            camTexture.SetActive(true);
            thisTr.position = RocketTr.position - new Vector3(0.42f, 0.045f, 0.8f);
        }
    }
    /*
     *로켓2 구현후 사용예정
    public Transform thisTr = null;
    public Transform RocketTr = null;
    public RocketBehavior2 rocketInstance = null;
    public GameObject camTexture = null;

    private void Update()
    {
        if (rocketInstance.currState == RocketBehavior2.RocketState.EXHALE)
        {
            camTexture.SetActive(true);
            thisTr.position = RocketTr.position - Vector3.down;
        }
    }*/

}
