using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using PulmoTrainer.Bluetooth;
using UnityEngine.SceneManagement;
using ArduinoBluetoothAPI;

public class BluetoothManager : MonoBehaviour
{
    [SerializeField]
    private string boudRate = "115200";

    [SerializeField]
    private int timeOut = 110;

    [SerializeField]
    private Text sensorText;
    public Text vrSensorText;

    float pressure = 0.0f;

    private Coroutine sensorCo;

    private readonly PulmoBluetoothPermissions permissions = new PulmoBluetoothPermissions();
    private PulmoBluetoothStatusView statusView;
    private bool shuttingDown;
    private bool receivedPressure;
    private bool wasConnected;
    [SerializeField, Min(0.25f)] private float reconnectDelaySeconds = 3f;
    private bool scanPending;
    private bool connectionPending;
    private bool subscriptionStarted;
    private Coroutine retryCoroutine;
    private float nextConnectionCheck;
    public string ConnectionStatus { get; private set; } = "권한 확인 중";

    private bool checkSerial = false;
    // Legacy serialized flag retained for scene compatibility.
    public bool checkingBLE = true;
    
    private int serialNum = 0;
    string serialNameToConnect = null;

    //In-game
    public GameObject logging = null;
    public GameObject currGameManager = null;
    private float outtakeTime = 0f;

    //인식된 블루투스 인스턴스
    public BluetoothHelper bluetoothHelperInstance = null;
    public BluetoothDevice pressureModule = null;

    //연결된 블루투스 장치에서 구독할 서비스와 캐릭터리스틱
    BluetoothHelperService pressureService = null;
    BluetoothHelperCharacteristic pressureLevelChar = null;

    //읽어온 byte data를 float으로 인코딩할 Union과 비슷한 형식의 FieldOffset 적용한 struct
    [StructLayout(LayoutKind.Explicit)]
    public struct encoder
    {
        [FieldOffset(0)]
        public float encodedFloat;

        [FieldOffset(0)]
        public byte byte0;

        [FieldOffset(1)]
        public byte byte1;

        [FieldOffset(2)]
        public byte byte2;

        [FieldOffset(3)]
        public byte byte3;
    }
    public encoder readData;

    //PC용 데이터 저장 리스트   
    public List<string[]> dataList = new List<string[]>();
    public struct inputSensorData
    {
        public List<string[]> dataList;
        public string savePath;
    }
    inputSensorData forSendingMessage;

