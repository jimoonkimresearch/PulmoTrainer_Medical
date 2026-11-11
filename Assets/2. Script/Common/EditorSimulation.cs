using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PulmoTrainer.Bluetooth;
using PulmoTrainer.Inputs;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-10000)]
public sealed class EditorSimulation : MonoBehaviour
{
    [Header("Windows Editor Simulation")]
    [Tooltip("Windows Editor의 Play Mode에서만 동작합니다. 다른 씬을 직접 실행해도 자동 생성되며 Android에서는 실행하지 않습니다.")]
    [SerializeField] private bool isSimulatingOnEditor = true;

    [Header("Target")]
    [Tooltip("비워두면 현재 씬의 활성 IBreathPressureReceiver를 연결합니다. 씬 전환 시 다시 연결합니다.")]
    [SerializeField] private GameObject targetGameManager;

    [Header("Keyboard Input")]
    [SerializeField] private KeyCode exhaleKey = KeyCode.UpArrow;
    [SerializeField] private KeyCode inhaleKey = KeyCode.DownArrow;

    [Header("Pressure")]
    [SerializeField, Min(0f)] private float maxPressure = 5f;
    [SerializeField, Min(0f)] private float pressureChangeSpeed = 5f;
    [SerializeField, Min(0f)] private float pressureReturnSpeed = 8f;
    [SerializeField, Min(0f)] private float stateTriggerStep = 2.5f;

    private void Awake()
    {
#if UNITY_EDITOR_WIN
        ClaimSession();
#else
        // No bootstrap, keyboard input, scene callbacks, or BLE suppression in a player build.
        enabled = false;
#endif
    }

#if UNITY_EDITOR_WIN
    private static EditorSimulation instance;
    private IBreathPressureReceiver receiver;
    private MonoBehaviour receiverOwner;
    private float simulatedPressure;
    private bool waitingForKeyRelease;

    // Read-only signal used before BluetoothManager initializes its editor connection path.
    public static bool IsRunning => instance != null && instance.enabled && instance.isSimulatingOnEditor;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPlaySession()
    {
        // Also runs when Enter Play Mode has domain reload disabled.
        if (instance != null) instance.UnsubscribeSceneEvents();
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        // Scene objects already exist here, but Awake has not run; include inactive objects
        // in the lookup and inspect the serialized hierarchy flags to reuse the scene copy.
        foreach (EditorSimulation simulation in FindObjectsByType<EditorSimulation>(
            FindObjectsInactive.Include, FindObjectsSortMode.InstanceID))
        {
            if (!simulation.enabled || !simulation.gameObject.scene.IsValid() ||
                !IsHierarchyEnabled(simulation.transform)) continue;
            simulation.ClaimSession();
            return;
        }
        new GameObject(nameof(EditorSimulation)).AddComponent<EditorSimulation>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BindInitialScene()
    {
        // Explicit binding also covers Play Mode with scene reload disabled.
        if (IsRunning) instance.BindScene(SceneManager.GetActiveScene());
    }

    private static bool IsHierarchyEnabled(Transform current)
    {
        while (current != null)
        {
            if (!current.gameObject.activeSelf) return false;
            current = current.parent;
        }
        return true;
    }

    private void ClaimSession()
    {
        if (!isSimulatingOnEditor)
        {
            enabled = false;
            return;
        }
        if (instance != null && instance != this)
        {
            enabled = false;
            // A copied standalone simulator can be removed as a whole; preserve unrelated
            // components/children if somebody attached the script to a shared object.
            if (transform.childCount == 0 && GetComponents<Component>().Length == 2)
                Destroy(gameObject);
            else Destroy(this);
            return;
        }
        instance = this;
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        // Remove before adding so Awake/OnEnable and reload-disabled Play cannot double-subscribe.
        UnsubscribeSceneEvents();
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void OnEnable()
    {
        ClaimSession();
        if (instance == this) BindScene(SceneManager.GetActiveScene());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single || scene == SceneManager.GetActiveScene()) BindScene(scene);
    }

    private void OnActiveSceneChanged(Scene previous, Scene next) { BindScene(next); }

    private void BindScene(Scene scene)
    {
        ResetPressure();
        receiver = null;
        receiverOwner = null;
        if (!scene.IsValid() || !scene.isLoaded) return;

        if (targetGameManager != null && targetGameManager.scene == scene)
            foreach (MonoBehaviour behaviour in targetGameManager.GetComponents<MonoBehaviour>())
                if (TryBindReceiver(behaviour)) break;

        // Resolve only on scene events, never in Update. Login/menu scenes need no receiver.
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (receiver == null)
                foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                    if (TryBindReceiver(behaviour)) break;

            foreach (Text label in root.GetComponentsInChildren<Text>(true))
            {
                if (label.name != "ArduinoState") continue;
                PulmoBluetoothStatusView view = label.GetComponent<PulmoBluetoothStatusView>();
                if (view == null) view = label.gameObject.AddComponent<PulmoBluetoothStatusView>();
                view.Initialize(label);
                view.SetStatus("시뮬레이션 중");
                // Visibility and layout remain owned by the existing UI code.
            }
        }
        targetGameManager = receiverOwner != null ? receiverOwner.gameObject : null;
        SendPressure(0f);
    }

    private bool TryBindReceiver(MonoBehaviour behaviour)
    {
        if (behaviour == null || !behaviour.isActiveAndEnabled ||
            !(behaviour is IBreathPressureReceiver target)) return false;
        receiver = target;
        receiverOwner = behaviour;
        return true;
    }

    private void Update()
    {
        if (!isSimulatingOnEditor)
        {
            ResetPressure();
            return;
        }
        bool exhalePressed = PulmoInput.KeyHeld(exhaleKey);
        bool inhalePressed = PulmoInput.KeyHeld(inhaleKey);
        // A held key must not carry the previous scene's breath into the next scene.
        if (waitingForKeyRelease)
        {
            waitingForKeyRelease = exhalePressed || inhalePressed;
            SendPressure(0f);
            return;
        }

        float limit = Mathf.Max(0f, maxPressure);
        float targetPressure = exhalePressed == inhalePressed ? 0f : (exhalePressed ? limit : -limit);
        if (PulmoInput.KeyPressed(inhaleKey) && !exhalePressed)
            simulatedPressure = -Mathf.Min(Mathf.Max(0f, stateTriggerStep), limit);
        else if (PulmoInput.KeyPressed(exhaleKey) && !inhalePressed)
            simulatedPressure = Mathf.Min(Mathf.Max(0f, stateTriggerStep), limit);

        float speed = targetPressure == 0f ? pressureReturnSpeed : pressureChangeSpeed;
        simulatedPressure = Mathf.MoveTowards(simulatedPressure, targetPressure,
            Mathf.Max(0f, speed) * Time.deltaTime);
        SendPressure(simulatedPressure);
    }

    private void SendPressure(float pressure)
    {
        // Keep the Unity object alongside the interface to respect destroyed/disabled targets.
        if (receiverOwner != null && receiverOwner.isActiveAndEnabled && receiver != null)
            receiver.SetPressure(pressure);
    }

    private void ResetPressure()
    {
        simulatedPressure = 0f;
        waitingForKeyRelease = true;
        SendPressure(0f);
    }

    private void OnApplicationFocus(bool focused) { ResetPressure(); }

    private void UnsubscribeSceneEvents()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void OnDisable()
    {
        UnsubscribeSceneEvents();
        ResetPressure();
        receiver = null;
        receiverOwner = null;
        if (instance == this) instance = null;
    }
#endif
}
