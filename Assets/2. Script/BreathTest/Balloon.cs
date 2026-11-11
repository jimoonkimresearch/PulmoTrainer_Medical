using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>풍선 측정 표시. 크기와 이동을 단일 경로로 제어한다.</summary>
[RequireComponent(typeof(Rigidbody))]
public class Balloon : MonoBehaviour
{
    private AudioSource balloonSoundSource;
    [SerializeField] private AudioClip inflation = null;
    [SerializeField] private AudioClip deflation = null;
    [SerializeField] private AudioClip flying = null;
    [SerializeField] private AudioClip popping = null;
    [SerializeField] private float deformationRatio = 0.1f;

    public float maxAltitude = 50f;
    public Text IntakeInput = null;
    public Text fev1Input = null;
    public Text fvcInput = null;
    public GameObject EffectCtrl = null;
    public GameObject endEffect = null;
    public GameObject boostEffect = null;
    public BreathTestGameManager testGameManager = null;
    public GameObject UIManager = null;
    public GameManager gameManager = null;
    public float speedOffset = 100f;

    [Header("Flight tuning (Unity units / second)")]
    [Min(0f)] public float pressureRiseSpeed = 2f;
    [Min(0f)] public float maxRiseSpeed = 5f;
    [Min(0.01f)] public float riseAcceleration = 3f;
    [Min(0f)] public float forwardSpeed = 0.6f;
    [Min(0f)] public float finishEffectSeconds = 1f;

    private Rigidbody thisRb;
    private Transform thisTr;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;
    private Vector3 initialLocalScale;
    private Vector3 exhaleStartScale;
    private float totalInhale;
    private float totalFEV1;
    private float totalFVC;
    private float launchRatio;
    private float currentRiseSpeed;
    private float pressureRatio;
    private float sealedRiseSpeed;
    private bool breathInputClosed;
    private bool initialized;
    private bool exhaleScaleCaptured;
    private bool isFlying;
    private bool isFinishing;
    private BreathTestUIManager view;
    public bool HasReachedFlightLimit => transform.localPosition.y >= maxAltitude;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void Start()
    {
        ResolveManagers();
    }

    private void EnsureInitialized()
    {
        if (initialized) return;
        thisTr = transform;
        thisRb = GetComponent<Rigidbody>();
        balloonSoundSource = GetComponent<AudioSource>();
        view = UIManager != null ? UIManager.GetComponent<BreathTestUIManager>() : null;
        initialLocalPosition = thisTr.localPosition;
        initialLocalRotation = thisTr.localRotation;
        initialLocalScale = thisTr.localScale;
        exhaleStartScale = initialLocalScale;
        // Collider remains available for ray selection. The balloon itself is
        // script-driven, so floor contacts cannot eject it when its scale changes.
        if (!thisRb.isKinematic)
        {
#if UNITY_6000_0_OR_NEWER
            thisRb.linearVelocity = Vector3.zero;
#else
            thisRb.velocity = Vector3.zero;
#endif
            thisRb.angularVelocity = Vector3.zero;
        }
        thisRb.useGravity = false;
        thisRb.isKinematic = true;
        initialized = true;
    }

