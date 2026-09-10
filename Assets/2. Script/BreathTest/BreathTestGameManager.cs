using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class BreathTestGameManager : MonoBehaviour
{


    public enum TestGameState
    {
        TUTORIALGUIDE = 0, INHALEREADY, INHALE, EXHALE, FINISH
    };
    public TestGameState currState3 = TestGameState.TUTORIALGUIDE;

    public float outtakeTime = 0f;
    private float intakeTime = 0f;

    private float[] maxInhalePressure = new float[3];
    private float[] maxInhaleCapacity = new float[3];
    private float[] maxExhalePressure = new float[3];
    private float[] maxExhaleCapacity = new float[3];
    private float[] maxFEV1Capacity = new float[3];

    float averageInhaleCapa = 0f;
    float averageInhalePres = 0f;
    float averageFEV1 = 0f;
    float averageExhaleCapa = 0f;
    float averageExhalePres = 0f;

    public float sensorData { get; set; }
    public void SetsensorData(float value)
    {
        sensorData = value;
    }

    public float prevSensorData = 0;

    private Scene currScene;

    public bool isGuiding = false;
    public bool inhaleReady = false;
    public bool launchReady = false;
    public bool isExhaling = false;
    public bool isBalloonFlying = false;
    public bool isEnteringFinish = false;
    public bool isResetting = false;

    public int guideCount = 0;

    public GameManager gameManager = null;
    public GameObject balloonControl = null;
    public GameObject testUIManager = null;
    public GameObject rayCastCam = null;
    public GameObject selectionStick = null;
    public VRUIManager vrUiManager = null;
    public BreathTestSoundManager soundManager = null;
    public GameObject loggingManager = null;

    public float clearTime = 0f;

    [SerializeField]
    private float guideTime = 0f;

    public float guideTerm = 7f;

    [Header("Round transition delays (seconds)")]
    [Min(0f)] public float finishDisplaySeconds = 2f;
    [Min(0f)] public float nextRoundDelaySeconds = 1f;

    private float finishTime = 0f;
    private bool isNextSceneRequested = false;

    public int currStage = 0;

    public static BreathTestGameManager instance = null;

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



    }

    private void Start()
    {
        gameManager = GameManager.instance;
        vrUiManager = VRUIManager.instance;
        loggingManager = GameObject.Find("LoggingManager");

        testUIManager = GameObject.Find("UIManager");
        testUIManager.SendMessage("SetStage", currStage);

        StartCoroutine(CheckState());

        for (int i = 0; i < 3; i++)
        {
            maxInhaleCapacity[i] = 0f;
            maxInhalePressure[i] = 0f;
            maxExhaleCapacity[i] = 0f;
            maxExhalePressure[i] = 0f;
            maxFEV1Capacity[i] = 0f;
        }
    }

    void GetCurrScene(Scene scene, LoadSceneMode mode)
    {
        currScene = scene;
    }




    /// <summary>
    /// State 컨트롤. state가 Inhale or exhale이면 로켓에 센서 전달, Inhale일때만 UI에 흡기센서 전달
    /// </summary>
    IEnumerator CheckState()
    {
        while (true)
        {
            switch (currState3)
            {
                case (TestGameState.TUTORIALGUIDE):
                    if (!isGuiding)
                    {
                        isGuiding = true;
                        vrUiManager.GetComponent<VRUIManager>().ShowTestStartGuide(guideCount);
                        //vrUiManager.SetHeightProgress(0.0f);
                        testUIManager.SendMessage("ResetUI");
                    }

                    /*if ((Input.touchCount > 0) || Input.GetMouseButtonUp(0))
                    {
                        if (currStage == 0)
                        {
                            if (guideCount == 5)
                            {
                                vrUiManager.GetComponent<VRUIManager>().HideTestGuide(guideCount);
                                currState3 = TestGameState.INHALEREADY;
                            }
                            else if (guideCount >= 0 && guideCount < 5)
                            {
                                vrUiManager.GetComponent<VRUIManager>().ShowTestStartGuide(guideCount);
                                guideCount++;
                                yield return new WaitForSeconds(1f);
                            }
                        }
                        else    //스테이지 두번째 이후거나 이미 가이드를 한 상황일 때
                        {
                            yield return new WaitForSeconds(1f);
                            //vrUiManager.ShowTestStartGuide(currStage);
                            currState3 = TestGameState.INHALEREADY;
                        }
                    }*/

                    //터치 없이 가이드 넘어가는 버전
                    guideTime += Time.deltaTime;
                    if (guideTime >= guideTerm || PulmoInput.PointerReleased)
                    {
                        if (currStage == 0)
                        {
                            if (guideCount == 5)
                            {
                                vrUiManager.GetComponent<VRUIManager>().HideTestGuide(guideCount);
                                guideTime = 0f;
                                currState3 = TestGameState.INHALEREADY;
                            }
                            else if (guideCount >= 0 && guideCount < 5)
                            {
                                vrUiManager.GetComponent<VRUIManager>().ShowTestStartGuide(guideCount);
                                guideCount++;
                                guideTime = 0f;
                                yield return new WaitForSeconds(1f);
                            }
                        }
                        else    //스테이지 두번째 이후거나 이미 가이드를 한 상황일 때
                        {
                            yield return new WaitForSeconds(1f);
                            //vrUiManager.ShowTestStartGuide(currStage);
                            currState3 = TestGameState.INHALEREADY;
                        }
                    }

                    break;
                case (TestGameState.INHALEREADY):
                    if (!inhaleReady)
                    {
                        inhaleReady = true;
                        vrUiManager.resetFill();
                        vrUiManager.ResetOutFill();
                        vrUiManager.ShowInhaleHud();
                        vrUiManager.ShowExhaleHud();
                    }
                    selectionStick.SetActive(true);
                    clearTime += Time.deltaTime;
                    rayCastCam.GetComponent<CamRayCast>().messageSended = false;
                    //Debug.Log(sensorData);
                    if (sensorData <= gameManager.sensorActionPotential * -1f && (sensorData - prevSensorData) <= -2f)
                    {
                        currState3 = TestGameState.INHALE;
                        //BluetoothManager.instance.checkingBLE = false;
                    }
                    prevSensorData = sensorData;
                    break;
                case (TestGameState.INHALE):
                    if (launchReady == false)
                    {
                        launchReady = true;
                        if (currStage <= 2)
                        {
                            balloonControl.SendMessage("ReadyForLaunch");
                        }
                        else
                        {
                            toFinishState();
                            break;
                        }
                        loggingManager.SendMessage("logPressure", "Inhale Start");
                    }
                    rayCastCam.GetComponent<CamRayCast>().messageSended = false;
                    loggingManager.SendMessage("logPressure", sensorData.ToString());
                    clearTime += Time.deltaTime;
                    intakeTime += Time.deltaTime;



                    maxInhaleCapacity[currStage] += sensorData;
                    // 흡기 압력은 음수이므로 가장 작은 값이 최대 흡기 압력이다.
                    if (maxInhalePressure[currStage] > sensorData)
                        maxInhalePressure[currStage] = sensorData;

                    vrUiManager.SendMessage("inHaleFill", sensorData);
                    balloonControl.SendMessage("Intake", sensorData);


                    if (sensorData > gameManager.sensorActionPotential && (sensorData - prevSensorData) >= 2f)
                    {
                        currState3 = TestGameState.EXHALE;
                    }
                    prevSensorData = sensorData;




                    break;
                case (TestGameState.EXHALE):
                    if (!isExhaling)
                    {
                        isExhaling = true;
                        loggingManager.SendMessage("logPressure", "Exhale Start");
                    }
                    vrUiManager.exHaleFill(sensorData);
                    rayCastCam.GetComponent<CamRayCast>().messageSended = false;
                    loggingManager.GetComponent<Logging>().logPressure(sensorData.ToString());
                    clearTime += Time.deltaTime;
                    outtakeTime += Time.deltaTime;
                    //vrUiManager.SetHeightProgress(balloonControl.GetComponent<Transform>().position.y);
                    //Debug.Log(outtakeTime);
                    if (outtakeTime < 1f)
                    {
                        maxFEV1Capacity[currStage] += sensorData;
                        //soundManager.SendMessage("OnBreatheOutSound");
                    }
                    maxExhaleCapacity[currStage] += sensorData;
                    // 호기 압력은 양수이므로 가장 큰 값이 최대 호기 압력이다.
                    if (maxExhalePressure[currStage] < sensorData)
                        maxExhalePressure[currStage] = sensorData;

                    if (outtakeTime >= 1f && sensorData > gameManager.sensorActionPotential)
                    {
                        if (!isBalloonFlying)
                        {
                            isBalloonFlying = true;
                            balloonControl.SendMessage("startLaunching");
                            soundManager.StopMusic();
                        }
                        balloonControl.SendMessage("FvcOuttake", sensorData);
                        //vrUiManager.HideExhaleHud();
                        //vrUiManager.HideInhaleHud();
                    }
                    else if (outtakeTime < 1f)
                    {
                        balloonControl.SendMessage("Fev1Outtake", sensorData);
                    }

                    balloonControl.SendMessage("CheckExhaleEnd");

                    break;

                case (TestGameState.FINISH):

                    if (!isEnteringFinish)
                    {
                        // FINISH 처리는 호흡 1회당 한 번만 실행한다.
                        // CheckState 코루틴은 계속 살아 있으므로 먼저 재진입을 막아야 한다.
                        isEnteringFinish = true;
                        finishTime = 0f;

                        //resetStage();
                        selectionStick.SetActive(false);
                        vrUiManager.HideExhaleHud();
                        vrUiManager.HideInhaleHud();
                        soundManager.SendMessage("ScoreBoardSound");
                        loggingManager.SendMessage("logClearTime", clearTime.ToString());
                        currStage = currStage + 1;
                        //vrUiManager.SendMessage("ShowInhaleHud");
                        //Debug.Log("finish");

                    }

                    break;
                default:
                    break;
            }

            yield return null;
        }



    }

    private void Update()
    {
        if (!isEnteringFinish || isResetting || isNextSceneRequested) return;
        finishTime += Time.deltaTime;
        if (finishTime < Mathf.Max(0f, finishDisplaySeconds)) return;

        if (currStage >= 3)
        {
            isNextSceneRequested = true;
            GameManager.instance.SimulNextScene();
        }
        else
        {
            resetStage();
        }
    }

    void InhaleFinished()
    {
        currState3 = TestGameState.EXHALE;
        balloonControl.SendMessage("startLaunching");
    }

    public void SensorStart()
    {
        balloonControl.SendMessage("InHaleStart");
    }

    public void toFinishState()
    {
        currState3 = BreathTestGameManager.TestGameState.FINISH;
    }

    private void resetStage()
    {
        if (isResetting) return;
        isResetting = true;
        outtakeTime = 0f;
        finishTime = 0f;
        clearTime = 0f;
        intakeTime = 0f;
        selectionStick.SetActive(false);
        vrUiManager.HideExhaleHud();
        vrUiManager.HideInhaleHud();
        //currStage = currStage + 1;
        //vrUiManager.SendMessage("ShowInhaleHud");
        Debug.Log(currStage);

        // 첫 번째 호흡이 끝난 직후에만 첫 측정값을 기준으로 최대치를 설정한다.
        // FINISH에서 currStage를 증가시켰으므로 첫 호흡 직후 currStage는 1이다.
        if (currStage == 1)
        {
            averageInhaleCapa = maxInhaleCapacity[0];
            averageInhalePres = maxInhalePressure[0];
            averageFEV1 = maxFEV1Capacity[0];
            averageExhaleCapa = maxExhaleCapacity[0];
            averageExhalePres = maxExhalePressure[0];

            // 풍선에서 오류로 제대로 측정되지 않은 경우 최소치 입력
            if (averageInhaleCapa >= -1000f)
                averageInhaleCapa = -1000f;
            if (averageExhaleCapa <= 800f)
                averageExhaleCapa = 800f;

            // 첫 호흡 기준보다 100 높게 설정하고 2·3회차에서는 유지한다.
            GameManager.instance.maxIntake = averageInhaleCapa - 100f;
            GameManager.instance.maxInhalePressure = averageInhalePres - 10f;
            GameManager.instance.maxFev1 = averageFEV1 + 100f;
            GameManager.instance.maxFvc = averageExhaleCapa + 100f;
            GameManager.instance.maxExhalePressure = averageExhalePres + 10f;
        }

        // SetNextBreathe가 상태를 INHALEREADY로 바꿀 때까지
        // isEnteringFinish를 true로 유지해 FINISH 재처리를 막는다.
        CancelInvoke(nameof(SetNextBreathe));
        Invoke(nameof(SetNextBreathe), Mathf.Max(0f, nextRoundDelaySeconds));
    }

    private void SetNextBreathe()
    {
        // Direct call also works while the balloon GameObject is inactive.
        // Restore its pose before ResetBalloon makes it visible again.
        Balloon balloon = balloonControl != null ? balloonControl.GetComponent<Balloon>() : null;
        if (balloon == null)
        {
            Debug.LogError("Balloon Control must reference the GameObject containing Balloon.cs.", this);
            return;
        }
        balloon.ResetBalloon();
        testUIManager.SendMessage("ResetScoreUI");
        testUIManager.SendMessage("ResetUI");
        vrUiManager.resetFill();
        vrUiManager.ResetOutFill();
        isGuiding = false;
        inhaleReady = false;
        launchReady = false;
        isExhaling = false;
        isBalloonFlying = false;
        isNextSceneRequested = false;
        testUIManager.SendMessage("SetStage", currStage);
        rayCastCam.GetComponent<CamRayCast>().messageSended = false;
        soundManager.GetComponent<BreathTestSoundManager>().PlayMusic();
        sensorData = 0f;
        prevSensorData = 0f;
        outtakeTime = 0f;
        intakeTime = 0f;
        clearTime = 0f;
        finishTime = 0f;
        guideTime = 0f;
        currState3 = TestGameState.INHALEREADY;
        isEnteringFinish = false;
        isResetting = false;
        vrUiManager.ShowInhaleHud();
        vrUiManager.ShowExhaleHud();
    }

    private void OnDestroy()
    {
        CancelInvoke(nameof(SetNextBreathe));
        if (instance == this) instance = null;
    }
}