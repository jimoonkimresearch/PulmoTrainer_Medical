using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CandleControl2 : MonoBehaviour
{
    public Transform[] candleFires = null;
    public CandleGameManager2 candleGameManager = null;
    public GameObject uiManager = null;

    private int candleCount = 10;
    private int offNum = 1;
    public int candlesForOff = 0;

    private float intakedAir = 0f;
    private float outtakedAir = 0f;
    private float outtakeTime = 0f;
    private float totalAir = 0f;
    private float sensorPlus = 0f;
    private float sensorMinus = 0f;

    public float tiltRatio = 300f;
    public float airPerOffcandle = 220f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Intake(float sensorData)
    {
        if (sensorData > 0f)
        {
            sensorMinus = (sensorData * -1f);

        }
        else
        {
            sensorMinus = sensorData;
        }
        intakedAir += sensorMinus;
        
        TiltFire(sensorMinus);
    }

    private void TiltFire(float sensorData)
    {
        float fireTiltAngle = (sensorData / GameManager.instance.maxInhalePressure) * tiltRatio;
        for (int i = 0; i < candleCount; i++)
        {
            candleFires[i].rotation = Quaternion.Euler(new Vector3(fireTiltAngle * -1f, 0f, 0f));
        }
    }

    private void turningOffFire(float sensorData)
    {
        //if (sensorData < GameManager.instance.sensorActionPotential && outtakeTime>1f)
        //{
        //    uiManager.SendMessage("ScoreUI", totalAir);
        //    candleGameManager.currState = CandleGameManager2.GameState.FINISH;
        //}
        if (sensorData < 0f)
        {
            sensorPlus = (sensorData * -1f);

        }
        else
        {
            sensorPlus = sensorData;
        }
        outtakeTime += Time.deltaTime;
        TiltFire(sensorPlus);
        outtakedAir += sensorPlus;
        totalAir = (1f + (intakedAir / GameManager.instance.maxIntake)) * outtakedAir;
        candlesForOff = (int)(totalAir / airPerOffcandle) ;
        //Debug.Log(candlesForOff);
        //offNum은 이번에 끌 차례인 촛불, candlesForOff는 호흡입력 결과 꺼야하는 촛불 수
        if (offNum <= candlesForOff && offNum < 11)
        {
            candleFires[offNum - 1].SendMessage("ShrinkAndOff");
            uiManager.GetComponent<CandleUIManager>().GetOffCandleStar(offNum);
            offNum++;
            candleGameManager.candleOffedOnThisStage++;
        }
    }

    public void ResetCandles()
    {
        for(int i = 0; i < candleCount; i++)
        {
            candleFires[i].gameObject.SetActive(true);
            candleFires[i].localScale = Vector3.one * 0.05f;
            candleFires[i].rotation = Quaternion.Euler(Vector3.zero);
        }
        intakedAir = 0f;
        outtakedAir = 0f;
        totalAir = 0f;
        offNum = 1;
        outtakeTime = 0f;
        sensorPlus = 0f;
        sensorMinus = 0f;
        candlesForOff = 0;
    }
}
