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

    [Header("BLE sensor")]
    [SerializeField] private string targetDeviceName = "Breat";
    [Tooltip("Optional exact BLE address. When set, it takes priority over the name.")]
    [SerializeField] private string targetDeviceAddress = "";
    [SerializeField, Min(2f)] private float retryDelaySeconds = 3f;
    [SerializeField, Min(5f)] private float connectionTimeoutSeconds = 15f;

    private enum ConnectionState { Permissions, BluetoothOff, LocationOff, Scanning,
        Connecting, Connected, Retry, Failed, Stopped }
    private ConnectionState connectionState = ConnectionState.Permissions;
    private readonly PulmoBluetoothPermissions permissions = new PulmoBluetoothPermissions();
    private PulmoBluetoothStatusView statusView;
    private float nextAttemptAt;
    private float operationDeadline;
    private float nextPollAt;
    private bool scanPending;
    private bool shuttingDown;
    private bool paused;
    private bool receivedPressure;
    public string ConnectionStatus { get; private set; } = "권한 확인 중";


    private bool checkSerial = false;
    // Legacy serialized flag retained for scene compatibility; connection health always updates.
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
        yield return permissions.RequestAccess();
        if (!permissions.IsGranted)
        {
            SetState(ConnectionState.Permissions, "권한 허용 필요");
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
            BluetoothHelper.ASYNC_EVENTS = false; // All Unity state updates stay on the main thread.
            bluetoothHelperInstance = BluetoothHelper.GetInstance();
            bluetoothHelperInstance.OnConnected += onConnected;
            bluetoothHelperInstance.OnConnectionFailed += OnConnectionFailed;
            bluetoothHelperInstance.OnScanEnded += OnScanEnded;
            bluetoothHelperInstance.OnServiceNotFound += OnServiceNotFound;
            bluetoothHelperInstance.OnCharacteristicNotFound += OnCharacteristicNotFound;
            bluetoothHelperInstance.OnCharacteristicChanged += OnCharacteristicChanged;
            if (!bluetoothHelperInstance.IsBluetoothEnabled())
                bluetoothHelperInstance.EnableBluetooth();
            SetState(ConnectionState.Retry, "센서 검색 준비");
            nextAttemptAt = Time.realtimeSinceStartup;
        }
        catch (Exception ex)
        {
            SetState(ConnectionState.Failed, "블루투스 초기화 실패");
            Debug.LogException(ex, this);
        }
    }

    private void Update()
    {
        if (shuttingDown || paused || Time.realtimeSinceStartup < nextPollAt) return;
        nextPollAt = Time.realtimeSinceStartup + 0.5f;
        try { PollConnection(); }
        catch (Exception ex)
        {
            SetState(ConnectionState.Failed, "블루투스 오류 · 로그 확인");
            Debug.LogException(ex, this);
        }
    }

    private void PollConnection()
    {
        if (!permissions.HasRequiredAccess())
        {
            pressure = 0f;
            SetState(ConnectionState.Permissions, "권한 허용 필요");
            return; // No automatic permission dialog loop.
        }
        if (bluetoothHelperInstance == null)
        {
            if (connectionState != ConnectionState.Failed) InitializeBluetooth();
            return;
        }
        if (!bluetoothHelperInstance.IsBluetoothEnabled())
        {
            pressure = 0f;
            SetState(ConnectionState.BluetoothOff, "블루투스 켜기 필요");
            return;
        }
        if (connectionState == ConnectionState.Connected)
        {
            if (!bluetoothHelperInstance.isConnected())
                ScheduleRetry("연결 끊김 · 재검색 대기");
            return;
        }
        if (connectionState == ConnectionState.Connecting)
        {
            if (Time.realtimeSinceStartup >= operationDeadline)
                ScheduleRetry("연결 시간 초과 · 재검색 대기");
            return;
        }
        if (scanPending)
        {
            if (Time.realtimeSinceStartup >= operationDeadline)
                SetState(ConnectionState.Failed, "검색 응답 없음 · 앱 재실행");
            return; // Never start a second scan while the native scanner is busy.
        }
        if (connectionState == ConnectionState.Failed || connectionState == ConnectionState.Stopped) return;
        if (!permissions.IsLocationServiceEnabled())
        {
            SetState(ConnectionState.LocationOff, "휴대폰 위치 켜기 필요");
            return;
        }
        if (Time.realtimeSinceStartup < nextAttemptAt) return;

        SetState(ConnectionState.Scanning, "센서 검색 중");
        operationDeadline = Time.realtimeSinceStartup + 20f;
        scanPending = bluetoothHelperInstance.ScanNearbyDevices();
        if (!scanPending)
            ScheduleRetry("검색 시작 실패 · 재시도 대기", false);
    }

    private void OnScanEnded(BluetoothHelper helper, LinkedList<BluetoothDevice> devices)
    {
        if (shuttingDown || !scanPending) return;
        scanPending = false;
        if (paused || !permissions.HasRequiredAccess())
        {
            ScheduleRetry("센서 검색 대기", false);
            return;
        }

        var results = devices ?? new LinkedList<BluetoothDevice>();
        foreach (BluetoothDevice device in results)
            Debug.Log("[PulmoBLE] Found name='" + device.DeviceName + "' address=" + device.DeviceAddress, this);

        BluetoothDevice selected;
        bool ambiguous;
        if (!PulmoBluetoothDeviceSelection.TrySelect(results, targetDeviceName, targetDeviceAddress,
            out selected, out ambiguous))
        {
            string status = ambiguous ? "동일 이름 센서 여러 대" : "대상 센서 미발견";
            Debug.LogWarning("[PulmoBLE] " + status + ". Expected name='" + targetDeviceName +
                "', address='" + targetDeviceAddress + "', scanned=" + results.Count, this);
            ScheduleRetry(status + " · 재검색 대기", false);
            return;
        }

        try
        {
            pressureModule = selected;
            helper.setDeviceName(selected.DeviceName);
            helper.setDeviceAddress(selected.DeviceAddress);
            receivedPressure = false;
            SetState(ConnectionState.Connecting, "센서 연결 중");
            operationDeadline = Time.realtimeSinceStartup + Mathf.Max(5f, connectionTimeoutSeconds);
            Debug.Log("[PulmoBLE] Connecting to '" + selected.DeviceName + "' " + selected.DeviceAddress, this);
            helper.Connect();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex, this);
            ScheduleRetry("연결 실패 · 재검색 대기");
        }
    }

    private void OnConnectionFailed(BluetoothHelper helper)
    {
        if (shuttingDown || (connectionState != ConnectionState.Connecting &&
            connectionState != ConnectionState.Connected)) return;
        ScheduleRetry("연결 실패 · 재검색 대기");
    }

    private void ScheduleRetry(string status, bool disconnect = true)
    {
        if (shuttingDown) return;
        // Change state BEFORE Disconnect: the plugin can report failure synchronously.
        SetState(ConnectionState.Retry, status);
        pressure = 0f;
        receivedPressure = false;
        nextAttemptAt = Time.realtimeSinceStartup + Mathf.Max(2f, retryDelaySeconds);
        if (disconnect && bluetoothHelperInstance != null)
        {
            try { bluetoothHelperInstance.Disconnect(); }
            catch (Exception ex) { Debug.LogWarning("[PulmoBLE] Disconnect: " + ex.Message, this); }
        }
    }

    public void onConnected(BluetoothHelper helper)
    {
        if (shuttingDown || connectionState != ConnectionState.Connecting) return;
        SetState(ConnectionState.Connected, "연결됨 · 데이터 대기");
        Debug.Log("[PulmoBLE] Connected. Subscribing to pressure characteristic.", this);
        try
        {
            // Keep the existing sensor protocol and UUIDs.
            pressureService = new BluetoothHelperService("00000000-0000-0000-0000-000000001101");
            pressureLevelChar = new BluetoothHelperCharacteristic("00000000-0000-0000-0000-000000002101");
            pressureService.addCharacteristic(pressureLevelChar);
            helper.Subscribe(pressureService);
            helper.StartListening();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex, this);
            ScheduleRetry("센서 구독 실패 · 재검색 대기");
        }
    }

    private void OnCharacteristicChanged(BluetoothHelper helper, byte[] value,
        BluetoothHelperCharacteristic characteristic)
    {
        if (shuttingDown || connectionState != ConnectionState.Connected || value == null || value.Length != 4)
            return;
        encoder sample = new encoder { byte0 = value[0], byte1 = value[1], byte2 = value[2], byte3 = value[3] };
        if (float.IsNaN(sample.encodedFloat) || float.IsInfinity(sample.encodedFloat)) return;
        readData = sample;
        pressure = sample.encodedFloat;
        if (!receivedPressure)
        {
            receivedPressure = true;
            SetState(ConnectionState.Connected, "연결됨");
            Debug.Log("[PulmoBLE] First valid pressure sample received.", this);
        }
    }

    private void OnServiceNotFound(BluetoothHelper helper, string service)
    {
        Debug.LogError("[PulmoBLE] Missing service: " + service, this);
        SetState(ConnectionState.Failed, "센서 서비스 확인 필요");
    }

    private void OnCharacteristicNotFound(BluetoothHelper helper, string service, string characteristic)
    {
        Debug.LogError("[PulmoBLE] Missing characteristic: " + service + "/" + characteristic, this);
        SetState(ConnectionState.Failed, "센서 데이터 항목 확인 필요");
    }

    private void SetState(ConnectionState state, string status)
    {
        connectionState = state;
        if (state != ConnectionState.Connected) pressure = 0f;
        if (ConnectionStatus == status) return;
        ConnectionStatus = status;
        if (statusView != null) statusView.Show(status);
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
                statusView.Initialize(label, Camera.main);
                statusView.Show(ConnectionStatus);
                return;
            }
        }
    }

    private void OnApplicationPause(bool pause) { paused = pause; }
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
        SetState(ConnectionState.Stopped, "블루투스 종료");
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
