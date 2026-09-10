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
        rocketUIManager.SendMessage("SetStage", currStage);
    }

    private void Start()
    {
        gameManager = GameManager.instance;
        vrUiManager = VRUIManager.instance;
        loggingManager = GameObject.Find("LoggingManager");
        rocketUIManager = GameObject.Find("UIManager");

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
                        vrUiManager.GetComponent<VRUIManager>().ShowGuide(currStage);
                        vrUiManager.SetHeightProgress(0.0f);
                        rocketUIManager.SendMessage("ResetUI");
                    }

                    /*if ((Input.touchCount > 0) || PulmoInput.PointerReleased)
                    {
                        if (currStage == 1)
                        {
                            if (guideCount == 4)
                            {
                                vrUiManager.GetComponent<VRUIManager>().HideRocketStartGuide(guideCount);
                                currState = RocketState.INHALEREADY;
                            }
                            else if (guideCount >= 0 && guideCount < 4)
                            {
                                vrUiManager.GetComponent<VRUIManager>().ShowRocketStartGuide(guideCount);
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
                    guideTime += Time.fixedDeltaTime;
                    if (guideTime >= guideTerm || PulmoInput.PointerReleased)
                    {
                        if (currStage == 1)
                        {
                            if (guideCount >= 4)
                            {
                                vrUiManager.GetComponent<VRUIManager>().HideRocketStartGuide(4);
                                guideTime = 0f;
                                currState = RocketState.INHALEREADY;
                            }
                            else if (guideCount >= 0 && guideCount < 4)
                            {
                                Debug.Log(guideCount);
                                vrUiManager.GetComponent<VRUIManager>().ShowRocketStartGuide(guideCount);
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
                    clearTime += Time.fixedDeltaTime;
                    rayCastCam.GetComponent<CamRayCast>().messageSended = false;

                    if (sensorData <= gameManager.sensorActionPotential * -1f && (sensorData - prevSensorData) <= -2f)
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
                            rocketControl.SendMessage("ReadyForLaunch");
                        }
                        loggingManager.SendMessage("logPressure", "Inhale Start");
                    }
                    rayCastCam.GetComponent<CamRayCast>().messageSended = false;
                    loggingManager.SendMessage("logPressure", sensorData.ToString());
                    clearTime += Time.fixedDeltaTime;
                    intakeTime += Time.fixedDeltaTime;

                    vrUiManager.SendMessage("inHaleFill", sensorData);
                    rocketControl.SendMessage("Intake", sensorData);

                    if (sensorData > gameManager.sensorActionPotential && (sensorData - prevSensorData) >= 2f)
                    {
                        currState = RocketState.EXHALE;
                    }
                    prevSensorData = sensorData;




                    break;
                case (RocketState.EXHALE):
                    if (!isRocketFlying)
                    {
                        isRocketFlying = true;
                        rocketControl.SendMessage("startLaunching");
                        soundManager.StopMusic();
                        soundManager.SendMessage("OnLaunchSound");
                        loggingManager.SendMessage("logPressure", "Exhale Start");
                    }
                    vrUiManager.exHaleFill(sensorData);
                    rayCastCam.GetComponent<CamRayCast>().messageSended = false;
                    loggingManager.GetComponent<Logging>().logPressure(sensorData.ToString());
                    clearTime += Time.fixedDeltaTime;
                    outtakeTime += Time.fixedDeltaTime;
                    vrUiManager.SetHeightProgress(rocketControl.GetComponent<Transform>().position.y);

                    if (outtakeTime >= 1f && sensorData > gameManager.sensorActionPotential)
                    {
                        rocketControl.SendMessage("FvcOuttake", sensorData);
                        //vrUiManager.HideExhaleHud();
                        //vrUiManager.HideInhaleHud();
                    }
                    else if (outtakeTime < 1f)
                    {
                        rocketControl.SendMessage("Fev1Outtake", sensorData);
                    }




                    break;

                case (RocketState.FINISH):
                    selectionStick.SetActive(false);
                    if (!isFinishScreen)
                    {
                        vrUiManager.HideExhaleHud();
                        vrUiManager.HideInhaleHud();
                        soundManager.SendMessage("ScoreBoardSound");
                        loggingManager.SendMessage("logClearTime", clearTime.ToString());
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
                            GameManager.instance.SimulNextScene();
                        }
                        else
                        {
                            Debug.Log("다음씬 함수 시작 ");
                            toNextStage();
                            break;
                        }

                    }
                    currStateTime += Time.fixedDeltaTime;
                    break;

            }

            yield return null;
        }



    }


    void InhaleFinished()
    {
        currState = RocketState.EXHALE;
        rocketControl.SendMessage("startLaunching");
    }

    public void SensorStart()
    {
        rocketControl.SendMessage("InHaleStart");
    }

    public void toFinishState()
    {
        currState = RocketGameManager.RocketState.FINISH;

    }

    public void toNextStage()
    {
        switch (currStage)
        {
            case 1:
                SceneManager.LoadScene("1-2. RocketStage2", LoadSceneMode.Single);
                break;
            case 2:
                SceneManager.LoadScene("1-345. RocketStage345", LoadSceneMode.Single);
                break;
            case 3:
                currStage = 4;
                setStage4();
                currState = RocketState.GUIDE;
                break;
            case 4:
                currStage = 5;
                setStage5();
                currState = RocketState.GUIDE;
                break;
        }
    }

        
    

    private void resetStage()
    {
        rocketControl.gameObject.SetActive(true);
        rocketControl.SendMessage("ResetRocket");
        rocketUIManager.SendMessage("ResetScoreUI");
    }

    void setStage4()
    {
        vrUiManager.BlockEye();                            
        rocketUIManager.SendMessage("ResetUI");
        stage3Planet.gameObject.SetActive(false);
        stage4Planet.gameObject.SetActive(true);
        rocketControl.SendMessage("ResetRocket");
        vrUiManager.resetFill();
        vrUiManager.ResetOutFill();
        resetStage();
        isGuiding = false;
        inhaleReady = false;
        launchReady = false;
        isRocketFlying = false;
        isFinishScreen = false;
        rocketUIManager.SendMessage("SetStage", currStage);
        rayCastCam.GetComponent<CamRayCast>().messageSended = false;
        soundManager.GetComponent<RocketSoundManager>().PlayMusic();
        vrUiManager.UnBlockEye();                          
    }

    void setStage5()
    {
        vrUiManager.BlockEye();
        rocketUIManager.SendMessage("ResetUI");
        stage4Planet.gameObject.SetActive(false);
        stage5Planet.gameObject.SetActive(true);
        rocketControl.SendMessage("ResetRocket");
        vrUiManager.resetFill();
        vrUiManager.ResetOutFill();
        resetStage();
        isGuiding = false;
        inhaleReady = false;
        launchReady = false;
        isRocketFlying = false;
        isFinishScreen = false;
        rocketUIManager.SendMessage("SetStage", currStage);
        soundManager.GetComponent<RocketSoundManager>().PlayMusic();
        vrUiManager.UnBlockEye();
    }

    private void OnDestroy()
    {
        //SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
