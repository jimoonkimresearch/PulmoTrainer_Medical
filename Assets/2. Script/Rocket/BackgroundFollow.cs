using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundFollow : MonoBehaviour
{
    public Transform rocketTr;
    public List<Transform> backgrounds = new List<Transform>();

    int turn = 0;
    float currY = 0f;
    Vector3 currPos;
    Vector3 newPos;

    // Update is called once per frame
    void Update()
    {
        if(rocketTr.position.y>=currY + 700f)
        {
            currPos = backgrounds[turn].position;
            newPos = new Vector3(currPos.x, currPos.y + 1400f, currPos.z); 

            backgrounds[turn].position = newPos;
            turn = (turn + 1) % 2;
            currY += 1400f;
        }
    }
}
