using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using PulmoTrainer.UI;

public class VRUIManager : MonoBehaviour
{
    public GameObject currGameManager = null;

    public GameObject[] rocketStartGuidePanels;
    public GameObject[] rocketGuidePanels;
    public GameObject[] candleStartGuidePanels;
    public GameObject candleGuidePanel;
    public GameObject[] inhaleStartGuidePanels;
    public GameObject inhaleGuidePanel;
    public GameObject[] tutoStartGuidePanels;
    public GameObject[] testStartGuidePanels;
    public GameObject testGuidePanel;
    public GameObject inhaleBarObj;
    public GameObject exhaleBarObj;

    private GameManager gameManager = null;
    private PulmoStatusVisibility statusVisibility;
    private Coroutine eyeFade;

    public Transform playerTr = null;

    [SerializeField]
    private GameObject hudObj = null;
    public RectTransform inhalehudTr = null;

    [SerializeField]
    private Image fillGuage = null;
    public Image eyeBlocker = null;

    private float inhaled = 0f;
    public float fillAmt = 0f;

    private float exhaled = 0f;
    public float exhaleFillAmt = 0f;
    [SerializeField]
    private Image exhaleFillGuage = null;

    public Image heightProgressImg = null;
    public Text heightTxt = null;
    public RectTransform rocketRect = null;
    public float maxRocketHeight = 700f;

