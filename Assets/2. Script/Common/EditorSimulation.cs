using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-10000)]
public class EditorSimulation : MonoBehaviour
{
    [Header("Editor Simulation")]
    [Tooltip("체크하면 Unity Editor의 Play Mode에서 키보드 입력을 사용하고 BluetoothManager를 자동으로 비활성화합니다.")]
    [SerializeField] private bool isSimulatingOnEditor = true;

    [Tooltip("비워두면 활성화된 BluetoothManager를 자동으로 찾습니다. EditorSimulation과는 별도 GameObject에 두는 것을 권장합니다.")]
    [SerializeField] private GameObject bluetoothManagerObject;

    [Header("Target")]
    [Tooltip("SetsensorData(float)를 가진 현재 씬의 게임 매니저. 비워두면 알려진 씬/오브젝트 이름으로 자동 탐색합니다.")]
    [SerializeField] private GameObject targetGameManager;

    [Header("Keyboard Input")]
    [SerializeField] private KeyCode exhaleKey = KeyCode.UpArrow;
    [SerializeField] private KeyCode inhaleKey = KeyCode.DownArrow;

    [Header("Pressure")]
    [SerializeField] private float maxPressure = 5f;
    [SerializeField] private float pressureChangeSpeed = 5f;
    [SerializeField] private float pressureReturnSpeed = 8f;
    [SerializeField] private float stateTriggerStep = 2.5f;

    private float simulatedPressure;

    private void Awake()
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        if (!isSimulatingOnEditor)
        {
            // 시뮬레이션이 꺼져 있으면 BluetoothManager는 그대로 사용합니다.
            enabled = false;
            return;
        }

        DisableBluetoothManager();
#else
        // Editor 전용 시뮬레이션이 빌드에서 입력을 덮어쓰지 않도록 합니다.
        enabled = false;
#endif
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        ResolveTarget(SceneManager.GetActiveScene());
        SendPressure(0f);
        GameObject.Find("ArduinoState").GetComponent<Text>().text = "시뮬레이션 중";
    }

    private void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        bool exhalePressed = Input.GetKey(exhaleKey);
        bool inhalePressed = Input.GetKey(inhaleKey);

        float targetPressure = 0f;

        if (exhalePressed && !inhalePressed)
        {
            targetPressure = maxPressure;
        }
        else if (inhalePressed && !exhalePressed)
        {
            targetPressure = -maxPressure;
        }
        else if (inhalePressed && exhalePressed)
        {
            Debug.Log("Simulation Input Error: Check input");
        }

        if (Input.GetKeyDown(inhaleKey) && !exhalePressed)
        {
            simulatedPressure = -Mathf.Min(stateTriggerStep, maxPressure);
        }
        else if (Input.GetKeyDown(exhaleKey) && !inhalePressed)
        {
            simulatedPressure = Mathf.Min(stateTriggerStep, maxPressure);
        }

        float changeSpeed = targetPressure == 0f
            ? pressureReturnSpeed
            : pressureChangeSpeed;

        simulatedPressure = Mathf.MoveTowards(
            simulatedPressure,
            targetPressure,
            changeSpeed * Time.deltaTime
        );

        SendPressure(simulatedPressure);
#endif
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        DisableBluetoothManager();
#endif

        simulatedPressure = 0f;
        targetGameManager = null;
        ResolveTarget(scene);
        SendPressure(0f);
    }

    private void DisableBluetoothManager()
    {
        if (bluetoothManagerObject == null)
        {
            BluetoothManager manager = FindObjectOfType<BluetoothManager>();

            if (manager != null)
            {
                bluetoothManagerObject = manager.gameObject;
            }
        }

        if (bluetoothManagerObject == null)
        {
            return;
        }

        // 같은 GameObject를 끄면 EditorSimulation도 함께 꺼지므로,
        // 이 경우에는 BluetoothManager 컴포넌트만 비활성화합니다.
        if (bluetoothManagerObject == gameObject)
        {
            BluetoothManager manager =
                bluetoothManagerObject.GetComponent<BluetoothManager>();

            if (manager != null)
            {
                manager.enabled = false;
            }

            Debug.LogWarning(
                "EditorSimulation: BluetoothManager와 같은 GameObject에 있습니다. " +
                "안정적인 초기화를 위해 두 컴포넌트를 서로 다른 GameObject에 두는 것을 권장합니다."
            );
            return;
        }

        if (bluetoothManagerObject.activeSelf)
        {
            bluetoothManagerObject.SetActive(false);
            Debug.Log("EditorSimulation: BluetoothManager를 비활성화했습니다.");
        }
    }

    private void ResolveTarget(Scene scene)
    {
        // 인스펙터에서 직접 지정한 대상이 있으면 그것을 우선 사용합니다.
        if (targetGameManager != null)
        {
            return;
        }

        string managerName = null;

        switch (scene.name)
        {
            case "0. Tutorial":
                managerName = "TutorialGameManager";
                break;
            case "0. BreathTest":
                managerName = "BreathTestGameManager";
                break;
            case "1-1. RocketGame":
            case "1-2. RocketStage2":
            case "1-345. RocketStage345":
                managerName = "RocketGameManager";
                break;
            case "2. CandleBlowing":
                managerName = "CandleGameManager";
                break;
            case "3. Inhaler":
                managerName = "InhaleGameManager";
                break;
        }

        if (!string.IsNullOrEmpty(managerName))
        {
            targetGameManager = GameObject.Find(managerName);
        }

        if (targetGameManager == null)
        {
            Debug.LogWarning(
                "EditorSimulation: SetsensorData(float)를 받을 게임 매니저를 찾지 못했습니다. " +
                "Target Game Manager에 현재 씬의 매니저 오브젝트를 지정하세요."
            );
        }
    }

    private void SendPressure(float pressure)
    {
        if (targetGameManager == null)
        {
            return;
        }

        targetGameManager.SendMessage(
            "SetsensorData",
            pressure,
            SendMessageOptions.DontRequireReceiver
        );
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

#if UNITY_EDITOR
        simulatedPressure = 0f;
        SendPressure(0f);
#endif
    }
}