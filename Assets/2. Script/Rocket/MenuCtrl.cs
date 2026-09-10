using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuCtrl : MonoBehaviour
{
    public Transform playerTr;
    public Transform thisTr;

    public void FollowPlayer()
    {
        thisTr.position = playerTr.position - new Vector3(0f, 0.5f, 2.5f);
    }
}
