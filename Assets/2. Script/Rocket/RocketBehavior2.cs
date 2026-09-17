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
    [Tooltip("Seconds to close 95% of an increase in target speed.")]
    [SerializeField, Min(0.01f)] private float riseResponseSeconds = 0.315f;
    [Tooltip("Seconds to close 95% of a decrease in target speed while blowing.")]
    [SerializeField, Min(0.01f)] private float releaseResponseSeconds = 0.6f;
    [Tooltip("Existing height-budget approach time while blowing; not a finish/settle hold.")]
    [SerializeField, Min(0.01f)] private float arrivalResponseSeconds = 0.4f;
    [SerializeField, Min(0f)] private float breathEndThreshold = 0.2f;
    [Tooltip("Continuous neutral confirmation before locking this stage's single breath.")]
    [SerializeField, Min(0.01f)] private float inputLockNeutralSeconds = 0.12f;
    [Tooltip("Constant visual-only drift after reaching the score height, in world units per second.")]
    [SerializeField, Min(0.01f)] private float postFinishRiseSpeed = 1f;
    private const float ArrivalEpsilon = 0.001f;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private RocketDeformation deformation;
    private bool initialized;
    private bool launchStarted;
    private bool inputLocked;
    private bool neutralInput;
    private bool finishStarted;
    private float totalInhale;
    private float totalExhale;
    private float totalFev1;
    private float exhaleSeconds;
    private float neutralSeconds;
    private float currentRiseSpeed;
    private float targetHeight;
    private float finalGoalHeight;
    private float flightHeight;
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
        deformation = new RocketDeformation(rocketTr);
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
        if (launchStarted || inputLocked || finishStarted) return;
        if (rocketGameManager != null) rocketGameManager.currState = RocketGameManager.RocketState.INHALE;
    }
    public void ReadyForLaunch()
    {
        if (ceiling != null) ceiling.CeilingOpening();
    }
    public void Intake(float sensorInput)
    {
        if (gameManager == null || sensorInput >= 0f || launchStarted || inputLocked || finishStarted ||
            float.IsNaN(sensorInput) || float.IsInfinity(sensorInput)) return;
        totalInhale += sensorInput;
        float ratio = gameManager.maxIntake < 0f ? Mathf.Clamp01(totalInhale / gameManager.maxIntake) : 0f;
        deformation.ApplyInhale(ratio);
        Vector3 anchoredPosition = rocketTr.position;
        anchoredPosition.y = initialPosition.y + deformation.VerticalOffset;
        if (rocketRb != null) rocketRb.position = anchoredPosition;
        rocketTr.position = anchoredPosition;
        SetText(IntakeInput, totalInhale);
    }
    public void startLaunching()
    {
        EnsureInitialized();
        if (launchStarted || inputLocked || finishStarted) return;
        launchStarted = true;
        exhaleSeconds = 0f;
        neutralSeconds = 0f;
        if (effects != null) effects.Boost();
    }

    public void ProcessExhale(float pressure, float deltaTime)
    {
        if (!launchStarted || inputLocked || finishStarted || float.IsNaN(pressure) || float.IsInfinity(pressure) ||
            deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
        // Match BreathTest's frame accumulation; no legacy force/speed multipliers in calibration units.
        float sample = Mathf.Max(0f, pressure);
        totalExhale += sample;
        exhaleSeconds += Mathf.Max(0f, deltaTime);
        if (exhaleSeconds < 1f) totalFev1 += sample;
        SetText(fev1Input, totalFev1);
        SetText(fvcInput, totalExhale);
        neutralInput = Mathf.Abs(pressure) <= Mathf.Max(0f, breathEndThreshold);
        neutralSeconds = neutralInput ? neutralSeconds + deltaTime : 0f;
        if (!neutralInput)
        {
            float intensity = gameManager != null && gameManager.maxExhalePressure > 0f
                ? Mathf.Clamp01(sample / gameManager.maxExhalePressure) : 0f;
            float targetSpeed = Mathf.Max(0.1f, maxRiseSpeed) * intensity;
            float response = targetSpeed > currentRiseSpeed ? riseResponseSeconds : releaseResponseSeconds;
            float blend = 1f - Mathf.Exp(-Mathf.Log(20f) * deltaTime / Mathf.Max(0.01f, response));
            currentRiseSpeed = Mathf.Lerp(currentRiseSpeed, targetSpeed, blend);
        }
        // One brief noise sample is tolerated; a confirmed end latches until ResetRocket.
        if (PerformanceRatio >= 1f || neutralSeconds >= Mathf.Max(0.01f, inputLockNeutralSeconds))
            LockBreathInput();
    }

    private void LockBreathInput()
    {
        if (inputLocked) return;
        inputLocked = true;
        finalGoalHeight = MapHeight(initialPosition.y, targetHeight, PerformanceRatio);
    }

    private void FixedUpdate()
    {
        if (!launchStarted) return;
        float deltaTime = Time.fixedDeltaTime;
        Vector3 next = rocketRb != null ? rocketRb.position : rocketTr.position;
        if (finishStarted)
        {
            currentRiseSpeed = Mathf.Max(0.01f, postFinishRiseSpeed);
            flightHeight += currentRiseSpeed * deltaTime;
        }
        else if (inputLocked)
        {
            AdvanceLockedFlight(deltaTime);
        }
        else
        {
            float goal = MapHeight(initialPosition.y, targetHeight, PerformanceRatio);
            // Preserve the pre-existing active-breath budget limiter. Only the rise response is faster.
            if (!neutralInput)
            {
                float remaining = Mathf.Max(0f, goal - flightHeight);
                currentRiseSpeed = Mathf.Min(currentRiseSpeed,
                    remaining / Mathf.Max(deltaTime, arrivalResponseSeconds));
            }
            flightHeight = Mathf.Min(goal, flightHeight + currentRiseSpeed * deltaTime);
        }
        deformation.RestoreDuringFlight(deltaTime);
        next.y = flightHeight + deformation.VerticalOffset;
        if (rocketRb != null) rocketRb.MovePosition(next);
        else rocketTr.position = next;
    }

    private void AdvanceLockedFlight(float deltaTime)
    {
        float remaining = Mathf.Max(0f, finalGoalHeight - flightHeight);
        float travel = currentRiseSpeed * deltaTime;
        if (travel < remaining)
        {
            flightHeight += travel;
            return;
        }

        // Split the crossing step: earned flight up to the score height, slow drift after it.
        // Switching here prevents high-speed MovePosition steps queuing up while LateUpdate
        // waits for the interpolated pose. Never overwrite the displayed Transform to finish.
        if (remaining > 0f)
        {
            deltaTime = Mathf.Max(0f, deltaTime - remaining / currentRiseSpeed);
            flightHeight += remaining;
        }
        currentRiseSpeed = Mathf.Max(0.01f, postFinishRiseSpeed);
        flightHeight += currentRiseSpeed * deltaTime;
    }

    private void LateUpdate()
    {
        if (!inputLocked || finishStarted) return;
        float physicalHeight = rocketRb != null ? rocketRb.position.y : Height;
        // Wait for MovePosition to be visible, but do not brake to zero or require a settle hold.
        if (flightHeight >= finalGoalHeight && physicalHeight >= finalGoalHeight - ArrivalEpsilon &&
            Height >= finalGoalHeight - ArrivalEpsilon) FinishRocket();
    }

    private void FinishRocket()
    {
        if (finishStarted) return;
        finishStarted = true;
        if (rocketRb != null) rocketRb.useGravity = false;
        currentRiseSpeed = Mathf.Max(0.01f, postFinishRiseSpeed);
        if (playerCtrl != null) playerCtrl.EndFollowing();
        if (effects != null) effects.ResetEffects();
        SetEffect(boostEffect, false);
        SetEffect(startEffect, false);
        SetEffect(endEffect, true);
        // The score and player stop here; FixedUpdate keeps the rocket drifting upwards.
        if (view != null) view.ScoreUI(finalGoalHeight);
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
            rocketRb.interpolation = RigidbodyInterpolation.Interpolate;
            rocketRb.position = initialPosition;
            rocketRb.rotation = initialRotation;
        }
        rocketTr.SetPositionAndRotation(initialPosition, initialRotation);
        deformation.ResetScale();
        totalInhale = totalExhale = totalFev1 = 0f;
        exhaleSeconds = neutralSeconds = currentRiseSpeed = 0f;
        flightHeight = initialPosition.y;
        finalGoalHeight = initialPosition.y;
        launchStarted = inputLocked = finishStarted = neutralInput = false;
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
