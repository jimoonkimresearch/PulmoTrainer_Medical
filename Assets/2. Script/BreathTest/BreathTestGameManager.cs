using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PulmoTrainer.Calibration;
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

    private readonly bool[] completedMeasurements = new bool[3];
    private bool calibrationApplied;
    private Balloon balloon;
    private BreathTestUIManager testView;
    private Logging pressureLog;
    [SerializeField, Min(0f)] private float breathEndThreshold = 0.2f;
    [SerializeField, Min(0.01f)] private float breathEndHoldSeconds = 0.35f;
    [SerializeField, Min(0.1f)] private float measurementTimeoutSeconds = 8f;
    [SerializeField, Min(0.1f)] private float postBreathFlightTimeoutSeconds = 8f;
    private float neutralDuration;
    private float postBreathFlightTime;
    private bool measurementEnded;
    private float BreathStartThreshold => Mathf.Max(0.01f, Mathf.Abs(gameManager.sensorActionPotential));

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
        balloon = balloonControl != null ? balloonControl.GetComponent<Balloon>() : null;
        testView = testUIManager != null ? testUIManager.GetComponent<BreathTestUIManager>() : null;
        pressureLog = loggingManager != null ? loggingManager.GetComponent<Logging>() : null;
        if (balloon == null || testView == null || vrUiManager == null || gameManager == null)
        {
            Debug.LogError("BreathTest requires Balloon, UIManager, VRUIManager and GameManager references.", this);
            return;
        }
        testView.SetStage(currStage);

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
                        vrUiManager.ShowTestStartGuide(guideCount);
                        //vrUiManager.SetHeightProgress(0.0f);
                        testView.ResetUI();
                    }

                    /*if ((Input.touchCount > 0) || Input.GetMouseButtonUp(0))
                    {
                        if (currStage == 0)
                        {
                            if (guideCount == 5)
                            {
                                vrUiManager.HideTestGuide(guideCount);
                                currState3 = TestGameState.INHALEREADY;
                            }
                            else if (guideCount >= 0 && guideCount < 5)
                            {
                                vrUiManager.ShowTestStartGuide(guideCount);
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
                                vrUiManager.HideTestGuide(guideCount);
                                guideTime = 0f;
                                currState3 = TestGameState.INHALEREADY;
                            }
                            else if (guideCount >= 0 && guideCount < 5)
                            {
                                vrUiManager.ShowTestStartGuide(guideCount);
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
                    if (sensorData <= -BreathStartThreshold)
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
                            balloon.ReadyForLaunch();
                        }
                        else
                        {
                            toFinishState();
                            break;
                        }
                        pressureLog?.logPressure("Inhale Start");
                    }
                    rayCastCam.GetComponent<CamRayCast>().messageSended = false;
                    pressureLog?.logPressure(sensorData.ToString());
                    clearTime += Time.deltaTime;
                    intakeTime += Time.deltaTime;



                    maxInhaleCapacity[currStage] += sensorData;
                    // 흡기 압력은 음수이므로 가장 작은 값이 최대 흡기 압력이다.
                    if (maxInhalePressure[currStage] > sensorData)
                        maxInhalePressure[currStage] = sensorData;

                    vrUiManager.inHaleFill(sensorData);
                    balloon.Intake(sensorData);


                    if (sensorData >= BreathStartThreshold)
                    {
                        neutralDuration = 0f;
                        currState3 = TestGameState.EXHALE;
                    }
                    prevSensorData = sensorData;




                    break;
                case (TestGameState.EXHALE):
                    if (!isExhaling)
                    {
                        isExhaling = true;
                        pressureLog?.logPressure("Exhale Start");
                    }
                    rayCastCam.GetComponent<CamRayCast>().messageSended = false;
                    pressureLog?.logPressure(sensorData.ToString());
                    clearTime += Time.deltaTime;
                    if (!measurementEnded) UpdateExhaleMeasurement();
                    else postBreathFlightTime += Time.deltaTime;
                    CheckFlightEnd();

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
                        soundManager.ScoreBoardSound();
                        pressureLog?.logClearTime(clearTime.ToString());
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
            if (ApplyCompletedCalibration()) gameManager.SimulNextScene();
        }
        else
        {
            resetStage();
        }
    }

    void InhaleFinished()
    {
        if (measurementEnded || currState3 != TestGameState.INHALE) return;
        currState3 = TestGameState.EXHALE;
        StartBalloonFlight();
    }

    public void SensorStart()
    {
        if (currState3 == TestGameState.INHALEREADY)
            currState3 = TestGameState.INHALE;
    }

    public void toFinishState()
    {
        if (currState3 == TestGameState.FINISH) return;
        if (currState3 == TestGameState.EXHALE) EndMeasurement();
        currState3 = BreathTestGameManager.TestGameState.FINISH;
        neutralDuration = 0f;
        if (balloon != null) balloon.CompleteFlight();
    }

    private void UpdateExhaleMeasurement()
    {
        // outtakeTime is measurement time only; post-breath flight has its own clock.
        outtakeTime += Time.deltaTime;
        vrUiManager.exHaleFill(sensorData);
        if (outtakeTime < 1f) maxFEV1Capacity[currStage] += sensorData;
        maxExhaleCapacity[currStage] += sensorData;
        if (maxExhalePressure[currStage] < sensorData) maxExhalePressure[currStage] = sensorData;
        balloon.SetFlightPressure(sensorData);
        if (outtakeTime >= 1f && sensorData > gameManager.sensorActionPotential)
        {
            StartBalloonFlight();
            balloon.FvcOuttake(sensorData);
        }
        else if (outtakeTime < 1f) balloon.Fev1Outtake(sensorData);

        float endThreshold = Mathf.Clamp(breathEndThreshold, 0f, BreathStartThreshold * 0.9f);
        neutralDuration = Mathf.Abs(sensorData) <= endThreshold
            ? neutralDuration + Time.deltaTime : 0f;
        if (neutralDuration >= Mathf.Max(0.01f, breathEndHoldSeconds) ||
            outtakeTime >= Mathf.Max(0.1f, measurementTimeoutSeconds)) EndMeasurement();
    }

    private void StartBalloonFlight()
    {
        if (isBalloonFlying) return;
        isBalloonFlying = true;
        balloon.startLaunching();
        soundManager.StopMusic();
    }

    private void EndMeasurement()
    {
        if (measurementEnded) return;
        measurementEnded = true;
        postBreathFlightTime = 0f;
        if (currStage >= 0 && currStage < completedMeasurements.Length)
            completedMeasurements[currStage] = true;
        StartBalloonFlight();
        balloon.SealBreathInput();
    }

    private void CheckFlightEnd()
    {
        if (balloon.HasReachedFlightLimit ||
            (measurementEnded && (postBreathFlightTime >= Mathf.Max(0.1f, postBreathFlightTimeoutSeconds) ||
                !balloon.isActiveAndEnabled)))
            toFinishState();
    }

    private void resetStage()
    {
        if (isResetting) return;
        isResetting = true;
        neutralDuration = 0f;
        postBreathFlightTime = 0f;
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

        // SetNextBreathe가 상태를 INHALEREADY로 바꿀 때까지
        // isEnteringFinish를 true로 유지해 FINISH 재처리를 막는다.
        CancelInvoke(nameof(SetNextBreathe));
        Invoke(nameof(SetNextBreathe), Mathf.Max(0f, nextRoundDelaySeconds));
    }

    private bool ApplyCompletedCalibration()
    {
        if (calibrationApplied) return true;
        if (currStage != 3 || !completedMeasurements[0] || !completedMeasurements[1] || !completedMeasurements[2])
            return false;
        BreathCalibration result = BreathCalibration.AverageOfThree(
            MeasurementAt(0), MeasurementAt(1), MeasurementAt(2));
        if (!result.IsUsable)
        {
            Debug.LogError("BreathTest: three-round calibration is invalid; repeat the measurement before training.", this);
            return false;
        }
        gameManager.ApplyCalibration(result);
        calibrationApplied = true;
        return true;
    }

    private BreathCalibration MeasurementAt(int index)
    {
        return new BreathCalibration(maxInhaleCapacity[index], maxInhalePressure[index],
            maxExhaleCapacity[index], maxExhalePressure[index], maxFEV1Capacity[index]);
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
        testView.ResetScoreUI();
        testView.ResetUI();
        vrUiManager.resetFill();
        vrUiManager.ResetOutFill();
        isGuiding = false;
        inhaleReady = false;
        launchReady = false;
        isExhaling = false;
        isBalloonFlying = false;
        isNextSceneRequested = false;
        testView.SetStage(currStage);
        rayCastCam.GetComponent<CamRayCast>().messageSended = false;
        soundManager.GetComponent<BreathTestSoundManager>().PlayMusic();
        sensorData = 0f;
        prevSensorData = 0f;
        neutralDuration = 0f;
        postBreathFlightTime = 0f;
        measurementEnded = false;
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
