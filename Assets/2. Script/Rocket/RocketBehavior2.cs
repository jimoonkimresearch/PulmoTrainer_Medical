using UnityEngine;
using UnityEngine.UI;

public class RocketBehavior2 : MonoBehaviour
{
    [SerializeField] private GameObject EffectCtrl = null;
    [SerializeField] private GameObject Ceiling = null;
    [SerializeField] private Transform rocketTr = null;
    [SerializeField] private Rigidbody rocketRb = null;
    [SerializeField] private GameObject startEffect = null;
    [SerializeField] private GameObject endEffect = null;
    [SerializeField] private GameObject boostEffect = null;
    [SerializeField] private InputField IntakeInput = null;
    [SerializeField] private InputField fev1Input = null;
    [SerializeField] private InputField fvcInput = null;
    [SerializeField] private GameObject UIManager = null;
    [SerializeField] private RawImage rocketCam = null;
    [SerializeField] private Image intakeGuage = null;
    [SerializeField] private GameObject networkManager = null;
    [SerializeField] private RocketGameManager rocketGameManager = null;
    [SerializeField] private GameManager gameManager = null;
    [SerializeField] private RocketPlayerBehavior playerCtrl = null;
    

    [SerializeField, Min(0.1f)] private float maxRiseSpeed = 200f;
    [SerializeField, Min(0.1f)] private float riseAcceleration = 120f;
    [SerializeField, Min(0f)] private float breathEndThreshold = 0.2f;
    [SerializeField, Min(0.01f)] private float breathEndHoldSeconds = 0.35f;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 initialScale;
    private bool initialized;
    private bool launchStarted;
    private bool exhaleEnded;
    private bool finishStarted;
    private float totalInhale;
    private float totalExhale;
    private float totalFev1;
    private float exhaleSeconds;
    private float neutralSeconds;
    private float currentRiseSpeed;
    private float targetHeight;
    private EffectContol effects;
    private OnLaunch ceiling;
    private RocketUIManager view;

    public float Height => rocketTr != null ? rocketTr.position.y : transform.position.y;
    public float PerformanceRatio => gameManager != null && gameManager.maxFvc > 0f
        ? Mathf.Clamp01(totalExhale / gameManager.maxFvc) : 0f;
    public static float MapHeight(float startHeight, float goalHeight, float performance)
        => Mathf.Lerp(startHeight, goalHeight, Mathf.Clamp01(performance));

    private void Awake() { EnsureInitialized(); }
    private void Start()
    {
        gameManager = GameManager.instance;
        rocketGameManager = RocketGameManager.instance;
        if (rocketGameManager != null) ConfigureStage(500f + rocketGameManager.currStage * 100f);
    }

    private void EnsureInitialized()
    {
        if (initialized) return;
        if (rocketTr == null) rocketTr = transform;
        if (rocketRb == null) rocketRb = GetComponent<Rigidbody>();
        initialPosition = rocketTr.position;
        initialRotation = rocketTr.rotation;
        initialScale = rocketTr.localScale;
        effects = EffectCtrl != null ? EffectCtrl.GetComponent<EffectContol>() : null;
        ceiling = Ceiling != null ? Ceiling.GetComponent<OnLaunch>() : null;
        view = UIManager != null ? UIManager.GetComponent<RocketUIManager>() : null;
        initialized = true;
        ResetRocket();
    }

    public void ConfigureStage(float height)
    {
        EnsureInitialized();
        targetHeight = Mathf.Max(initialPosition.y, height);
    }

    public void InHaleStart()
    {
        if (rocketGameManager != null) rocketGameManager.currState = RocketGameManager.RocketState.INHALE;
    }
    public void ReadyForLaunch()
    {
        if (ceiling != null) ceiling.CeilingOpening();
    }
    public void Intake(float sensorInput)
    {
        if (gameManager == null || sensorInput >= 0f || launchStarted) return;
        totalInhale += sensorInput;
        float ratio = gameManager.maxIntake < 0f ? Mathf.Clamp01(totalInhale / gameManager.maxIntake) : 0f;
        rocketTr.localScale = Vector3.Scale(initialScale,
            new Vector3(1f + 0.1f * ratio, 1f - 0.5f * ratio, 1f + 0.1f * ratio));
        SetText(IntakeInput, totalInhale);
    }
    public void startLaunching()
    {
        EnsureInitialized();
        if (launchStarted || finishStarted) return;
        launchStarted = true;
        exhaleSeconds = 0f;
        neutralSeconds = 0f;
        if (effects != null) effects.Boost();
    }

