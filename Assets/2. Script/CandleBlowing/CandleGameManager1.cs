using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;


public class CandleGameManager1 : MonoBehaviour
{
    private float clicktime = 0f;
    private float input1perframe = 11f;
    private float input2perframe = 6f;
    private int routineNum = 0;


    public InputField fev1Input = null;
    public InputField fvcInput = null;
    public float maxFev1 = 1000f;
    public float maxFvc = 1400f;

    public GameObject candleControl = null;
    public GameObject UImanager = null;

    public static CandleGameManager1 instance = null;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            //Destroy(this.gameObject);
        }
        
    }

    private void Start()
    {
        StartCoroutine("BreathInput");
    }

    /// <summary>
    /// 호기량에 따른 실시간 입력. 시뮬레이션 패널로의 반영과 동시에 실제 촛불 움직임을 CandleControl에 전달
    ///
    /// </summary>
    /// <returns></returns>
    IEnumerator BreathInput()
    {
        while (true)
        {

            if (PulmoInput.AHeld)
            {
                clicktime += 1 / 60f;
                if (clicktime >= 1f)
                {
                    fvcInput.text = (clicktime * input1perframe * 60).ToString();
                    candleControl.SendMessage("fvcReaction");

                }
                else
                {
                    fev1Input.text = (clicktime * input1perframe * 60).ToString();
                    candleControl.SendMessage("fev1Reaction");

                }
            }

            if (PulmoInput.SHeld)
            {
                clicktime += 1 / 60f;
                if (clicktime >= 1f)
                {
                    fvcInput.text = (clicktime * input2perframe * 60).ToString();
                    candleControl.SendMessage("fvcReaction");
                }
                else
                {
                    fev1Input.text = (clicktime * input2perframe * 60).ToString();
                    candleControl.SendMessage("fev1Reaction");
                }

            }

            if (clicktime >= 1f)
            {
                if (PulmoInput.DHeld)
                {
                    candleControl.SendMessage("blowEnd");
                    StopCoroutine("BreathInput");
                }

            }

            yield return 1 / 60f;
        }
    }

}