    //Singleton
    public static BluetoothManager instance = null;
    private void Awake()
    {
#if UNITY_EDITOR_WIN
        // The editor simulator owns pressure input for this Play session. Skip initialization
        // before subscribing to scene events; a disabled BLE coroutine must not overwrite it.
        if (EditorSimulation.IsRunning)
        {
            enabled = false;
            return;
        }
#endif
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void RestartSensorCoroutine()
    {
        if (sensorCo != null)
        {
            StopCoroutine(sensorCo);
            sensorCo = null;
        }

        if (currGameManager != null)
            sensorCo = StartCoroutine(displaySensorData());
    }

    void OnSceneLoaded (Scene scene, LoadSceneMode mode)
    {
        BindStatusView(scene);
        switch (scene.name)
        {
            case ("0. Tutorial"):
                currGameManager = GameObject.Find("TutorialGameManager");
                RestartSensorCoroutine();
                break;
            case ("0. BreathTest"):
                currGameManager = GameObject.Find("BreathTestGameManager");
                RestartSensorCoroutine();
                break;
            case ("1-1. RocketGame"):
                currGameManager = GameObject.Find("RocketGameManager");
                RestartSensorCoroutine();
                break;
            case ("1-2. RocketStage2"):
                currGameManager = GameObject.Find("RocketGameManager");
                RestartSensorCoroutine();
                //Start();
                break;
            case ("1-345. RocketStage345"):
                currGameManager = GameObject.Find("RocketGameManager");
                RestartSensorCoroutine();
                //Start();
                break;
            case ("2. CandleBlowing"):
                currGameManager = GameObject.Find("CandleGameManager");
                RestartSensorCoroutine();
                break;
            case ("3. Inhaler"):
                currGameManager = GameObject.Find("InhaleGameManager");
                RestartSensorCoroutine();
                break;
            default:
                currGameManager = null;
                break;
        }

    }

    IEnumerator displaySensorData()
    {
        while(true)
        {
            //씬에따른 차이 있어 수정필요
            //sensorText.text = pressure.ToString();
            //vrSensorText.text = pressure.ToString();

            if (currGameManager != null)
                currGameManager.SendMessage("SetsensorData", pressure, SendMessageOptions.DontRequireReceiver);
            //currGameManager.SendMessage("SetsensorData", pressure);
            //호흡데이터저장테스트용
            //dataList.Add(dataToArray(sensorText.text));
            yield return null;
        }
        
    }

    private IEnumerator Start()
    {
        BindStatusView(SceneManager.GetActiveScene());
        logging = GameObject.Find("LoggingManager");
        Debug.Log("[PulmoBLE] Checking permissions.", this);
        yield return permissions.RequestAccess();
        Debug.Log("[PulmoBLE] Permissions granted: " + permissions.IsGranted, this);
        if (!permissions.IsGranted)
        {
            SetStatus("권한 허용 필요");
            yield break;
        }
        InitializeBluetooth();
    }

    private void InitializeBluetooth()
    {
        if (bluetoothHelperInstance != null || shuttingDown) return;
        try
        {
            BluetoothHelper.BLE = true;
            bluetoothHelperInstance = BluetoothHelper.GetInstance();
            bluetoothHelperInstance.OnConnected += onConnected;
            bluetoothHelperInstance.OnConnectionFailed += OnConnectionFailed;
            bluetoothHelperInstance.OnScanEnded += OnScanEnded;
            bluetoothHelperInstance.OnServiceNotFound += OnServiceNotFound;
            bluetoothHelperInstance.OnCharacteristicNotFound += OnCharacteristicNotFound;
            bluetoothHelperInstance.OnCharacteristicChanged += OnCharacteristicChanged;

            if (!bluetoothHelperInstance.IsBluetoothEnabled())
            {
                SetStatus("블루투스 켜기 필요");
                bluetoothHelperInstance.EnableBluetooth();
                StartCoroutine(WaitForBluetoothAndScan());
                return;
            }
            StartDeviceScan();
        }
        catch (Exception ex)
        {
            SetStatus("블루투스 초기화 실패");
            Debug.LogError("[PulmoBLE] " + ex, this);
        }
    }

    private IEnumerator WaitForBluetoothAndScan()
    {
        float deadline = Time.realtimeSinceStartup + 30f;
        while (!shuttingDown && bluetoothHelperInstance != null &&
            !bluetoothHelperInstance.IsBluetoothEnabled() && Time.realtimeSinceStartup < deadline)
            yield return null;

        if (shuttingDown || bluetoothHelperInstance == null) yield break;
        if (bluetoothHelperInstance.IsBluetoothEnabled())
            StartDeviceScan();
        else
            SetStatus("블루투스 켜기 필요 · 앱 재실행");
    }

    private void StartDeviceScan()
    {
        if (shuttingDown || bluetoothHelperInstance == null || scanPending || connectionPending || wasConnected) return;
        SetStatus("센서 검색 중");
        scanPending = true;
        try
        {
            bool started = bluetoothHelperInstance.ScanNearbyDevices();
            if (!started) scanPending = false;
            Debug.Log("[PulmoBLE] Scan started: " + started, this);
            if (!started) SetStatus("검색 시작 실패 · 권한/블루투스/위치 확인");
        }
        catch (Exception ex)
        {
            scanPending = false;
            Debug.LogError("[PulmoBLE] Scan failed: " + ex, this);
            QueueRetry(true);
        }
    }

    private void OnScanEnded(BluetoothHelper helper, LinkedList<BluetoothDevice> devices)
    {
        if (shuttingDown || helper != bluetoothHelperInstance || !scanPending) return;
        scanPending = false;
        Debug.Log("[PulmoBLE] Scan result count: " + (devices == null ? 0 : devices.Count), this);
        if (devices == null || devices.Count == 0)
        {
            QueueRetry(true);
            return;
        }

        try
        {
            // Preserve the working name lookup; do not replace it with a MAC address.
            bluetoothHelperInstance.setDeviceName("Breat");
            BeginConnection();
        }
        catch (Exception ex)
        {
            Debug.LogError("[PulmoBLE] Device lookup failed: " + ex, this);
            QueueRetry(true);
        }
    }

    private void BeginConnection()
    {
        if (shuttingDown || bluetoothHelperInstance == null || connectionPending || wasConnected) return;
        connectionPending = true;
        receivedPressure = false;
        SetStatus("센서 연결 중");
        Debug.Log("[PulmoBLE] Connecting to Breat.", this);
        try { bluetoothHelperInstance.Connect(); }
        catch (Exception ex)
        {
            connectionPending = false;
            Debug.LogError("[PulmoBLE] Connect failed: " + ex, this);
            QueueRetry(true);
        }
    }

    private void OnConnectionFailed(BluetoothHelper helper)
    {
        if (shuttingDown || helper != bluetoothHelperInstance || (!connectionPending && !wasConnected)) return;
        connectionPending = false;
        wasConnected = false;
        subscriptionStarted = false;
        pressure = 0f;
        receivedPressure = false;
        Debug.LogWarning("[PulmoBLE] Connection failed.", this);
        QueueRetry(false);
    }

    private void QueueRetry(bool scanAgain)
    {
        if (shuttingDown || retryCoroutine != null) return;
        SetStatus(scanAgain ? "재검색 대기" : "재연결 대기");
        retryCoroutine = StartCoroutine(RetryAfterDelay(scanAgain));
    }

    private IEnumerator RetryAfterDelay(bool scanAgain)
    {
        // Yield before another native call: even a synchronous failure cannot recurse.
        yield return new WaitForSecondsRealtime(Mathf.Max(0.25f, reconnectDelaySeconds));
        while (!shuttingDown && bluetoothHelperInstance != null &&
            (!permissions.HasRequiredAccess() || !bluetoothHelperInstance.IsBluetoothEnabled()))
            yield return new WaitForSecondsRealtime(1f);
        retryCoroutine = null;
        if (shuttingDown || bluetoothHelperInstance == null || wasConnected) yield break;
        Debug.Log("[PulmoBLE] Reconnect attempt: " + (scanAgain ? "scan then name lookup" : "existing Breat helper"), this);
        if (scanAgain) StartDeviceScan();
        else BeginConnection();
    }

    public void onConnected(BluetoothHelper helper)
    {
        if (shuttingDown || helper != bluetoothHelperInstance || subscriptionStarted) return;
        connectionPending = false;
        scanPending = false;
        subscriptionStarted = true;
        wasConnected = true;
        SetStatus("연결됨 · 데이터 대기");
        Debug.Log("[PulmoBLE] Connected name='" + helper.getDeviceName() +
            "' address=" + helper.getDeviceAddress(), this);

        // Keep the original service, characteristic, and Subscribe -> StartListening order.
        pressureService = new BluetoothHelperService("00000000-0000-0000-0000-000000001101");
        pressureLevelChar = new BluetoothHelperCharacteristic("00000000-0000-0000-0000-000000002101");
        pressureService.addCharacteristic(pressureLevelChar);
        try
        {
            helper.Subscribe(pressureService);
            Debug.Log("[PulmoBLE] Subscribe succeeded: request submitted; waiting for notifications.", this);
        }
        catch (Exception ex)
        {
            SetStatus("연결됨 · 구독 오류");
            Debug.LogError("[PulmoBLE] Subscribe failed. Connection was not reset.", this);
            Debug.LogError("[PulmoBLE] " + ex, this);
            return;
        }

        try
        {
            helper.StartListening();
            Debug.Log("[PulmoBLE] StartListening succeeded.", this);
        }
        catch (Exception ex)
        {
            // A legacy stream-listening error does not prove BLE Subscribe failed.
            // Keep any active notification subscription and report the actual failing stage.
            if (!receivedPressure) SetStatus("연결됨 · 수신 시작 오류");
            Debug.LogError("[PulmoBLE] StartListening failed after Subscribe returned. Connection was not reset.", this);
            Debug.LogError("[PulmoBLE] " + ex, this);
        }
    }

    private void OnCharacteristicChanged(BluetoothHelper helper, byte[] value,
        BluetoothHelperCharacteristic characteristic)
    {
        if (shuttingDown || value == null || value.Length < 4) return;
        // The original protocol reads the first four bytes, including in longer notifications.
        encoder sample = new encoder { byte0 = value[0], byte1 = value[1], byte2 = value[2], byte3 = value[3] };
        if (float.IsNaN(sample.encodedFloat) || float.IsInfinity(sample.encodedFloat)) return;
        readData = sample;
        pressure = sample.encodedFloat;
        if (!receivedPressure)
        {
            receivedPressure = true;
            SetStatus("연결됨");
            Debug.Log("[PulmoBLE] First valid pressure sample received.", this);
        }
    }

    private void OnServiceNotFound(BluetoothHelper helper, string service)
    {
        Debug.LogError("[PulmoBLE] Missing service: " + service, this);
        SetStatus("연결됨 · 센서 서비스 확인 필요");
    }

    private void OnCharacteristicNotFound(BluetoothHelper helper, string service, string characteristic)
    {
        Debug.LogError("[PulmoBLE] Missing characteristic: " + service + "/" + characteristic, this);
        SetStatus("연결됨 · 센서 데이터 항목 확인 필요");
    }

    private void Update()
    {
        // Monitoring belongs to the connection service, independent of status-text visibility.
        if (shuttingDown || bluetoothHelperInstance == null || !wasConnected ||
            Time.realtimeSinceStartup < nextConnectionCheck) return;
        nextConnectionCheck = Time.realtimeSinceStartup + 0.5f;
        if (bluetoothHelperInstance.isConnected()) return;
        wasConnected = false;
        connectionPending = false;
        subscriptionStarted = false;
        receivedPressure = false;
        pressure = 0f;
        Debug.LogWarning("[PulmoBLE] Connection lost.", this);
        QueueRetry(false);
    }

    private void SetStatus(string status)
    {
        if (ConnectionStatus == status) return;
        ConnectionStatus = status;
        if (statusView != null) statusView.SetStatus(status);
        Debug.Log("[PulmoBLE] " + status, this);
    }

    private void BindStatusView(Scene scene)
    {
        statusView = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Text label in root.GetComponentsInChildren<Text>(true))
            {
                if (label.name != "ArduinoState") continue;
                statusView = label.GetComponent<PulmoBluetoothStatusView>();
                if (statusView == null) statusView = label.gameObject.AddComponent<PulmoBluetoothStatusView>();
                statusView.Initialize(label);
                statusView.SetStatus(ConnectionStatus);
                return;
            }
        }
    }

    private void OnApplicationQuit() { QuitBLE(); }
    private void OnDestroy()
    {
        if (instance != this) return;
        QuitBLE();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }

    public void QuitBLE()
    {
        if (shuttingDown) return;
        shuttingDown = true;
        if (retryCoroutine != null) StopCoroutine(retryCoroutine);
        retryCoroutine = null;
        scanPending = false;
        connectionPending = false;
        pressure = 0f;
        SetStatus("블루투스 종료");
        if (bluetoothHelperInstance == null) return;
        bluetoothHelperInstance.OnConnected -= onConnected;
        bluetoothHelperInstance.OnConnectionFailed -= OnConnectionFailed;
        bluetoothHelperInstance.OnScanEnded -= OnScanEnded;
        bluetoothHelperInstance.OnServiceNotFound -= OnServiceNotFound;
        bluetoothHelperInstance.OnCharacteristicNotFound -= OnCharacteristicNotFound;
        bluetoothHelperInstance.OnCharacteristicChanged -= OnCharacteristicChanged;
        try { bluetoothHelperInstance.Disconnect(); }
        catch (Exception ex) { Debug.LogWarning("[PulmoBLE] Shutdown: " + ex.Message, this); }
        bluetoothHelperInstance = null;
    }
}