    public void ProcessExhale(float pressure, float deltaTime)
    {
        if (!launchStarted || exhaleEnded || float.IsNaN(pressure) || float.IsInfinity(pressure)) return;
        // Match BreathTest's frame accumulation; no legacy force/speed multipliers in calibration units.
        float sample = Mathf.Max(0f, pressure);
        totalExhale += sample;
        exhaleSeconds += Mathf.Max(0f, deltaTime);
        if (exhaleSeconds < 1f) totalFev1 += sample;
        SetText(fev1Input, totalFev1);
        SetText(fvcInput, totalExhale);
        neutralSeconds = Mathf.Abs(pressure) <= breathEndThreshold
            ? neutralSeconds + Mathf.Max(0f, deltaTime) : 0f;
        exhaleEnded = PerformanceRatio >= 1f || neutralSeconds >= Mathf.Max(0.01f, breathEndHoldSeconds);
    }

    private void FixedUpdate()
    {
        if (!launchStarted || finishStarted) return;
        float goal = MapHeight(initialPosition.y, targetHeight, PerformanceRatio);
        currentRiseSpeed = Mathf.MoveTowards(currentRiseSpeed, Mathf.Max(0.1f, maxRiseSpeed),
            Mathf.Max(0.1f, riseAcceleration) * Time.fixedDeltaTime);
        Vector3 next = initialPosition;
        next.y = Mathf.MoveTowards(Height, goal, currentRiseSpeed * Time.fixedDeltaTime);
        if (rocketRb != null) rocketRb.MovePosition(next);
        else rocketTr.position = next;
        rocketTr.localScale = Vector3.MoveTowards(rocketTr.localScale, initialScale, Time.fixedDeltaTime * 3f);
        // Keep animating after the breath ends so smoothing cannot reduce the achieved score/height.
        if (exhaleEnded && Mathf.Abs(next.y - goal) <= 0.001f) FinishRocket(goal);
    }

    private void FinishRocket(float height)
    {
        if (finishStarted) return;
        finishStarted = true;
        launchStarted = false;
        Vector3 finalPosition = initialPosition;
        finalPosition.y = height;
        if (rocketRb != null) rocketRb.position = finalPosition;
        rocketTr.position = finalPosition;
        rocketTr.localScale = initialScale;
        if (playerCtrl != null) playerCtrl.EndFollowing();
        if (effects != null) effects.ResetEffects();
        SetEffect(boostEffect, false);
        SetEffect(startEffect, false);
        SetEffect(endEffect, true);
        if (view != null) view.ScoreUI(height);
        if (rocketGameManager != null) rocketGameManager.toFinishState();
    }

    public void ResetRocket()
    {
        if (!initialized) { EnsureInitialized(); return; }
        StopAllCoroutines();
        CancelInvoke();
        if (rocketRb != null)
        {
            // Clear stored velocities before returning to controlled kinematic movement.
            rocketRb.isKinematic = false;
            rocketRb.linearVelocity = Vector3.zero;
            rocketRb.angularVelocity = Vector3.zero;
            rocketRb.useGravity = false;
            rocketRb.isKinematic = true;
            rocketRb.position = initialPosition;
            rocketRb.rotation = initialRotation;
        }
        rocketTr.SetPositionAndRotation(initialPosition, initialRotation);
        rocketTr.localScale = initialScale;
        totalInhale = totalExhale = totalFev1 = 0f;
        exhaleSeconds = neutralSeconds = currentRiseSpeed = 0f;
        launchStarted = exhaleEnded = finishStarted = false;
        SetText(IntakeInput, 0f);
        SetText(fev1Input, 0f);
        SetText(fvcInput, 0f);
        if (effects != null) effects.ResetEffects();
        SetEffect(startEffect, false);
        SetEffect(boostEffect, false);
        SetEffect(endEffect, false);
        if (playerCtrl != null) playerCtrl.ResetFollowing();
        gameObject.SetActive(true);
    }

    private static void SetText(InputField field, float value)
    {
        if (field != null) field.text = value.ToString();
    }
    private static void SetEffect(GameObject effect, bool visible)
    {
        if (effect == null) return;
        if (visible) effect.SetActive(true);
        foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (visible) particles.Play(true);
        }
        effect.SetActive(visible);
    }
}
