using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FireReaction : MonoBehaviour
{
    private Transform fireTr;

    private Vector3 rotatePoint = Vector3.zero;
    private Vector3 rotateAxis = Vector3.right;
    private float maxRotateAngle = 60f;
    private float originZ = 0f;
    private bool once = false;

    private float trembleRange = 5f;


    public bool isBlowing = false;


    public GameObject offEffect = null;


    // Start is called before the first frame update
    void Start()
    {
        fireTr = this.gameObject.GetComponent<Transform>();
        rotatePoint = fireTr.position;
    }


    public void BlowStart(int i)
    {
        isBlowing = true;
        if(!once)
            StartCoroutine("fireRotateOnBlowed", i);
        once = true;
    }

    public void FireSmaller(float fev1)
    {

        StartCoroutine("smallerAction", fev1);
    }


    IEnumerator fireRotateOnBlowed(int i)
    {
        float rotated = 0f;
        
        while (isBlowing)
        {
            if (rotated >= (maxRotateAngle - (i * 3)))
                break;

            fireTr.RotateAround(rotatePoint, rotateAxis, 1f);
            rotated++;
            yield return null;
            
        }

        while (isBlowing)
        {
            Vector3 eulerRot = new Vector3(maxRotateAngle - (i * 3), fireTr.rotation.y, Random.Range(-1f * trembleRange, trembleRange));
            fireTr.rotation = Quaternion.Euler(eulerRot);
            yield return 1 / 60f;
        }
    }

    /// <summary>
    /// 촛불의 불을 작게 해주는 함수.
    /// </summary>
    /// <param name="border"></param>
    /// <returns></returns>
    IEnumerator smallerAction(float fev1)
    {
        float targetScale = 0.05f - (0.04f * ((fev1 % 100f) / 100f));
        fireTr.localScale = Vector3.one * targetScale;
        yield return null;
    }

    
    /// <summary>
    /// 호기 종료 직후 반응.  촛불이 바람에 밀려 기울어졌던 것이 돌아오기.
    /// </summary>
    /// <param name="i"></param>
    void blowFinished(int i)
    {
        isBlowing = false;
        StartCoroutine("blowEnd", i);
    }

    IEnumerator blowEnd(int i)
    {
        float rotated = 70f - (i * 3f);
        while (!isBlowing)
        {
            if (fireTr.rotation.x <= 0.001f)
                break;
            
            fireTr.RotateAround(rotatePoint, rotateAxis, -1f);
            rotated--;
            yield return 0.00001f;

        }

        fireTr.rotation = Quaternion.Euler(Vector3.zero);
    }

    /// <summary>
    /// 숨 불기가 1초 이후부터 숨 불기가 끝나기 전까지의 반응. 불 끄기
    /// </summary>
    /// <param name="fvc"></param>
    public void fireOff()
    {
        offEffect.SetActive(true);
    }
}
