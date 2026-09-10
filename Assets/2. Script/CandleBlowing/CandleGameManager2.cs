using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CandleGameManager2 : MonoBehaviour
{
    public float outtakeTime = 0f;
    private float intakeTime = 0f;
    public float sensorData { get; set; }
    public void SetsensorData(float value)
    {
        sensorData = value;
    }
    private float prevSensorData = 0f;

    public bool isGuiding = false;
    public bool inhaleReady = false;
    public bool exhaleReady = false;
    public bool isExhaling = false;
    public bool isExhaled = false;
    public bool isFinishScreen = false;

    public CandlePlayerCtrl playerCtrl = null;
    public GameManager gameManager = null;
    public GameObject candleUIManager = null;
    public GameObject[] candleControl = null;
    public GameObject rayCastCam = null;
    public GameObject selectionStick = null;
    public GameObject[] candleStages = null;

    public VRUIManager vrUiManager = null;
    public CandleSoundManager soundManager = null;
    public GameObject loggingManager = null;
    public int currCandleSeeing = 0;
    public int blowedCandles = 0;
    public int candleOffedOnThisStage = 0;

    public float clearTime = 0f;
    [SerializeField] private float guideTerm = 6f;
    [SerializeField] private float guideTime = 0f;
    private float currStateTime = 0f;

    public enum GameState { GUIDE = 0, SEEKINGCANDLE, INHALEREADY, INHALE, EXHALE, FINISH };
    public GameState currState = GameState.GUIDE;

    public int currStage = 1;
    public int guideCount = 1;

    public static CandleGameManager2 instance = null;
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

        candleUIManager = GameObject.Find("UIManager");
    }

    // Start is called before the first frame update
    void Start()
    {
        candleUIManager.SendMessage("SetStage", currStage);
        gameManager = GameManager.instance;
        vrUiManager = VRUIManager.instance;
        loggingManager = GameObject.Find("LoggingManager");

        StartCoroutine(CheckState());
    }

    private void Update()
    {
        //Debug.Log(candleOffedOnThisStage);
    }

    /// <summary>
    /// State 컨트롤. state가 Inhale or exhale이면 촛불에 센서 전달, Inhale일때만 UI에 흡기센서 전달
    /// </summary>
    IEnumerator CheckState()
    {
        while (true)
        {
            switch (currState)
            {
                case (GameState.GUIDE):
                    VRUIManager.instance.HideInhaleHud();
                    vrUiManager.HideExhaleHud();
                    if (!isGuiding)
                    {
                        isGuiding = true;
                        vrUiManager.GetComponent<VRUIManager>().ShowCandleGuide(currStage);
                        rayCastCam.GetComponent<CamRayCast>().ResetFlag();
                    }

                    /*if ((Input.touchCount > 0) || PulmoInput.PointerReleased)
                    {
                        if (currStage == 1)
                        {
                            if (guideCount == 4)
                            {
                                vrUiManager.GetComponent<VRUIManager>().HideCandleStartGuide(guideCount);

                                candleOffedOnThisStage = 0;
                                currState = GameState.SEEKINGCANDLE;
                            }
                            else if (guideCount >= 0 && guideCount < 4)
                            {
                                vrUiManager.GetComponent<VRUIManager>().ShowCandleStartGuide(guideCount);
                                guideCount++;
                                yield return new WaitForSeconds(1f);
                            }
                        }
                        else
                        {
                            yield return new WaitForSeconds(1f);
                            vrUiManager.HideCandleGuide();
                            candleOffedOnThisStage = 0;
                            currState = GameState.SEEKINGCANDLE;
                        }
                    }
                    candleUIManager.SendMessage("ResetUI");*/

                    //터치 없이 가이드 넘어가는 버전
                    guideTime += Time.fixedDeltaTime;
                    if (guideTime >= guideTerm || PulmoInput.PointerReleased)
                    {
                        if (currStage == 1)
                        {
                            if (guideCount >= 4)
                            {
                                vrUiManager.GetComponent<VRUIManager>().HideCandleStartGuide(4);

                                candleOffedOnThisStage = 0;
                                guideTime = 0f;
                                currState = GameState.SEEKINGCANDLE;
                            }
                            else if (guideCount >= 0 && guideCount < 4)
                            {
                                vrUiManager.GetComponent<VRUIManager>().ShowCandleStartGuide(guideCount);
                                guideCount++;
                                guideTime = 0f;
                                yield return new WaitForSeconds(1f);
                            }
                        }
                        else    //스테이지 두번째 이후거나 이미 가이드를 한 상황일 때
                        {
                            yield return new WaitForSeconds(1f);
                            vrUiManager.HideCandleGuide();
                            candleOffedOnThisStage = 0;
                            currState = GameState.SEEKINGCANDLE;
                        }
                    }
                    candleUIManager.SendMessage("ResetUI");
                    break;
                case (GameState.SEEKINGCANDLE):
                    //yield return new WaitForSeconds(0.5f);
                    selectionStick.SetActive(true);
                    clearTime += Time.fixedDeltaTime;
                    //playerCtrl.SeekingCandle();
                    vrUiManager.resetFill();
                    vrUiManager.ResetOutFill();
                    break;
                case (GameState.INHALEREADY):
                    //if (!inhaleReady)
                    //{
                    //    inhaleReady = true;
                    //    vrUiManager.GetComponent<VRUIManager>().HideCandleGuide();
                    //}
                    vrUiManager.ShowExhaleHud();
                    vrUiManager.ShowInhaleHud();
                    clearTime += Time.fixedDeltaTime;
                    rayCastCam.GetComponent<CamRayCast>().ResetFlag();
                    if (sensorData <= gameManager.sensorActionPotential * -1f)
                    {
                        currState = GameState.INHALE;
                        BluetoothManager.instance.checkingBLE = false;
                    }
                    break;
                case (GameState.INHALE):
                    if (exhaleReady == false)
                    {
                        exhaleReady = true;
                        //candleControl.SendMessage("ReadyForLaunch");
                        loggingManager.SendMessage("logPressure", "Inhale Start");
                    }

                    if (sensorData > 0f && sensorData-prevSensorData>=3f)
                    {
                        currState = GameState.EXHALE;
                    }
                    rayCastCam.GetComponent<CamRayCast>().messageSended = false;
                    loggingManager.SendMessage("logPressure", sensorData.ToString());
                    clearTime += Time.fixedDeltaTime;
                    intakeTime += Time.fixedDeltaTime;

                    vrUiManager.SendMessage("inHaleFill", sensorData);
                    candleControl[currCandleSeeing].SendMessage("Intake", sensorData);
                    prevSensorData = sensorData;





                    break;
                case (GameState.EXHALE):
                    if (!isExhaling)
                    {
                        //vrUiManager.SendMessage("HideInhaleHud");
                        isExhaling = true;
                        soundManager.StopMusic();
                        soundManager.SendMessage("OnBreatheSound");
                        loggingManager.SendMessage("logPressure", "Exhale Start");
                    }
                    vrUiManager.exHaleFill(sensorData);
                    rayCastCam.GetComponent<CamRayCast>().messageSended = false;
                    loggingManager.GetComponent<Logging>().logPressure(sensorData.ToString());
                    clearTime += Time.fixedDeltaTime;
                    outtakeTime += Time.fixedDeltaTime;
                    candleControl[currCandleSeeing].SendMessage("turningOffFire", sensorData);
                    if (outtakeTime >= 1f)
                        isExhaled = true;
                    if(isExhaled && sensorData<=gameManager.sensorActionPotential)
                    {
                        isExhaled = false;
                        outtakeTime = 0f;
                        switch (blowedCandles)
                        {
                            case 0:
                                blowedCandles = 1;
                                break;
                            case 1:
                                blowedCandles = 2;
                                break;
                            case 2:
                                blowedCandles = 3;
                                break;
                            case 3:
                                blowedCandles = 4;
                                break;
                            case 4:
                                blowedCandles = 5;
                                break;
                        }
                        if (blowedCandles == currStage)
                        {
                            yield return new WaitForSeconds(1f);
                            currState = GameState.FINISH;
                        }
                        else
                        {
                            
                            yield return new WaitForSeconds(0.5f);
                            CandleUIManager.instance.ResetShowStars();
                            currState = GameState.SEEKINGCANDLE;
                        }
                    }
                    break;

                case (GameState.FINISH):
                    if (!isFinishScreen)
                    {
                        candleUIManager.SendMessage("ScoreUI", candleOffedOnThisStage);
                        loggingManager.SendMessage("logClearTime", clearTime.ToString());
                        vrUiManager.SendMessage("HideInhaleHud");
                        isFinishScreen = true;
                        selectionStick.SetActive(false);
                    }

                    if(PulmoInput.TouchPresent || PulmoInput.PointerReleased || currStateTime >= 8f)
                    {
                        currStateTime = -10f;
                        if(currStage==5)
                        {
                            //Application.Quit();
                            GameManager.instance.SimulNextScene();
                        }
                        else
                        {
                            selectionStick.SetActive(false);
                            if (isFinishScreen)
                                toNextStage();
                        }    
                    }
                    currStateTime += Time.fixedDeltaTime;
                    break;

            }

            yield return null;
        }



    }

    public void EyesOnCandle()
    {
        currState = GameState.INHALEREADY;
    }

    public void EyesOffCandle()
    {
        currState = GameState.SEEKINGCANDLE;
        inhaleReady = false;
        vrUiManager.HideInhaleHud();
        vrUiManager.HideExhaleHud();
        vrUiManager.resetFill();
    }

    public void toNextStage()
    {
        switch(currStage)
        {
            case 1:
                currStage = 2;
                break;
            case 2:
                currStage = 3;
                break;
            case 3:
                currStage = 4;
                break;
            case 4:
                currStage = 5;
                break;
        }

        resetStage(currStage);

    }

    private void resetStage(int stage)
    {
        vrUiManager.BlockEye();
        for (int i = 1; i <= stage; i++)
        {
            candleControl[i - 1].GetComponent<CandleControl2>().ResetCandles();
            candleStages[i - 1].SetActive(true);
        }
        candleOffedOnThisStage = 0;
        blowedCandles = 0;
        candleUIManager.SendMessage("ResetScoreUI");
        vrUiManager.resetFill();
        vrUiManager.ResetOutFill();
        isGuiding = false;
        inhaleReady = false;
        exhaleReady = false;
        isExhaling = false;
        isExhaled = false;
        isFinishScreen = false;
        currState = GameState.GUIDE;
        candleUIManager.SendMessage("ResetUI");
        candleUIManager.SendMessage("SetStage", currStage);
        rayCastCam.GetComponent<CamRayCast>().messageSended = false;
        soundManager.GetComponent<CandleSoundManager>().StopMusic();
        soundManager.GetComponent<CandleSoundManager>().PlayMusic();
        CandleUIManager.instance.ResetShowStars();
        //CandleUIManager.instance.FillShowStar();

        vrUiManager.UnBlockEye();
    }

    
}
