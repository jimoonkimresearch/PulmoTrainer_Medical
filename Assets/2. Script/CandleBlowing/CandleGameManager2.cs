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
    private CandleControl2[] interactions;
    private CandleControl2 activeCandle;
    private CandleUIManager candleView;
    private Logging pressureLog;
    private float neutralDuration;
    [SerializeField, Min(0f)] private float breathEndThreshold = 0.2f;
    [SerializeField, Min(0.01f)] private float breathEndHoldSeconds = 0.35f;
    [Tooltip("Safety limit for one set's inhale/exhale attempt, starting at the first inhale.")]
    [SerializeField, Min(1f)] private float attemptTimeoutSeconds = 20f;
    private float attemptElapsed;
    private bool attemptEnding;
    private Coroutine attemptTransition;
    private enum CandleAttemptEndReason { FullyExtinguished, BreathEnded, GazeLost, Timeout }
    private bool IsBreathing => currState == GameState.INHALE || currState == GameState.EXHALE;
    public bool CanTrackCandleGaze => isActiveAndEnabled && interactions != null && !attemptEnding &&
        (currState == GameState.SEEKINGCANDLE || currState == GameState.INHALEREADY || IsBreathing);

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
        candleView = candleUIManager != null ? candleUIManager.GetComponent<CandleUIManager>() : null;
        interactions = new CandleControl2[candleControl.Length];
        for (int i = 0; i < interactions.Length; i++)
            interactions[i] = candleControl[i] != null ? candleControl[i].GetComponent<CandleControl2>() : null;
        if (candleView != null)
        {
            candleView.SetStage(currStage);
            candleView.SetCandleCountVisible(false);
        }
        gameManager = GameManager.instance;
        vrUiManager = VRUIManager.instance;
        loggingManager = GameObject.Find("LoggingManager");
        pressureLog = loggingManager != null ? loggingManager.GetComponent<Logging>() : null;

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
            if (attemptEnding) { yield return null; continue; }
            if (IsBreathing && activeCandle != null)
            {
                attemptElapsed += Time.deltaTime;
                if (attemptElapsed >= Mathf.Max(1f, attemptTimeoutSeconds))
                {
                    EndActiveAttempt(CandleAttemptEndReason.Timeout);
                    yield return null;
                    continue;
                }
            }
            switch (currState)
            {
                case (GameState.GUIDE):
                    VRUIManager.instance.HideInhaleHud();
                    vrUiManager.HideExhaleHud();
                    if (!isGuiding)
                    {
                        if (candleView != null) candleView.SetCandleCountVisible(false);
                        isGuiding = true;
                        vrUiManager.ShowCandleGuide(currStage);
                        rayCastCam.GetComponent<CamRayCast>().ResetFlag();
                    }

                    /*if ((Input.touchCount > 0) || PulmoInput.PointerReleased)
                    {
                        if (currStage == 1)
                        {
                            if (guideCount == 4)
                            {
                                vrUiManager.HideCandleStartGuide(guideCount);

                                candleOffedOnThisStage = 0;
                                currState = GameState.SEEKINGCANDLE;
                            }
                            else if (guideCount >= 0 && guideCount < 4)
                            {
                                vrUiManager.ShowCandleStartGuide(guideCount);
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
                    candleView.ResetUI();*/

                    //터치 없이 가이드 넘어가는 버전
                    guideTime += Time.deltaTime;
                    if (guideTime >= guideTerm || PulmoInput.PointerReleased)
                    {
                        if (currStage == 1)
                        {
                            if (guideCount >= 4)
                            {
                                vrUiManager.HideCandleStartGuide(4);

                                candleOffedOnThisStage = 0;
                                guideTime = 0f;
                                currState = GameState.SEEKINGCANDLE;
                                if (candleView != null) candleView.SetCandleCountVisible(true);
                            }
                            else if (guideCount >= 0 && guideCount < 4)
                            {
                                vrUiManager.ShowCandleStartGuide(guideCount);
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
                            if (candleView != null) candleView.SetCandleCountVisible(true);
                        }
                    }
                    candleView.ResetUI();
                    break;
                case (GameState.SEEKINGCANDLE):
                    //yield return new WaitForSeconds(0.5f);
                    selectionStick.SetActive(true);
                    clearTime += Time.deltaTime;
                    //playerCtrl.SeekingCandle();
                    vrUiManager.resetFill();
                    vrUiManager.ResetOutFill();
                    break;
                case (GameState.INHALEREADY):
                    //if (!inhaleReady)
                    //{
                    //    inhaleReady = true;
                    //    vrUiManager.HideCandleGuide();
                    //}
                    vrUiManager.ShowExhaleHud();
                    vrUiManager.ShowInhaleHud();
                    clearTime += Time.deltaTime;
                    rayCastCam.GetComponent<CamRayCast>().ResetFlag();
                    if (sensorData <= gameManager.sensorActionPotential * -1f)
                    {
                        activeCandle = SelectedCandle();
                        if (!CanSelectCandle(activeCandle))
                        {
                            activeCandle = null;
                            currState = GameState.SEEKINGCANDLE;
                            break;
                        }
                        ResetBreathState();
                        attemptElapsed = 0f;
                        currState = GameState.INHALE;
                    }
                    break;
                case (GameState.INHALE):
                    if (exhaleReady == false)
                    {
                        exhaleReady = true;
                        //candleControl.SendMessage("ReadyForLaunch");
                        pressureLog?.logPressure("Inhale Start");
                    }

                    // A slow pressure ramp is still an exhale; do not require a sudden pressure jump.
                    if (sensorData >= Mathf.Max(0.01f, Mathf.Abs(gameManager.sensorActionPotential)))
                    {
                        currState = GameState.EXHALE;
                    }
                    rayCastCam.GetComponent<CamRayCast>().messageSended = false;
                    pressureLog?.logPressure(sensorData.ToString());
                    clearTime += Time.deltaTime;
                    intakeTime += Time.deltaTime;

                    vrUiManager.inHaleFill(sensorData);
                    if (activeCandle != null) activeCandle.Intake(sensorData);
                    prevSensorData = sensorData;





                    break;
                case (GameState.EXHALE):
                    if (!isExhaling)
                    {
                        //vrUiManager.HideInhaleHud();
                        isExhaling = true;
                        soundManager.StopMusic();
                        soundManager.OnBreatheSound();
                        pressureLog?.logPressure("Exhale Start");
                    }
                    rayCastCam.GetComponent<CamRayCast>().messageSended = false;
                    pressureLog?.logPressure(sensorData.ToString());
                    clearTime += Time.deltaTime;
                    outtakeTime += Time.deltaTime;
                    if (activeCandle == null)
                    {
                        currState = GameState.SEEKINGCANDLE;
                        break;
                    }
                    int previousCount = activeCandle.ExtinguishedCount;
                    activeCandle.ProcessExhale(sensorData, Time.deltaTime);
                    vrUiManager.SetExhaleProgress(activeCandle.DoseProgress);
                    int currentCount = activeCandle.ExtinguishedCount;
                    candleOffedOnThisStage += currentCount - previousCount;
                    if (candleView != null)
                        for (int count = previousCount + 1; count <= currentCount; count++)
                            candleView.GetOffCandleStar(count);
                    float endThreshold = Mathf.Clamp(breathEndThreshold, 0f,
                        Mathf.Max(0.01f, Mathf.Abs(gameManager.sensorActionPotential)) * 0.9f);
                    neutralDuration = Mathf.Abs(sensorData) <= endThreshold
                        ? neutralDuration + Time.deltaTime : 0f;
                    if (activeCandle.IsFullyExtinguished)
                        EndActiveAttempt(CandleAttemptEndReason.FullyExtinguished);
                    else if (neutralDuration >= Mathf.Max(0.01f, breathEndHoldSeconds))
                        EndActiveAttempt(CandleAttemptEndReason.BreathEnded);
                    break;

                case (GameState.FINISH):
                    if (!isFinishScreen)
                    {
                        candleView.ScoreUI(candleOffedOnThisStage);
                        pressureLog?.logClearTime(clearTime.ToString());
                        vrUiManager.HideInhaleHud();
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
                    currStateTime += Time.deltaTime;
                    break;

            }

            yield return null;
        }



    }

    private CandleControl2 SelectedCandle()
    {
        return interactions != null && currCandleSeeing >= 0 && currCandleSeeing < interactions.Length
            ? interactions[currCandleSeeing] : null;
    }

    public bool CanSelectCandle(CandleControl2 candle)
    {
        if (!CanTrackCandleGaze || candle == null || !candle.isActiveAndEnabled || candle.IsAttemptFinished)
            return false;
        int index = System.Array.IndexOf(interactions, candle);
        if (index < 0 || index >= currStage) return false;
        return !IsBreathing || candle == activeCandle;
    }

    public bool TrySelectCandle(CandleControl2 candle)
    {
        if (!CanSelectCandle(candle)) return false;
        if (IsBreathing) return true; // Only the current set can reach here; never switch during a breath.
        currCandleSeeing = System.Array.IndexOf(interactions, candle);
        vrUiManager.SetExhaleProgress(candle.DoseProgress);
        if (candleView != null) candleView.ResetShowStars();
        currState = GameState.INHALEREADY;
        return true;
    }

    public void NotifyCandleGazeLost(CandleControl2 candle)
    {
        if (attemptEnding) return;
        if (IsBreathing)
        {
            if (candle == activeCandle) EndActiveAttempt(CandleAttemptEndReason.GazeLost);
            return;
        }
        if (currState != GameState.INHALEREADY || candle != SelectedCandle()) return;
        currState = GameState.SEEKINGCANDLE;
        inhaleReady = false;
        vrUiManager.HideInhaleHud();
        vrUiManager.HideExhaleHud();
        vrUiManager.resetFill();
    }

    private void EndActiveAttempt(CandleAttemptEndReason reason)
    {
        if (activeCandle == null || attemptEnding) return;
        if (reason == CandleAttemptEndReason.FullyExtinguished && !activeCandle.IsFullyExtinguished) return;
        // Latch before disabling colliders: selector exit callbacks cannot count this attempt twice.
        attemptEnding = true;
        activeCandle.FinishAttempt();
        activeCandle = null;
        blowedCandles++;
        candleOffedOnThisStage = CountExtinguishedCandles();
        ResetBreathState();
        vrUiManager.resetFill();
        attemptTransition = StartCoroutine(AdvanceAfterAttempt());
    }

    private int CountExtinguishedCandles()
    {
        int score = 0;
        for (int i = 0; i < Mathf.Min(currStage, interactions.Length); i++)
            if (interactions[i] != null) score += interactions[i].ExtinguishedCount;
        return score;
    }

    private IEnumerator AdvanceAfterAttempt()
    {
        bool stageFinished = blowedCandles >= currStage;
        yield return new WaitForSeconds(stageFinished ? 1f : 0.5f);
        if (!stageFinished && candleView != null) candleView.ResetShowStars();
        currState = stageFinished ? GameState.FINISH : GameState.SEEKINGCANDLE;
        attemptEnding = false;
        attemptTransition = null;
    }

    private void ResetBreathState()
    {
        outtakeTime = intakeTime = neutralDuration = 0f;
        prevSensorData = 0f;
        inhaleReady = exhaleReady = isExhaling = isExhaled = false;
    }

    public void EyesOnCandle()
    {
        TrySelectCandle(SelectedCandle());
    }

    public void EyesOffCandle()
    {
        NotifyCandleGazeLost(activeCandle != null ? activeCandle : SelectedCandle());
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
        if (candleView != null) candleView.SetCandleCountVisible(false);
        vrUiManager.BlockEye();
        if (attemptTransition != null) StopCoroutine(attemptTransition);
        attemptTransition = null;
        attemptEnding = false;
        attemptElapsed = 0f;
        ResetBreathState();
        activeCandle = null;
        clearTime = guideTime = currStateTime = sensorData = prevSensorData = 0f;
        for (int i = 1; i <= stage; i++)
        {
            if (interactions[i - 1] != null) interactions[i - 1].ResetInteraction();
            candleStages[i - 1].SetActive(true);
        }
        candleOffedOnThisStage = 0;
        blowedCandles = 0;
        candleView.ResetScoreUI();
        vrUiManager.resetFill();
        vrUiManager.ResetOutFill();
        isGuiding = false;
        inhaleReady = false;
        exhaleReady = false;
        isExhaling = false;
        isExhaled = false;
        isFinishScreen = false;
        currState = GameState.GUIDE;
        candleView.ResetUI();
        candleView.SetStage(currStage);
        rayCastCam.GetComponent<CamRayCast>().messageSended = false;
        soundManager.GetComponent<CandleSoundManager>().StopMusic();
        soundManager.GetComponent<CandleSoundManager>().PlayMusic();
        CandleUIManager.instance.ResetShowStars();
        //CandleUIManager.instance.FillShowStar();

        vrUiManager.UnBlockEye();
    }

    
}
