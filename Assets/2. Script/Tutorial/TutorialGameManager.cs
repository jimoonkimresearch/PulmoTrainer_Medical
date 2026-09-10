using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialGameManager : MonoBehaviour
{
    public GameObject UImanager = null;
    public Transform playerTr = null;

    public float playTime = 0f;
    public float intakedAir = 0f;
    public float outtakedAir = 0f;
    public float sensorData { get; set; }
    public void SetsensorData(float value)
    {
        sensorData = value;
    }

    //public bool isInhaleGuiding = false;
    //public bool inhaleReady = false;
    //public bool isInhaleing = false;
    //public bool isExhaleGuiding = false;
    //public bool exhaleReady = false;
    //public bool isExhaleing = false;
    //public bool isBreatheGuiding = false;
    //public bool breatheReady = false;
    //public bool isBreatheing = false;
    //public bool isFinishScreen = false;

    public bool isTutoGuiding = false;

    public GameManager gameManager = null;
    public TutorialUIManager uiManager = null;
    public GameObject selectionStick = null;
    public VRUIManager vrUiManager = null;
    public TutorialSoundManager soundManager = null;
    
    public GameObject currObjSeeing = null;

    //public enum GameState1
    //{
    //    INHALEGUIDE = 0, SEEKINGINHALETARGET, INHALEREADY, INHALE,
    //    EXHALEGUIDE, SEEKINGEXHALETARGET, EXHALEREADY, EXHALE,
    //    BREATHEGUIDE, SEEKINGBREEATHEETARGET, BREATHEREADY, BREATHE, FINISH
    //};
    //public GameState1 currState1;

    public enum GameState2
    {
        TUTORIALGUIDE = 0, SEEKINGTARGET, BREATHEREADY ,BREATHING, FINISH
    };
    public GameState2 currState2 = GameState2.TUTORIALGUIDE;

    

    public int guideCount = 2;

    public static TutorialGameManager instance = null;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(this.gameObject);
        }
        uiManager = GameObject.Find("UIManager").GetComponent<TutorialUIManager>();
    }


    // Start is called before the first frame update
    void Start()
    {
        gameManager = GameManager.instance;
        vrUiManager = VRUIManager.instance;
        StartCoroutine(CheckState2());
    }

    // Update is called once per frame
    void Update()
    {

    }

    /// <summary>
    /// State 컨트롤 1. 각각의 훈련을 따로 하는 코루틴
    /// /// </summary>
    //IEnumerator CheckState1()
    //{
    //    while (true)
    //    {
    //        switch (currState)
    //        {
    //            case (GameState.GUIDE):
    //                if (!isGuiding)
    //                {
    //                    isGuiding = true;
    //                    vrUiManager.GetComponent<VRUIManager>().ShowInhaleGuide(currStage);
    //                    rayCastCam.GetComponent<CamRayCast>().ResetFlag();
    //                }

    //                if ((Input.touchCount > 0) || Input.GetMouseButtonUp(0))
    //                {
    //                    if (currStage == 1)
    //                    {
    //                        if (guideCount == 5)
    //                        {
    //                            vrUiManager.GetComponent<VRUIManager>().HideInhaleStartGuide(guideCount);
    //                            currState = GameState.SEEKINGFOOD;
    //                        }
    //                        else if (guideCount >= 0 && guideCount < 5)
    //                        {
    //                            vrUiManager.GetComponent<VRUIManager>().ShowInhaleStartGuide(guideCount);
    //                            guideCount++;
    //                            yield return new WaitForSeconds(1f);
    //                        }
    //                    }
    //                    else
    //                    {
    //                        vrUiManager.HideInhaleGuide();
    //                        currState = GameState.SEEKINGFOOD;
    //                    }

    //                }
    //                break;
    //            case (GameState.SEEKINGFOOD):
    //                selectionStick.SetActive(true);
    //                VRUIManager.instance.HideInhaleHud();
    //                clearTime += Time.deltaTime;
    //                playerCtrl.SeekingFood();
    //                vrUiManager.resetFill();
    //                break;
    //            case (GameState.INHALEREADY):
    //                //해당 state 처입장시 1회만 작동할 코드
    //                if (!inhaleReady)
    //                {
    //                    inhaleReady = true;
    //                }
    //                //해당 state 시 반복작동할 코S
    //                playerCtrl.SeekingFood();
    //                VRUIManager.instance.ShowInhaleHud();
    //                clearTime += Time.deltaTime;
    //                rayCastCam.GetComponent<CamRayCast>().messageSended = false;
    //                if (sensorData <= gameManager.sensorActionPotential * -1f)
    //                {
    //                    currState = GameState.INHALE;
    //                    BluetoothManager.instance.checkingBLE = false;
    //                }
    //                break;
    //            case (GameState.INHALE):
    //                if (isInhaleing == false)
    //                {
    //                    isInhaleing = true;
    //                    loggingManager.SendMessage("logPressure", "Inhale Start");
    //                }

    //                rayCastCam.GetComponent<CamRayCast>().messageSended = false;
    //                loggingManager.SendMessage("logPressure", sensorData.ToString());
    //                clearTime += Time.deltaTime;
    //                intakedAir += sensorData;
    //                vrUiManager.SendMessage("inHaleFill", sensorData);
    //                if (vrUiManager.fillAmt > 1f)
    //                {
    //                    currFoodSeeing.SendMessage("Inhaled");
    //                    playerCtrl.InhaleFood();
    //                    soundManager.OnBreatheSound();
    //                    EyesOffFood();
    //                    yield return new WaitForSeconds(1f);
    //                    soundManager.ChewSound();
    //                }
    //                if (currFoodeat == currStage * 3)
    //                {
    //                    currState = GameState.FINISH;
    //                }
    //                break;
    //            case (GameState.FINISH):
    //                if (!isFinishScreen)
    //                {
    //                    selectionStick.SetActive(false);
    //                    yield return new WaitForSeconds(0.5f);
    //                    inhaleUIManager.InhaleScoreUI();
    //                    soundManager.SendMessage("ScoreBoardSound");
    //                    loggingManager.SendMessage("logClearTime", clearTime.ToString());
    //                    vrUiManager.SendMessage("ShowInhaleHud");
    //                    //foodReseter.SendMessage("ResetFoods");
    //                    isFinishScreen = true;
    //                }


    //                if ((Input.touchCount > 0) || Input.GetMouseButtonUp(0))
    //                {
    //                    if (currStage == 5)
    //                    {
    //                        Application.Quit();
    //                    }
    //                    else
    //                    {
    //                        selectionStick.SetActive(false);
    //                        if (isFinishScreen)
    //                            toNextStage();
    //                    }
    //                }
    //                break;

    //        }

    //        yield return null;
    //    }

    IEnumerator CheckState2()
    {
        while(true)
        {
            switch(currState2)
            {
                case GameState2.TUTORIALGUIDE:
                    if (!isTutoGuiding)
                    {
                        vrUiManager.ShowTutoStartGuide(1);
                        isTutoGuiding = true;
                    }
                    vrUiManager.HideInhaleHud();
                    
                    if ((Input.touchCount > 0) || Input.GetMouseButtonUp(0))
                    {
                        Debug.Log("터치");
                        if (guideCount == 5)
                        {
                            vrUiManager.HideTutoStartGuide(guideCount);
                            currState2 = GameState2.SEEKINGTARGET;
                        }
                        else if (guideCount >= 1 && guideCount < 5)
                        {
                            guideCount++;
                            vrUiManager.ShowTutoStartGuide(guideCount);
                            yield return new WaitForSeconds(1f);
                        }
                        //currState2 = GameState2.SEEKINGTARGET;
                    }
                    break;
                case GameState2.SEEKINGTARGET:
                    selectionStick.SetActive(true);
                    vrUiManager.resetFill();
                    vrUiManager.ResetOutFill();
                    playTime += Time.deltaTime;
                    if ((Input.touchCount > 0) || Input.GetMouseButtonUp(0))
                    {
                        if(playTime>=10f)
                            currState2 = GameState2.FINISH;
                    }
                    break;
                case GameState2.BREATHEREADY:
                    playTime += Time.deltaTime;
                    if (sensorData < -1f || sensorData > 1f)
                        currState2 = GameState2.BREATHING;
                    if ((Input.touchCount > 0) || Input.GetMouseButtonUp(0))
                    {
                        if (playTime >= 10f)
                            currState2 = GameState2.FINISH;
                    }
                    break;
                case GameState2.BREATHING:
                    vrUiManager.ShowInhaleHud();
                    uiManager.SetPressureTxt(sensorData);
                    if (sensorData>0f)
                    {
                        vrUiManager.exHaleFill(sensorData);
                        currObjSeeing.SendMessage("OnExhale", sensorData);
                        if ((sensorData / gameManager.maxInhalePressure) < 0f)
                            uiManager.SetPressureGuage(sensorData / gameManager.maxInhalePressure * -1f);
                        else
                            uiManager.SetPressureGuage(sensorData / gameManager.maxInhalePressure);
                    }
                    else if(sensorData<0f)
                    {
                        vrUiManager.inHaleFill(sensorData);
                        currObjSeeing.SendMessage("OnInhale", sensorData);
                        if ((sensorData / gameManager.maxInhalePressure) > 0f)
                            uiManager.SetPressureGuage(sensorData / gameManager.maxInhalePressure * -1f);
                        else
                            uiManager.SetPressureGuage(sensorData / gameManager.maxInhalePressure);
                    }

                    
                    break;
                case GameState2.FINISH:
                    selectionStick.SetActive(false);
                    vrUiManager.HideInhaleHud();
                    vrUiManager.HideExhaleHud();
                    /*if ((Input.touchCount > 0) || Input.GetMouseButtonUp(0))
                    {
                        Application.Quit();
                    }*/
                    break;
            }
            yield return null;
        }
    }

    public void EyesOnBall()
    {
        currState2 = GameState2.BREATHEREADY;
    }

    public void EyesOffBall()
    {
        currState2 = GameState2.SEEKINGTARGET;
        intakedAir = 0f;
        outtakedAir = 0f;
        vrUiManager.HideInhaleHud();
        vrUiManager.resetFill();
    }

    public void SimulNextScene()
    {
        currState2 = GameState2.FINISH;
        Invoke("ToNextScene", 1f);
    }

    public void ToNextScene()
    {

    }

}