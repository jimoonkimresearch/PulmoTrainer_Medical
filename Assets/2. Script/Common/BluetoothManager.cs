using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;
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

    private bool checkSerial = false;
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

        /*try
        {
            switch (scene.name)
            {
                case ("0. Tutorial"):
                    currGameManager = GameObject.Find("TutorialGameManager");
                    StartCoroutine(displaySensorData());
                    break;
                case ("1-1. RocketGame"):
                    currGameManager = GameObject.Find("RocketGameManager");
                    StartCoroutine(displaySensorData());
                    break;
                case ("1-2. RocketStage2"):
                    currGameManager = GameObject.Find("RocketGameManager");
                    StartCoroutine(displaySensorData());
                    Start();
                    break;
                case ("1-345. RocketStage345"):
                    currGameManager = GameObject.Find("RocketGameManager");
                    StartCoroutine(displaySensorData());
                    Start();
                    break;
                case ("2. CandleBlowing"):
                    currGameManager = GameObject.Find("CandleGameManager");
                    StartCoroutine(displaySensorData());
                    break;
                case ("3. Inhaler"):
                    currGameManager = GameObject.Find("InhaleGameManager");
                    StartCoroutine(displaySensorData());
                    break;
                default:
                    currGameManager = null;
                    break;
            }
        }
        catch (Exception)
        {
            Scene thisScene = SceneManager.GetSceneByName(scene.name);
            OnSceneLoaded(thisScene, LoadSceneMode.Single);
            throw;
        }
        */
    }



    void sceneLoaded()
    {
        Scene scene = SceneManager.GetActiveScene();
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

    // Start is called before the first frame update
    void Start()
    {
        BluetoothHelper.BLE = true;
        bluetoothHelperInstance = BluetoothHelper.GetInstance();

        if (!bluetoothHelperInstance.IsBluetoothEnabled())
            bluetoothHelperInstance.EnableBluetooth();

        SetBLEEvents();

        //Device scan
        Debug.Log("is scanning: " + bluetoothHelperInstance.ScanNearbyDevices());
        //when scan not started
        if (!bluetoothHelperInstance.ScanNearbyDevices())
        {

        }
        Permission.RequestUserPermission(Permission.CoarseLocation);
        //bluetoothHelperInstance.setDeviceName("BreatheInput");
        //bluetoothHelperInstance.setDeviceAddress("09:40:40:8a:39:3a");

        logging = GameObject.Find("LoggingManager");
        //sceneLoaded();
    }

    private void SetBLEEvents()
    {
        bluetoothHelperInstance.OnConnected += onConnected;
        bluetoothHelperInstance.OnConnectionFailed += (helper) =>
        {
            Debug.Log("Connection failed");
            ((BluetoothHelper)helper).Connect();
        };
        bluetoothHelperInstance.OnScanEnded += OnScanEnded;
        bluetoothHelperInstance.OnServiceNotFound += (helper, serviceName) =>
        {
            Debug.Log(serviceName);
        };
        bluetoothHelperInstance.OnCharacteristicNotFound += (helper, serviceName, characteristicName) =>
        {
            Debug.Log(characteristicName);
        };
        bluetoothHelperInstance.OnCharacteristicChanged += (helper, value, characteristic) =>
        {
            readData.byte0 = value[0];
            readData.byte1 = value[1];
            readData.byte2 = value[2];
            readData.byte3 = value[3];
            pressure = readData.encodedFloat;
            //Debug.Log(pressure);

        };

    }


    private void OnScanEnded(BluetoothHelper helper, LinkedList<BluetoothDevice> devices)
    {
        Debug.Log("FOund " + devices.Count);
        if (devices.Count == 0)
        {
            bluetoothHelperInstance.ScanNearbyDevices();
            return;
        }

        foreach (var d in devices)
        {
            Debug.Log(d.DeviceName);
        }

        try
        {
            //bluetoothHelperInstance.setDeviceName("BreatheInput");
            bluetoothHelperInstance.setDeviceName("Breat");
            //bluetoothHelperInstance.setDeviceAddress("09:40:40:8a:39:3a");
            //bluetoothHelperInstance.setDeviceAddress("C1:9F:CC:B3:2D:8A");
            bluetoothHelperInstance.Connect();
            Debug.Log("Connecting");
        }
        catch (Exception ex)
        {
            bluetoothHelperInstance.ScanNearbyDevices();
            Debug.Log(ex.Message);
        }

    }

  

    

    // Update is called once per frame
    void Update()
    {
        if(checkingBLE)
            chkBLE();
    }

    void chkBLE()
    {

        if (bluetoothHelperInstance.isConnected())
        {

            GameObject.Find("ArduinoState").GetComponent<Text>().text = ("연결됨");
            //GameObject.Find("ff").GetComponent<Text>().text = pressure.ToString();
            //vrSensorText.text = "Device Name: " + bluetoothHelperInstance.getDeviceName();
        }
        else if (!bluetoothHelperInstance.isConnected())
        {
            GameObject.Find("ArduinoState").GetComponent<Text>().text = "검색중";
        }
    }

    string[] dataToArray(string data)
    {
        string[] dataArray = new string[2];
        dataArray[0] = System.DateTime.Now.ToString();
        dataArray[1] = data;

        return dataArray;
    }

    public void onConnected(BluetoothHelper helper)
    {
        Debug.Log("Device connected: " + helper.isConnected());
        Debug.Log("Device Name: " + helper.getDeviceName());
        Debug.Log("Service Name: " + helper.getGattServices()[0]);
        Debug.Log("device addr: " + helper.getDeviceAddress());

        //Subscribe to pressure data Service and Characteristic
        pressureService = new BluetoothHelperService("00000000-0000-0000-0000-000000001101");

        //pressureLevelChar = new BluetoothHelperCharacteristic("2101");
        pressureLevelChar = new BluetoothHelperCharacteristic("00000000-0000-0000-0000-000000002101");
        pressureService.addCharacteristic(pressureLevelChar);

        helper.Subscribe(pressureService);
        helper.StartListening();
        //helper.ReadCharacteristic(pressureLevelChar);

        
    }

    private void OnApplicationQuit()
    {
        QuitBLE();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void QuitBLE()
    {
       
        bluetoothHelperInstance.Disconnect();
        bluetoothHelperInstance.OnConnected -= onConnected;
    }

    
}
