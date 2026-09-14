using UnityEngine;

public class CandleControl2 : MonoBehaviour
{
    [SerializeField] private Transform[] candleFires;
    [SerializeField] private CandleGameManager2 candleGameManager;
    [SerializeField] private GameObject uiManager;
    [SerializeField] private float tiltRatio = 300f;
    [SerializeField, HideInInspector] private float airPerOffcandle = 220f;
    [SerializeField, Range(0f, 1f)] private float minimumEffectiveIntensity = 0f;
    [Tooltip("Usability-test dose; tune in the Inspector after the pilot.")]
    [SerializeField, Min(0.01f)] private float targetDose = 1f;
    [SerializeField, Min(0.01f)] private float difficultyMultiplier = 1f;
    private float accumulatedDose;
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
    public float DoseProgress => Mathf.Clamp01(accumulatedDose / RequiredDose);
    private float RequiredDose => Mathf.Max(0.01f, targetDose) * Mathf.Max(0.01f, difficultyMultiplier);

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
        float calibration = GameManager.instance.maxExhalePressure;
        if (calibration <= 0f) return;
        float intensity = Mathf.Clamp01(Mathf.Max(0f, pressure) / calibration);
        if (intensity < Mathf.Clamp01(minimumEffectiveIntensity)) return;
        accumulatedDose = Mathf.Min(RequiredDose, accumulatedDose + intensity * deltaTime);
        if (accumulatedDose >= RequiredDose * (1f - 0.000001f)) accumulatedDose = RequiredDose;
        TiltFire(pressure, calibration);
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
        // Partial and full attempts both lock this set, preserving its earned dose and flames.
        IsAttemptFinished = true;
        SetHighlighted(false);
        foreach (Collider collider in selectionColliders) if (collider != null) collider.enabled = false;
    }

    public void ResetInteraction()
    {
        EnsureInitialized();
        StopAllCoroutines();
        CancelInvoke();
        accumulatedDose = 0f;
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
