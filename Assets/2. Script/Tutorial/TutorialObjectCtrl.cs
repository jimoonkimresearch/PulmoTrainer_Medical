using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialObjectCtrl : MonoBehaviour
{
    private float pressurePlus = 0f;
    private float pressureMinus = 0f;
    private float accelerationRatio = 1f;
    private Rigidbody thisRb;
    private Transform thisTr;
    private Transform playerTr;


    // Start is called before the first frame update
    void Start()
    {
        thisRb = this.GetComponent<Rigidbody>();
        thisTr = this.GetComponent<Transform>();
        playerTr = GameObject.Find("Player").GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnExhale(float pressure)
    {
        Vector3 forceDir = thisTr.position - playerTr.position + (Vector3.up * Random.Range(-1f, 1f));
        thisRb.AddForce(forceDir * accelerationRatio, ForceMode.Acceleration);
    }

    public void OnInhale(float pressure)
    {
        Vector3 forceDir = playerTr.position - thisTr.position + (Vector3.up * Random.Range(-1f, 1f));
        thisRb.AddForce(forceDir * accelerationRatio, ForceMode.Acceleration);
    }
}
