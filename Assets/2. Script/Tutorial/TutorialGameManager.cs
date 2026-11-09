using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialGameManager : MonoBehaviour, PulmoTrainer.Inputs.IBreathPressureReceiver
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

    void PulmoTrainer.Inputs.IBreathPressureReceiver.SetPressure(float pressure) => SetsensorData(pressure);

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
    [SerializeField, Min(0.01f)] private float breathStartThreshold = 1f;
    [SerializeField, Min(0f)] private float breathEndThreshold = 0.2f;
    [SerializeField, Min(0.01f)] private float breathEndHoldSeconds = 0.35f;
    private float neutralDuration;
    private TutorialObjectCtrl breathingTarget;

    public GameManager gameManager = null;
    public TutorialUIManager uiManager = null;
    public GameObject selectionStick = null;
    public VRUIManager vrUiManager = null;
    public TutorialSoundManager soundManager = null;
    
    [SerializeField] private GameObject currObjSeeing = null;

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
                    
                    if (PulmoInput.PointerReleased)
                    {
                        Debug.Log("터치");
                        if (guideCount == 5)
                        {
                            vrUiManager.HideTutoStartGuide(guideCount);
                            currState2 = GameState2.SEEKINGTARGET;
                        }
                        else if (guideCount >= 1 && guideCount < 5)
                        {
                            vrUiManager.ShowTutoStartGuide(guideCount);
                            guideCount++;
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
                    if (PulmoInput.PointerReleased)
                    {
                        if(playTime>=10f)
                            currState2 = GameState2.FINISH;
                    }
                    break;
                case GameState2.BREATHEREADY:
                    playTime += Time.deltaTime;
                    if (Mathf.Abs(sensorData) >= Mathf.Max(0.01f, breathStartThreshold))
                    {
                        neutralDuration = 0f;
                        breathingTarget = currObjSeeing != null ? currObjSeeing.GetComponent<TutorialObjectCtrl>() : null;
                        currState2 = GameState2.BREATHING;
                    }
                    if (PulmoInput.PointerReleased)
                    {
                        if (playTime >= 10f)
                            currState2 = GameState2.FINISH;
                    }
                    break;
                case GameState2.BREATHING:
                    playTime += Time.deltaTime;
                    float endThreshold = Mathf.Clamp(breathEndThreshold, 0f, Mathf.Max(0.01f, breathStartThreshold) * 0.9f);
                    if (Mathf.Abs(sensorData) <= endThreshold)
                    {
                        neutralDuration += Time.deltaTime;
                        if (neutralDuration >= Mathf.Max(0.01f, breathEndHoldSeconds)) FinishBreath();
                        break;
                    }
                    neutralDuration = 0f;
                    vrUiManager.ShowInhaleHud();
                    uiManager.SetPressureTxt(sensorData);
                    if (sensorData>0f)
                    {
                        vrUiManager.exHaleFill(sensorData);
                        if (breathingTarget != null) breathingTarget.OnExhale(sensorData);
                        if ((sensorData / gameManager.maxInhalePressure) < 0f)
                            uiManager.SetPressureGuage(sensorData / gameManager.maxInhalePressure * -1f);
                        else
                            uiManager.SetPressureGuage(sensorData / gameManager.maxInhalePressure);
                    }
                    else if(sensorData<0f)
                    {
                        vrUiManager.inHaleFill(sensorData);
                        if (breathingTarget != null) breathingTarget.OnInhale(sensorData);
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
                    /*if (PulmoInput.PointerReleased)
                    {
                        Application.Quit();
                    }*/
                    break;
            }
            yield return null;
        }
    }

    private void FinishBreath()
    {
        neutralDuration = 0f;
        intakedAir = 0f;
        outtakedAir = 0f;
        breathingTarget = null;
        vrUiManager.resetFill();
        vrUiManager.ResetOutFill();
        uiManager.SetPressureTxt(0f);
        uiManager.SetPressureGuage(0f);
        currState2 = GameState2.BREATHEREADY;
    }

    private void OnValidate()
    {
        breathStartThreshold = Mathf.Max(0.01f, breathStartThreshold);
        breathEndThreshold = Mathf.Clamp(breathEndThreshold, 0f, breathStartThreshold * 0.9f);
        breathEndHoldSeconds = Mathf.Max(0.01f, breathEndHoldSeconds);
    }

    public void EyesOnBall()
    {
        if (currState2 == GameState2.SEEKINGTARGET)
            currState2 = GameState2.BREATHEREADY;
    }

    public bool TrySelectTarget(TutorialObjectCtrl target)
    {
        if (target == null || !target.isActiveAndEnabled) return false;
        if (currObjSeeing == target.gameObject)
            return currState2 == GameState2.BREATHEREADY || currState2 == GameState2.BREATHING;
        if (currObjSeeing != null || currState2 != GameState2.SEEKINGTARGET) return false;
        currObjSeeing = target.gameObject;
        EyesOnBall();
        return true;
    }

    public void ReleaseTarget(TutorialObjectCtrl target)
    {
        if (target == null || currObjSeeing != target.gameObject) return;
        currObjSeeing = null;
        if (currState2 == GameState2.BREATHEREADY || currState2 == GameState2.BREATHING)
            EyesOffBall();
    }

    public void EyesOffBall()
    {
        currObjSeeing = null;
        neutralDuration = 0f;
        breathingTarget = null;
        vrUiManager.ResetOutFill();
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