    //Singleton
    public static VRUIManager instance = null;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            //DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this);
            return;
        }

        gameManager = GameManager.instance;

        ConfigureStatusVisibility();
        ResetGuides();
        if (eyeBlocker != null)
        {
            eyeBlocker.color = new Color(0f, 0f, 0f, 0f);
            eyeBlocker.gameObject.SetActive(false);
        }
    }

    //개체 사용되도록 설정됐을 때
    private void OnEnable()
    {
        //씬 매니저의 sceneLoaded 델리게이트에 onSceneLoaded를 추가한다->씬로드마다 onSceneLoaded작동
        SceneManager.sceneLoaded += onSceneLoaded;

    }

    void onSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        switch (scene.name)
        {
            case ("1-1. RocketGame"):
            case ("1-2. RocketStage2"):
            case ("1-345. RocketStage345"):
                currGameManager = GameObject.Find("RocketGameManager");
                break;
            case ("2. CandleBlowing"):
                currGameManager = GameObject.Find("CandleGameManager");
                break;
            case ("3. Inhaler"):
                currGameManager = GameObject.Find("InhaleGameManager");
                break;
            case ("0. InitialBreathTest"):
                currGameManager = GameObject.Find("BreathTestGameManager");
                break;

            default:
                currGameManager = null;
                break;
        }
        //fillGuage = GameObject.Find("InhaleFill").GetComponent<Image>();
        //gameManager = GameManager.instance;

        //hudObj = GameObject.Find("HudCanvas");
        //hudTr = hudObj.GetComponent<RectTransform>();
        //playerTr = GameObject.Find("Main Camera").GetComponent<Transform>();
    }

    private void Start()
    {
        // Bind presentation first: an absent optional gauge must not stop head tracking.
        Camera viewCamera = playerTr != null ? playerTr.GetComponent<Camera>() : null;
        if (viewCamera == null) viewCamera = Camera.main;
        if (viewCamera != null)
        {
            BindHudCanvas(hudObj != null ? hudObj.GetComponent<RectTransform>() : null,
                viewCamera, new Vector3(0.05f, 0f, 0.4f));
            BindHudCanvas(inhalehudTr, viewCamera, new Vector3(0f, 0f, 0.4f));
        }
        else if (hudObj != null || inhalehudTr != null)
        {
            Debug.LogError("VRUIManager: assign the scene Main Camera to Player Tr.", this);
        }

        if (fillGuage == null)
        {
            GameObject inhaleFill = GameObject.Find("InhaleFill");
            if (inhaleFill != null) fillGuage = inhaleFill.GetComponent<Image>();
        }
        if (exhaleFillGuage == null)
        {
            GameObject exhaleFill = GameObject.Find("ExhaleFill");
            if (exhaleFill != null) exhaleFillGuage = exhaleFill.GetComponent<Image>();
        }
        gameManager = GameManager.instance;
    }

    private void ConfigureStatusVisibility()
    {
        if (hudObj == null) return;
        PulmoTrainer.Bluetooth.PulmoBluetoothStatusView view = null;
        foreach (Text text in hudObj.GetComponentsInChildren<Text>(true))
        {
            if (text.name != "ArduinoState") continue;
            view = text.GetComponent<PulmoTrainer.Bluetooth.PulmoBluetoothStatusView>();
            if (view == null) view = text.gameObject.AddComponent<PulmoTrainer.Bluetooth.PulmoBluetoothStatusView>();
            view.Initialize(text);
            break;
        }
        if (view == null) return;
        statusVisibility = GetComponent<PulmoStatusVisibility>();
        if (statusVisibility == null) statusVisibility = gameObject.AddComponent<PulmoStatusVisibility>();
        statusVisibility.ResetForScene(view);
    }

    public void SetScoreVisible(bool visible)
    {
        SetStatusBlocked(PulmoStatusVisibility.Blocker.Score, visible);
    }

    private void SetStatusBlocked(PulmoStatusVisibility.Blocker blocker, bool blocked)
    {
        if (statusVisibility != null) statusVisibility.SetBlocked(blocker, blocked);
    }

    private void BindHudCanvas(RectTransform hud, Camera viewCamera, Vector3 cameraOffset)
    {
        if (hud == null) return;
        Canvas canvas = hud.GetComponentInParent<Canvas>(true);
        if (canvas == null)
        {
            Debug.LogError("VRUIManager: HUD needs a World Space Canvas ancestor.", hud);
            return;
        }

        Canvas root = canvas.rootCanvas;
        var binding = root.GetComponent<PulmoTrainer.UI.PulmoHeadLockedCanvas>();
        if (binding == null)
            binding = root.gameObject.AddComponent<PulmoTrainer.UI.PulmoHeadLockedCanvas>();
        if (!binding.Bind(viewCamera, new Vector3(0f, 0f, 0.4f))) return;

        // Preserve the Canvas hierarchy, layout, scale, and panel visibility.
        // Set the camera-relative pose once; parenting follows XR's final render pose.
        hud.position = viewCamera.transform.TransformPoint(cameraOffset);
        hud.rotation = viewCamera.transform.rotation;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= onSceneLoaded;
    }

    public void SetHeightProgress(float height)
    {
        float progressRatio = (height / maxRocketHeight);
        heightProgressImg.fillAmount = progressRatio;
        heightTxt.text = height.ToString();
        float target = (progressRatio *80f) - 40f;
        rocketRect.localPosition = new Vector3(0f, target, 0f);
    }

    public void inHaleFill(float inputInhale)
    {
        //Debug.Log("filling");
        inhaled += inputInhale;
        fillAmt = inhaled / gameManager.maxIntake;
        fillGuage.fillAmount = fillAmt;
    }

    public void exHaleFill(float inputExhale)
    {
        //Debug.Log("filling");
        if (inputExhale < 0f)
            inputExhale *= -1f;
        exhaled += inputExhale;
        exhaleFillAmt = exhaled / GameManager.instance.maxFev1;
        if (exhaleFillAmt < 0f)
            exhaleFillAmt *= -1f;
        exhaleFillGuage.fillAmount = exhaleFillAmt;
    }

    public void resetFill()
    {
        inhaled = 0f;
        fillAmt = 0f;
        fillGuage.fillAmount = 0f;
    }

    public void ResetOutFill()
    {
        exhaled = 0f;
        exhaleFillAmt = 0f;
        exhaleFillGuage.fillAmount = 0f;
    }

    /// <summary>
    /// 로켓발사 후 로켓 객체의 RocketBehavior 스크립트로부터 메시지 받아 흡기 Hud숨기기
    /// </summary>
    public void HideInhaleHud()
    {
        inhaleBarObj.SetActive(false);
    }

    public void ShowInhaleHud()
    {
        inhaleBarObj.SetActive(true);
    }

    public void HideExhaleHud()
    {
        exhaleBarObj.SetActive(false);
    }

    public void ShowExhaleHud()
    {
        exhaleBarObj.SetActive(true);
    }

    public void BlockEye()
    {
        if (eyeBlocker == null) return;
        if (eyeFade != null) { StopCoroutine(eyeFade); eyeFade = null; }
        SetStatusBlocked(PulmoStatusVisibility.Blocker.EyeBlocker, true);
        eyeBlocker.gameObject.SetActive(true);
        eyeBlocker.color = new Color(0f, 0f, 0f, 1f);
        resetFill();
    }

    public void UnBlockEye()
    {
        if (eyeBlocker == null) return;
        if (eyeFade != null) StopCoroutine(eyeFade);
        eyeFade = StartCoroutine(FadeEyeBlocker());
    }

    private IEnumerator FadeEyeBlocker()
    {
        float alpha = eyeBlocker.color.a;
        while (alpha > 0f)
        {
            alpha = Mathf.Max(0f, alpha - Time.deltaTime);
            eyeBlocker.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }
        eyeBlocker.gameObject.SetActive(false);
        SetStatusBlocked(PulmoStatusVisibility.Blocker.EyeBlocker, false);
        eyeFade = null;
    }

    private static void HidePanels(GameObject[] panels)
    {
        if (panels == null) return;
        foreach (GameObject panel in panels) if (panel != null) panel.SetActive(false);
    }

    private void ResetGuides()
    {
        HidePanels(rocketStartGuidePanels);
        HidePanels(rocketGuidePanels);
        HidePanels(candleStartGuidePanels);
        HidePanels(inhaleStartGuidePanels);
        HidePanels(tutoStartGuidePanels);
        HidePanels(testStartGuidePanels);
        if (candleGuidePanel != null) candleGuidePanel.SetActive(false);
        if (inhaleGuidePanel != null) inhaleGuidePanel.SetActive(false);
        if (testGuidePanel != null) testGuidePanel.SetActive(false);
        SetStatusBlocked(PulmoStatusVisibility.Blocker.Guide, false);
    }

    private void SetGuideVisible(GameObject panel, bool visible)
    {
        if (panel == null) return;
        panel.SetActive(visible);
        SetStatusBlocked(PulmoStatusVisibility.Blocker.Guide, visible);
    }

    private void ShowStartGuide(GameObject[] panels, int index, bool playAudio = true)
    {
        if (panels == null || index < 0 || index >= panels.Length || panels[index] == null) return;
        HidePanels(panels);
        SetGuideVisible(panels[index], true);
        AudioSource audio = panels[index].GetComponent<AudioSource>();
        if (playAudio && audio != null) audio.Play();
    }

    private void HideStartGuide(GameObject[] panels)
    {
        HidePanels(panels);
        SetStatusBlocked(PulmoStatusVisibility.Blocker.Guide, false);
    }

    public void HideAllRocketGuides()
    {
        HidePanels(rocketStartGuidePanels);
        HideStartGuide(rocketGuidePanels);
    }

    public void ShowRocketStartGuide(int guideNum) => ShowStartGuide(rocketStartGuidePanels, guideNum);
    public void HideRocketStartGuide(int guideNum) => HideStartGuide(rocketStartGuidePanels);

    public void ShowGuide(int currStage)
    {
        if (currStage == 1) ShowRocketStartGuide(0);
        else ShowStartGuide(rocketGuidePanels, currStage - 1, false);
    }

    public void HideGuide(int currStage) => HideStartGuide(rocketGuidePanels);
    public void ShowCandleStartGuide(int guideNum) => ShowStartGuide(candleStartGuidePanels, guideNum);
    public void HideCandleStartGuide(int guideNum) => HideStartGuide(candleStartGuidePanels);

    public void ShowCandleGuide(int currStage)
    {
        if (currStage == 1) ShowCandleStartGuide(0);
        else SetGuideVisible(candleGuidePanel, true);
    }

    public void HideCandleGuide() => SetGuideVisible(candleGuidePanel, false);
    public void ShowInhaleStartGuide(int guideNum) => ShowStartGuide(inhaleStartGuidePanels, guideNum);
    public void HideInhaleStartGuide(int guideNum) => HideStartGuide(inhaleStartGuidePanels);

    public void ShowInhaleGuide(int currStage)
    {
        if (currStage == 1) ShowInhaleStartGuide(0);
        else SetGuideVisible(inhaleGuidePanel, true);
    }

    public void HideInhaleGuide() => SetGuideVisible(inhaleGuidePanel, false);
    public void ShowTutoStartGuide(int guideNum) => ShowStartGuide(tutoStartGuidePanels, guideNum);
    public void HideTutoStartGuide(int guideNum) => HideStartGuide(tutoStartGuidePanels);
    public void ShowTestStartGuide(int guideNum) => ShowStartGuide(testStartGuidePanels, guideNum, false);
    public void HideTestGuide(int guideNum) => HideStartGuide(testStartGuidePanels);
}
