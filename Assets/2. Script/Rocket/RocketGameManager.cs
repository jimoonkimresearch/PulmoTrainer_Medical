using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class RocketGameManager : MonoBehaviour
{
    public float outtakeTime = 0f;
    private float intakeTime = 0f;
    public float sensorData { get; set; }
    public void SetsensorData(float value)
    {
        sensorData = value;
    }

    public float prevSensorData = 0;

    private Scene currScene;
    private RocketBehavior2 rocket;
    private RocketUIManager rocketView;
    private Logging pressureLog;
    private bool transitionInProgress;
    private bool nextGameRequested;

    [SerializeField] private bool isGuiding = false;
    [SerializeField] private bool inhaleReady = false;
    [SerializeField] private bool launchReady = false;
    [SerializeField] private bool isRocketFlying = false;
    [SerializeField] private bool isFinishScreen = false;

    [SerializeField] private int guideCount = 1;

    public GameManager gameManager = null;
    public GameObject rocketControl;
    public GameObject rocketUIManager = null;
    public GameObject stage3Planet = null;
    public GameObject stage4Planet = null;
    public GameObject stage5Planet = null;
    public GameObject rayCastCam = null;
    public GameObject selectionStick = null;
    public VRUIManager vrUiManager = null;
    public RocketSoundManager soundManager = null;
    public GameObject loggingManager = null;

    public float clearTime = 0f;
    [SerializeField] private float guideTerm = 6f;
    [SerializeField] private float guideTime = 0f;
    [SerializeField] private float currStateTime = 0f;

    public enum RocketState { GUIDE = 0, INHALEREADY, INHALE, EXHALE, FINISH };
    public RocketState currState = RocketState.GUIDE;

    public int currStage = 1;

    public static RocketGameManager instance = null;

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

        //SceneManager.sceneLoaded += OnSceneLoaded;
        //rocketUIManager = GameObject.Find("UIManager");
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        switch (scene.name)
        {

            case ("1-2. RocketStage2"):
                currStage = 2;
                break;
            case ("1-345. RocketStage345"):
                currStage = 3;
                break;

        }
        //Debug.Log("currstage: " + currStage);
        rocketView.SetStage(currStage);
    }

    private void Start()
    {
        gameManager = GameManager.instance;
        vrUiManager = VRUIManager.instance;
        loggingManager = GameObject.Find("LoggingManager");
        rocketUIManager = GameObject.Find("UIManager");

        rocket = rocketControl != null ? rocketControl.GetComponent<RocketBehavior2>() : null;
        rocketView = rocketUIManager != null ? rocketUIManager.GetComponent<RocketUIManager>() : null;
        pressureLog = loggingManager != null ? loggingManager.GetComponent<Logging>() : null;
        if (rocket == null || rocketView == null || gameManager == null || vrUiManager == null)
        {
            Debug.LogError("Rocket requires RocketBehavior2, RocketUIManager, GameManager and VRUIManager.", this);
            return;
        }
        rocket.ConfigureStage(500f + currStage * 100f);
        rocketView.SetStage(currStage);
        StartCoroutine(CheckState());
        //SceneManager.sceneLoaded += OnSceneLoaded;
        
    }

    void GetCurrScene(Scene scene, LoadSceneMode mode)
    {
        currScene = scene;
    }


    private void Update()
    {
        if (PulmoInput.Digit1Pressed)
        {
            toNextStage();
        }
    }

    /// <summary>
    /// State 컨트롤. state가 Inhale or exhale이면 로켓에 센서 전달, Inhale일때만 UI에 흡기센서 전달
    /// </summary>
    IEnumerator CheckState()
    {
        while (true)
        {
            switch (currState)
            {
                case (RocketState.GUIDE):
                    if (!isGuiding)
                    {
                        isGuiding = true;
                        vrUiManager.ShowGuide(currStage);
                        vrUiManager.SetHeightProgress(0.0f);
                        rocketView.ResetUI();
                    }

                    /*if ((Input.touchCount > 0) || PulmoInput.PointerReleased)
                    {
                        if (currStage == 1)
                        {
                            if (guideCount == 4)
                            {
                                vrUiManager.HideRocketStartGuide(guideCount);
                                currState = RocketState.INHALEREADY;
                            }
                            else if (guideCount >= 0 && guideCount < 4)
                            {
                                vrUiManager.ShowRocketStartGuide(guideCount);
                                guideCount++;
                                yield return new WaitForSeconds(1f);
                            }
                        }
                        else
                        {
                            yield return new WaitForSeconds(1f);
                            vrUiManager.HideGuide(currStage);
                            currState = RocketState.INHALEREADY;
                        }
                    }*/

                    //터치 없이 가이드 넘어가는 버전
                    guideTime += Time.deltaTime;
                    if (guideTime >= guideTerm || PulmoInput.PointerReleased)
                    {
                        if (currStage == 1)
                        {
                            if (guideCount >= 4)
                            {
                                vrUiManager.HideRocketStartGuide(4);
                                guideTime = 0f;
                                currState = RocketState.INHALEREADY;
                            }
                            else if (guideCount >= 0 && guideCount < 4)
                            {
                                Debug.Log(guideCount);
                                vrUiManager.ShowRocketStartGuide(guideCount);
                                guideCount++;
                                guideTime = 0f;
                                yield return new WaitForSeconds(1f);
                            }
                        }
                        else    //스테이지 두번째 이후거나 이미 가이드를 한 상황일 때
                        {
                            yield return new WaitForSeconds(1f);
                            vrUiManager.HideGuide(currStage);
                            currState = RocketState.INHALEREADY;
                        }
                    }

                    break;
                case (RocketState.INHALEREADY):
                    if (!inhaleReady)
                    {
                        inhaleReady = true;
                        vrUiManager.resetFill();
                        vrUiManager.ResetOutFill();
                        vrUiManager.ShowInhaleHud();
                        vrUiManager.ShowExhaleHud();
                        vrUiManager.maxRocketHeight = 500 + (currStage * 100);
                    }
                    selectionStick.SetActive(true);
                    clearTime += Time.deltaTime;
                    rayCastCam.GetComponent<CamRayCast>().messageSended = false;

                    if (sensorData <= -Mathf.Max(0.01f, Mathf.Abs(gameManager.sensorActionPotential)))
                    {
                        currState = RocketState.INHALE;
                        //BluetoothManager.instance.checkingBLE = false;
                    }
                    prevSensorData = sensorData;
                    break;
                case (RocketState.INHALE):
                    if (launchReady == false)
                    {
                        launchReady = true;
                        if (currStage == 1)
                        {
                            rocket.ReadyForLaunch();
                        }
                        pressureLog?.logPressure("Inhale Start");
                    }
                    rayCastCam.GetComponent<CamRayCast>().messageSended = false;
                    pressureLog?.logPressure(sensorData.ToString());
                    clearTime += Time.deltaTime;
                    intakeTime += Time.deltaTime;

                    vrUiManager.inHaleFill(sensorData);
                    rocket.Intake(sensorData);

                    if (sensorData >= Mathf.Max(0.01f, Mathf.Abs(gameManager.sensorActionPotential)))
                    {
                        currState = RocketState.EXHALE;
                    }
                    prevSensorData = sensorData;




                    break;
                case (RocketState.EXHALE):
                    if (!isRocketFlying)
                    {
                        isRocketFlying = true;
                        rocket.startLaunching();
                        soundManager.StopMusic();
                        soundManager.OnLaunchSound();
                        pressureLog?.logPressure("Exhale Start");
                    }
                    vrUiManager.exHaleFill(sensorData);
                    rayCastCam.GetComponent<CamRayCast>().messageSended = false;
                    pressureLog?.logPressure(sensorData.ToString());
                    clearTime += Time.deltaTime;
                    outtakeTime += Time.deltaTime;
                    vrUiManager.SetHeightProgress(rocket.Height);

                    rocket.ProcessExhale(sensorData, Time.deltaTime);

                    break;

                case (RocketState.FINISH):
                    selectionStick.SetActive(false);
                    if (!isFinishScreen)
                    {
                        vrUiManager.HideExhaleHud();
                        vrUiManager.HideInhaleHud();
                        soundManager.ScoreBoardSound();
                        pressureLog?.logClearTime(clearTime.ToString());
                        //vrUiManager.SendMessage("ShowInhaleHud");
                        
                        isFinishScreen = true;
                    }
                    else if (PulmoInput.TouchPresent || PulmoInput.PointerReleased || currStateTime >= 8f)
                    {
                        //StopCoroutine(CheckState());
                        currStateTime = -10f;
                        if (currStage == 5)
                        {
                            //Application.Quit();
                            if (!nextGameRequested)
                            {
                                nextGameRequested = true;
                                gameManager.SimulNextScene();
                            }
                        }
                        else
                        {
                            Debug.Log("다음씬 함수 시작 ");
                            toNextStage();
                            break;
                        }

                    }
                    currStateTime += Time.deltaTime;
                    break;

            }

            yield return null;
        }



    }


    void InhaleFinished()
    {
        currState = RocketState.EXHALE;
        rocket.startLaunching();
    }

    public void SensorStart()
    {
        rocket.InHaleStart();
    }

    public void toFinishState()
    {
        currState = RocketGameManager.RocketState.FINISH;

    }

    public void toNextStage()
    {
        if (transitionInProgress) return;
        transitionInProgress = true;
        switch (currStage)
        {
            case 1:
                SceneManager.LoadScene("1-2. RocketStage2", LoadSceneMode.Single);
                return;
            case 2:
                SceneManager.LoadScene("1-345. RocketStage345", LoadSceneMode.Single);
                return;
            case 3:
                currStage = 4;
                setStage4();
                break;
            case 4:
                currStage = 5;
                setStage5();
                break;
        }
        transitionInProgress = false;
    }

    private void resetStage()
    {
        outtakeTime = intakeTime = clearTime = guideTime = currStateTime = 0f;
        sensorData = prevSensorData = 0f;
        isGuiding = inhaleReady = launchReady = isRocketFlying = isFinishScreen = false;
        nextGameRequested = false;
        guideCount = 1;
        vrUiManager.HideAllRocketGuides();
        rocket.ConfigureStage(500f + currStage * 100f);
        rocket.ResetRocket(); // The only rocket reset for a Stage 3->4 or 4->5 transition.
        rocketView.ResetScoreUI();
        rocketView.ResetUI();
        rocketView.SetStage(currStage);
        vrUiManager.resetFill();
        vrUiManager.ResetOutFill();
        vrUiManager.maxRocketHeight = 500f + currStage * 100f;
        vrUiManager.SetHeightProgress(0f);
        vrUiManager.HideInhaleHud();
        vrUiManager.HideExhaleHud();
        if (selectionStick != null) selectionStick.SetActive(false);
        if (rayCastCam != null) rayCastCam.GetComponent<CamRayCast>().messageSended = false;
        if (soundManager != null) soundManager.PlayMusic();
        currState = RocketState.GUIDE;
    }

    private void setStage4()
    {
        vrUiManager.BlockEye();
        if (stage3Planet != null) stage3Planet.SetActive(false);
        if (stage4Planet != null) stage4Planet.SetActive(true);
        resetStage();
        vrUiManager.UnBlockEye();
    }

    private void setStage5()
    {
        vrUiManager.BlockEye();
        if (stage4Planet != null) stage4Planet.SetActive(false);
        if (stage5Planet != null) stage5Planet.SetActive(true);
        resetStage();
        vrUiManager.UnBlockEye();
    }

    private void OnDestroy()
    {
        //SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
