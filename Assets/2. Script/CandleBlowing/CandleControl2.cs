using UnityEngine;
using UnityEngine.Serialization;

public class CandleControl2 : MonoBehaviour
{
    [SerializeField] private Transform[] candleFires;
    [SerializeField] private CandleGameManager2 candleGameManager;
    [SerializeField] private GameObject uiManager;
    [SerializeField] private float tiltRatio = 300f;
    [SerializeField, HideInInspector] private float airPerOffcandle = 220f;
    [SerializeField, Range(0f, 1f)] private float minimumEffectiveIntensity = 0f;
    [Tooltip("1 matches the BreathTest/tutorial Exhaled air target: the calibrated mean FEV1 amount.")]
    [FormerlySerializedAs("targetDose")]
    [SerializeField, Min(0.01f)] private float targetExhaledAirRatio = 1f;
    [SerializeField, Min(0.01f)] private float difficultyMultiplier = 1f;
    private float accumulatedExhaledAir;
    private float requiredExhaledAir;
    private int extinguishedCount;
    private Collider[] selectionColliders;
    private bool[] colliderEnabled;
    private FireControl[] flames;
    private Vector3[] initialScales;
    private Quaternion[] initialRotations;
    private Outline outline;
    private bool initialized;
    public bool IsAttemptFinished { get; private set; }
    public bool IsFullyExtinguished => candleFires != null && candleFires.Length > 0 &&
        extinguishedCount >= candleFires.Length;
    public int ExtinguishedCount => extinguishedCount;
    public int candlesForOff => extinguishedCount;
    public float DoseProgress => requiredExhaledAir > 0f
        ? Mathf.Clamp01(accumulatedExhaledAir / requiredExhaledAir) : 0f;

    private void Awake() { EnsureInitialized(); }
    private void EnsureInitialized()
    {
        if (initialized) return;
        if (candleFires == null) candleFires = new Transform[0];
        flames = new FireControl[candleFires.Length];
        initialScales = new Vector3[candleFires.Length];
        initialRotations = new Quaternion[candleFires.Length];
        for (int i = 0; i < candleFires.Length; i++)
        {
            if (candleFires[i] == null) continue;
            flames[i] = candleFires[i].GetComponent<FireControl>();
            initialScales[i] = candleFires[i].localScale;
            initialRotations[i] = candleFires[i].localRotation;
        }
        selectionColliders = GetComponentsInChildren<Collider>(true);
        colliderEnabled = new bool[selectionColliders.Length];
        for (int i = 0; i < selectionColliders.Length; i++) colliderEnabled[i] = selectionColliders[i].enabled;
        outline = GetComponent<Outline>();
        initialized = true;
    }

    public void Intake(float pressure)
    {
        EnsureInitialized();
        if (IsAttemptFinished || pressure >= 0f || GameManager.instance == null) return;
        TiltFire(pressure, GameManager.instance.maxInhalePressure);
    }

    public void ProcessExhale(float pressure, float deltaTime)
    {
        EnsureInitialized();
        if (IsAttemptFinished || !isActiveAndEnabled || GameManager.instance == null ||
            deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) ||
            float.IsNaN(pressure) || float.IsInfinity(pressure)) return;
        if (requiredExhaledAir <= 0f)
        {
            float reference = GameManager.instance.maxFev1;
            if (reference <= 0f || float.IsNaN(reference) || float.IsInfinity(reference)) return;
            requiredExhaledAir = reference * Mathf.Max(0.01f, targetExhaledAirRatio)
                * Mathf.Max(0.01f, difficultyMultiplier);
        }
        float sample = Mathf.Max(0f, pressure);
        float peakPressure = GameManager.instance.maxExhalePressure;
        float intensity = peakPressure > 0f ? Mathf.Clamp01(sample / peakPressure) : 0f;
        if (intensity < Mathf.Clamp01(minimumEffectiveIntensity)) return;
        // Use the same positive per-frame sensor sum as BreathTest and VRUIManager.exHaleFill.
        // Do not normalize by peak pressure or multiply by time: those are different units.
        accumulatedExhaledAir = Mathf.Min(requiredExhaledAir, accumulatedExhaledAir + sample);
        if (accumulatedExhaledAir >= requiredExhaledAir * (1f - 0.000001f))
            accumulatedExhaledAir = requiredExhaledAir;
        TiltFire(pressure, peakPressure);
        int targetCount = Mathf.Min(candleFires.Length,
            Mathf.FloorToInt(DoseProgress * candleFires.Length + 0.00001f));
        while (extinguishedCount < targetCount)
        {
            int index = extinguishedCount++;
            if (flames[index] != null) flames[index].ShrinkAndOff();
            else if (candleFires[index] != null) candleFires[index].gameObject.SetActive(false);
        }
        if (DoseProgress >= 1f && candleFires.Length > 0 && extinguishedCount == candleFires.Length)
        {
            foreach (FireControl flame in flames) if (flame != null) flame.FinishExtinguishing();
            FinishAttempt();
        }
    }

    private void TiltFire(float pressure, float reference)
    {
        if (Mathf.Abs(reference) <= 0.00001f) return;
        float angle = Mathf.Clamp(pressure / reference, -1f, 1f) * -tiltRatio;
        for (int i = extinguishedCount; i < candleFires.Length; i++)
            if (candleFires[i] != null)
                candleFires[i].localRotation = initialRotations[i] * Quaternion.Euler(angle, 0f, 0f);
    }

    public void SetHighlighted(bool highlighted)
    {
        EnsureInitialized();
        if (outline != null) outline.enabled = highlighted && !IsAttemptFinished;
    }

    public void FinishAttempt()
    {
        EnsureInitialized();
        if (IsAttemptFinished) return;
        // Partial and full attempts both lock this set, preserving its earned air amount and flames.
        IsAttemptFinished = true;
        SetHighlighted(false);
        foreach (Collider collider in selectionColliders) if (collider != null) collider.enabled = false;
    }

    public void ResetInteraction()
    {
        EnsureInitialized();
        StopAllCoroutines();
        CancelInvoke();
        accumulatedExhaledAir = requiredExhaledAir = 0f;
        extinguishedCount = 0;
        IsAttemptFinished = false;
        if (outline != null) outline.enabled = false;
        for (int i = 0; i < selectionColliders.Length; i++)
            if (selectionColliders[i] != null) selectionColliders[i].enabled = colliderEnabled[i];
        for (int i = 0; i < candleFires.Length; i++)
        {
            if (candleFires[i] == null) continue;
            if (flames[i] != null) flames[i].ResetFlame();
            candleFires[i].localScale = initialScales[i];
            candleFires[i].localRotation = initialRotations[i];
            candleFires[i].gameObject.SetActive(true);
        }
    }
    public void ResetCandles() { ResetInteraction(); }
}
