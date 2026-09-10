using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonCtrl : MonoBehaviour
{
    public GameObject player;
    public Outline thisOutLine;


    public void OnRayHit()
    {
        //Debug.Log(this.gameObject.name);
        thisOutLine.enabled = true;
        player.SendMessage("StartSelection");
    }

    public void OutLineOff()
    {
        thisOutLine.enabled = false;
    }
}
