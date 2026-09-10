using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnLaunch : MonoBehaviour
{
    public float movetime = 0.7f;
    private bool isLaunching = false;

    public Transform leftCeiling = null;
    public Transform rightCeiling = null;
    public GameObject RocketCam = null;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void CeilingOpening()
    {
        StartCoroutine("openUp");
    }

    IEnumerator openUp()
    {
        
        float angle = 0f;
        while (angle < 90)
        {
            Vector3 leftAxis = new Vector3(-4.29f, 3.79f, 0);
            leftCeiling.RotateAround(leftAxis, Vector3.forward, 3f);
            Vector3 rightAxis = new Vector3(3.45f, 3.79f, 0);
            rightCeiling.RotateAround(rightAxis, Vector3.forward, -3f);
            angle += 3f;
            yield return 0.01f;
        }
        RocketCam.SetActive(true);
    }
}