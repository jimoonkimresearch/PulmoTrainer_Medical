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
            Debug.LogException(ex, this);
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
        if (shuttingDown || bluetoothHelperInstance == null) return;
        SetStatus("센서 검색 중");
        bool started = bluetoothHelperInstance.ScanNearbyDevices();
        Debug.Log("[PulmoBLE] Scan started: " + started, this);
        if (!started) SetStatus("검색 시작 실패 · 권한/블루투스/위치 확인");
    }

    private void OnScanEnded(BluetoothHelper helper, LinkedList<BluetoothDevice> devices)
    {
        if (shuttingDown) return;
        Debug.Log("[PulmoBLE] Found " + (devices == null ? 0 : devices.Count), this);
        if (devices == null || devices.Count == 0)
        {
            StartDeviceScan();
            return;
        }

        foreach (BluetoothDevice device in devices)
            Debug.Log("[PulmoBLE] Found name='" + device.DeviceName + "' address=" + device.DeviceAddress, this);

        try
        {
            // Restore the original name-based lookup performed by the plugin.
            // Addresses are logged only; do not override the device name with an address.
            bluetoothHelperInstance.setDeviceName("Breat");
            receivedPressure = false;
            SetStatus("센서 연결 중");
            bluetoothHelperInstance.Connect();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex, this);
            StartDeviceScan();
        }
    }

    private void OnConnectionFailed(BluetoothHelper helper)
    {
        if (shuttingDown) return;
        pressure = 0f;
        receivedPressure = false;
        wasConnected = false;
        SetStatus("연결 실패 · 다시 연결 중");
        // Preserve the original retry on the same helper/name, without Disconnect + scan.
        helper.Connect();
    }

    public void onConnected(BluetoothHelper helper)
    {
        if (shuttingDown) return;
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
        }
        catch (Exception ex)
        {
            SetStatus("연결됨 · 구독 오류");
            Debug.LogError("[PulmoBLE] Subscribe failed. Connection was not reset.", this);
            Debug.LogException(ex, this);
            return;
        }

        try
        {
            helper.StartListening();
        }
        catch (Exception ex)
        {
            // A legacy stream-listening error does not prove BLE Subscribe failed.
            // Keep any active notification subscription and report the actual failing stage.
            if (!receivedPressure) SetStatus("연결됨 · 수신 시작 오류");
            Debug.LogError("[PulmoBLE] StartListening failed after Subscribe returned. Connection was not reset.", this);
            Debug.LogException(ex, this);
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
        // Observe link loss for display only. Do not restart or disconnect a subscribed link.
        if (shuttingDown || bluetoothHelperInstance == null || !wasConnected) return;
        if (bluetoothHelperInstance.isConnected()) return;
        wasConnected = false;
        receivedPressure = false;
        pressure = 0f;
        SetStatus("연결 끊김");
    }

    private void SetStatus(string status)
    {
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
