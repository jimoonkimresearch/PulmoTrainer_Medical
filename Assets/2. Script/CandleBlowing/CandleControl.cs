using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CandleControl : MonoBehaviour
{
    public Transform[] candleFires = null;

    private float fev1;
    private float fvc;
    private float smallerNumber;
    private float sizeBorder = 0.01f;
    private int offNum = 0;
    private int offTurn = 0;

    public void fev1Reaction()
    {
        fev1 = float.Parse(CandleGameManager1.instance.fev1Input.text);
        for(int i=0;i<10;i++)
            candleFires[i].gameObject.SendMessage("BlowStart", i);
        StartCoroutine("fireSmaller");
    }

    IEnumerator fireSmaller()
    {
        
        candleFires[(int)(fev1 / 100f)].gameObject.SendMessage("FireSmaller", fev1);
        yield return null;
        
        StopCoroutine("fireSmaller");
    }

    /// <summary>
    /// 1초 이후 fvc에 따라 줄어들었던 불이 정해진 갯수만큼 꺼지게 하는 것
    /// offTurn은 이번에 꺼질 차례인 촛불을 의미.
    /// 꺼칠 차례의 촛불의 FireReaction 스크립트에 꺼지라는 메시지 전달.
    /// </summary>
    public void fvcReaction()
    {
        fvc = float.Parse(CandleGameManager1.instance.fvcInput.text);
        float offRatio = (fvc / CandleGameManager1.instance.maxFvc);
        offNum = (int)(offRatio * (int)(fev1 / 100f));
        if(offNum>offTurn && offRatio<=1.01f)
        { 
            candleFires[offTurn].gameObject.SendMessage("fireOff", fvc);
            offTurn++;
        }
    }

    /// <summary>
    /// 호기 종료 이후에 각 촛불의 기울기를 원래대로 되돌림. 게임컨트롤에서 불러옴
    /// </summary>
    public void blowEnd()
    {
        for (int i = (int)smallerNumber; i < 10; i++)
        {
            candleFires[i].gameObject.SendMessage("blowFinished", i);
        }
    }
}