    private void ResolveManagers()
    {
        if (testGameManager == null) testGameManager = BreathTestGameManager.instance;
        if (gameManager == null) gameManager = GameManager.instance;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static float DisplayRatio(float value, float reference)
    {
        if (!IsFinite(value) || !IsFinite(reference) || Mathf.Abs(reference) < 0.0001f)
            return 0f;
        // Clamp visual response only; measurement totals are not clamped.
        return Mathf.Clamp01(Mathf.Abs(value) / Mathf.Abs(reference));
    }

    public void InHaleStart()
    {
        ResolveManagers();
        if (testGameManager != null && !isFinishing)
            testGameManager.SensorStart();
    }

    public void ReadyForLaunch()
    {
        EnsureInitialized();
        ResolveManagers();
    }

    public void Intake(float sensorInput)
    {
        ResolveManagers();
        if (breathInputClosed || isFinishing || gameManager == null || !IsFinite(sensorInput) ||
            sensorInput > -gameManager.sensorActionPotential) return;
        totalInhale += sensorInput;
        if (IntakeInput != null) IntakeInput.text = totalInhale.ToString();
        float ratio = DisplayRatio(totalInhale, gameManager.maxIntake);
        thisTr.localScale = initialLocalScale *
            Mathf.Max(0.05f, 1f - ratio * deformationRatio);
    }

    public void Fev1Outtake(float sensorInput)
    {
        ResolveManagers();
        if (breathInputClosed || isFinishing || gameManager == null || !IsFinite(sensorInput) ||
            sensorInput < gameManager.sensorActionPotential) return;
        if (!exhaleScaleCaptured)
        {
            exhaleStartScale = thisTr.localScale;
            exhaleScaleCaptured = true;
        }
        totalFEV1 += sensorInput;
        totalFVC += sensorInput;
        if (fev1Input != null) fev1Input.text = totalFEV1.ToString();
        if (fvcInput != null) fvcInput.text = totalFVC.ToString();
        float ratio = DisplayRatio(totalFEV1, gameManager.maxFev1);
        // A fixed baseline avoids repeatedly adding the accumulated ratio.
        thisTr.localScale = exhaleStartScale + initialLocalScale *
            (Mathf.Max(0f, deformationRatio) * ratio * 1.2f);
    }

    public void FvcOuttake(float sensorInput)
    {
        if (breathInputClosed || isFinishing || !IsFinite(sensorInput) || sensorInput <= 0f) return;
        totalFVC += sensorInput;
        if (fvcInput != null) fvcInput.text = totalFVC.ToString();
        // Movement is performed only by FixedUpdate, never here.
    }

    public void startLaunching()
    {
        ResolveManagers();
        if (isFlying || isFinishing || gameManager == null) return;
        launchRatio = DisplayRatio(totalFEV1, gameManager.maxFev1);
        currentRiseSpeed = 0f;
        isFlying = true;
        PlayEffect(boostEffect);
    }

    public void SetFlightPressure(float pressure)
    {
        if (breathInputClosed || isFinishing || gameManager == null) return;
        pressureRatio = IsFinite(pressure) && pressure > gameManager.sensorActionPotential
            ? DisplayRatio(pressure, gameManager.maxExhalePressure) : 0f;
    }

    public void SealBreathInput()
    {
        EnsureInitialized();
        if (breathInputClosed || isFinishing) return;
        // A short breath may end before the usual one-second launch point.
        startLaunching();
        sealedRiseSpeed = Mathf.Clamp(Mathf.Max(currentRiseSpeed, TargetRiseSpeed()),
            0f, Mathf.Max(0f, maxRiseSpeed));
        breathInputClosed = true;
    }

    private float TargetRiseSpeed()
    {
        // Old BasicSpeed at 60 fps: speedOffset / 1000 * 60 = speedOffset * .06.
        return Mathf.Clamp(
            launchRatio * Mathf.Max(0f, speedOffset) * 0.06f +
            pressureRatio * Mathf.Max(0f, pressureRiseSpeed),
            0f, Mathf.Max(0f, maxRiseSpeed));
    }

    private void FixedUpdate()
    {
        if (!isFlying || isFinishing) return;
        float targetSpeed = breathInputClosed ? sealedRiseSpeed : TargetRiseSpeed();
        currentRiseSpeed = Mathf.MoveTowards(currentRiseSpeed, targetSpeed,
            Mathf.Max(0.01f, riseAcceleration) * Time.fixedDeltaTime);
        Vector3 localDelta = new Vector3(0f, currentRiseSpeed, Mathf.Max(0f, forwardSpeed))
            * Time.fixedDeltaTime;
        Vector3 worldDelta = thisTr.parent != null
            ? thisTr.parent.TransformVector(localDelta) : localDelta;
        thisRb.MovePosition(thisRb.position + worldDelta);
    }

    public void CompleteFlight()
    {
        EnsureInitialized();
        if (isFinishing) return;
        isFinishing = true;
        isFlying = false;
        currentRiseSpeed = 0f;
        if (isActiveAndEnabled) StartCoroutine(FinishBalloon());
        else if (view != null) view.ScoreUI();
    }

    private IEnumerator FinishBalloon()
    {
        StopEffect(boostEffect);
        if (balloonSoundSource != null) balloonSoundSource.Stop();
        if (view != null) view.ScoreUI();
        PlayEffect(endEffect);
        yield return new WaitForSeconds(Mathf.Max(0f, finishEffectSeconds));
        StopEffect(endEffect);
        gameObject.SetActive(false);
    }

    public void ResetBalloon()
    {
        EnsureInitialized();
        StopAllCoroutines();
        isFlying = false;
        isFinishing = false;
        exhaleScaleCaptured = false;
        breathInputClosed = false;
        totalInhale = totalFEV1 = totalFVC = 0f;
        launchRatio = currentRiseSpeed = 0f;
        pressureRatio = sealedRiseSpeed = 0f;
        StopEffect(boostEffect);
        StopEffect(endEffect);
        if (balloonSoundSource != null) balloonSoundSource.Stop();
        thisRb.useGravity = false;
        thisRb.isKinematic = true;
        thisTr.localPosition = initialLocalPosition;
        thisTr.localRotation = initialLocalRotation;
        thisTr.localScale = initialLocalScale;
        thisRb.position = thisTr.position;
        thisRb.rotation = thisTr.rotation;
        exhaleStartScale = initialLocalScale;
        if (IntakeInput != null) IntakeInput.text = "0";
        if (fev1Input != null) fev1Input.text = "0";
        if (fvcInput != null) fvcInput.text = "0";
        gameObject.SetActive(true);
    }

    // Keep existing message entry points for other scene/inspector references.
    public void OnExhale(float pressure)
    {
        ResolveManagers();
        if (gameManager == null || breathInputClosed || isFinishing) return;
        PlaySound(inflation);
        thisTr.localScale = initialLocalScale *
            (1f + DisplayRatio(pressure, gameManager.maxExhalePressure) * Mathf.Max(0f, deformationRatio));
    }

    public void OnInhale(float pressure)
    {
        ResolveManagers();
        if (gameManager == null || breathInputClosed || isFinishing) return;
        PlaySound(deflation);
        thisTr.localScale = initialLocalScale * Mathf.Max(0.05f,
            1f - DisplayRatio(pressure, gameManager.maxInhalePressure) * deformationRatio);
    }

    private void PlaySound(AudioClip clip)
    {
        if (balloonSoundSource == null || clip == null) return;
        if (balloonSoundSource.isPlaying && balloonSoundSource.clip == clip) return;
        balloonSoundSource.clip = clip;
        balloonSoundSource.Play();
    }

    private static void PlayEffect(GameObject effect)
    {
        if (effect == null) return;
        effect.SetActive(true);
        foreach (ParticleSystem particle in effect.GetComponentsInChildren<ParticleSystem>(true))
            particle.Play(false);
    }

    private static void StopEffect(GameObject effect)
    {
        if (effect == null) return;
        foreach (ParticleSystem particle in effect.GetComponentsInChildren<ParticleSystem>(true))
            particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        effect.SetActive(false);
    }

    private void OnDisable()
    {
        isFlying = false;
        currentRiseSpeed = 0f;
        StopAllCoroutines();
        if (balloonSoundSource != null) balloonSoundSource.Stop();
    }
}
