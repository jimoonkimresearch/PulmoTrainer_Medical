using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System;


public class GameManager : MonoBehaviour
{
    /*
    GameManager
    세이브기능 담당.
    단계별 수행시간, 흡기 및 호기압력  저장.
    게임 내에 영점조정 루틴 개발
    최대흡기/호기량 수치 보유
    */

    //In-game values
    [SerializeField]
    public float maxIntake = -4000f;
    [SerializeField]
    public float maxFev1 = 1000f;
    [SerializeField]
    public float maxFvc = 1100f;
    [SerializeField]
    public float maxInhalePressure = -300f;
    [SerializeField]
    public float maxExhalePressure = 200f;

    [SerializeField]
    public float accelerationRatio = 10f;
    [SerializeField]
    public float sensorToIntakeRatio = 1.0f;
    [SerializeField]
    public float sensorToOuttakeRatio = 3.0f;
    [SerializeField]
    public float outtakeToSpeedRatio = 0.8f;
    [SerializeField]
    public float sensorActionPotential = 1f;

    public string GameDataFileName = ".json";

    int saveCount = 0;

    [SerializeField]
    private int loadSceneSeed = -1;
    [SerializeField]
    private int currScenePlayed = 0;

    private String[] sceneNames = { "1-1. RocketGame", "2. CandleBlowing", "3. Inhaler" };

    public int getStage { get; set; } = -1;

    public float getProgress { get; set; } = -1f;
    public int getStar { get; set; } = -1;

    public GameData _gameData;
    public GameData gameData
    {
        get
        {
            if(_gameData == null)
            {
                LoadGameData();
                SaveGameData();
            }
            return _gameData;
        }
    }

    // 세이브 구현을 위한 Singleton
    public static GameManager instance = null;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else if (instance != this)
        {
            Destroy(this.gameObject);
        }

        if (PlayerPrefs.GetInt("IsSeedSet", 0) == 0)
        {
            System.Random sceneSeed = new System.Random();
            loadSceneSeed = sceneSeed.Next(3, 21);
            PlayerPrefs.SetInt("SceneSeed", loadSceneSeed);
        }
        else if(PlayerPrefs.GetInt("IsSeedSet") != 0)
        {
            while (loadSceneSeed > 38)
                loadSceneSeed = loadSceneSeed - 18;

            loadSceneSeed = PlayerPrefs.GetInt("IsSeedSet");
            PlayerPrefs.SetInt("SceneSeed", 1);
        }
    }

    private void Start()
    {
        saveCount = PlayerPrefs.GetInt("SaveCount");
    }

    /// <summary>
    /// Inhale Capacity setting용 함수
    /// /// </summary>
    public void InhaleCapacitySetter(float inputMaxInhale, float inputInhalePressure)
    {
        maxIntake = inputMaxInhale;
        maxInhalePressure = inputInhalePressure;
    }

    public void ExhaleCapacitySetter(float inputMaxExhale, float inputMaxFEV1, float inputExhalePressure)
    {
        maxFvc = inputMaxExhale;
        maxFev1 = inputMaxFEV1;
        maxExhalePressure = inputExhalePressure;
    }

    public void LoadGameData()
    {
        string filePath = Application.persistentDataPath + GameDataFileName;

        if(File.Exists(filePath))
        {
            Debug.Log("불러오기 성공!");
            string FromJsonData = File.ReadAllText(filePath);
            _gameData = JsonUtility.FromJson<GameData>(FromJsonData);
        }
        else
        {
            Debug.Log("새로운 파일 생성");

            _gameData = new GameData();
        }
    }

    public void SaveGameData()
    {
        string ToJsonData = JsonUtility.ToJson(gameData);
        string filePath = Application.persistentDataPath + GameDataFileName;
        File.WriteAllText(filePath, ToJsonData);
        saveCount++;
        //Debug.Log("저장 완료");
    }

    public void SimulNextScene()
    {
        Debug.Log("SimulNextScene clicked");
        switch (loadSceneSeed%6)
        {
            case 0:
                if (currScenePlayed == 0)
                {
                    currScenePlayed += 1;
                    SceneManager.LoadScene(sceneNames[0]);
                    Debug.Log("Load Scene 0");
                }
                else if (currScenePlayed == 1)
                {
                    currScenePlayed += 1;
                    SceneManager.LoadScene(sceneNames[2]);
                    Debug.Log("Load Scene 2");
                }
                else if (currScenePlayed == 2)
                {
                    currScenePlayed += 1;
                    SceneManager.LoadScene(sceneNames[1]);
                    Debug.Log("Load Scene 1");
                }
                else
                {
                    Application.Quit();
                }
                break;
            case 1:
                if (currScenePlayed == 0)
                {
                    currScenePlayed += 1;
                    SceneManager.LoadScene(sceneNames[1]);
                    Debug.Log("Load Scene 1");
                }
                else if (currScenePlayed == 1)
                {
                    currScenePlayed += 1;
                    SceneManager.LoadScene(sceneNames[0]);
                    Debug.Log("Load Scene 0");
                }
                else if (currScenePlayed == 2)
                {
                    currScenePlayed += 1;
                    SceneManager.LoadScene(sceneNames[2]);
                    Debug.Log("Load Scene 2");
                }
                else
                {
                    Application.Quit();
                }
                break;
            case 2:
                if (currScenePlayed == 0)
                {
                    currScenePlayed += 1;
                    SceneManager.LoadScene(sceneNames[2]);
                    Debug.Log("Load Scene 2");
                }
                else if (currScenePlayed == 1)
                {
                    currScenePlayed += 1;
                    SceneManager.LoadScene(sceneNames[1]);
                    Debug.Log("Load Scene 1");
                }
                else if (currScenePlayed == 2)
                {
                    currScenePlayed += 1;
                    SceneManager.LoadScene(sceneNames[0]);
                    Debug.Log("Load Scene 0");
                }
                else
                {
                    Application.Quit();
                }
                break;
            case 3:
                if (currScenePlayed == 0)
                {
                    currScenePlayed += 1;
                    SceneManager.LoadScene(sceneNames[0]);
                    Debug.Log("Load Scene 0");
                }
                else if (currScenePlayed == 1)
                {
                    currScenePlayed += 1;
                    SceneManager.LoadScene(sceneNames[1]);
                    Debug.Log("Load Scene 1");
                }
                else if (currScenePlayed == 2)
                {
                    currScenePlayed += 1;
                    SceneManager.LoadScene(sceneNames[2]);
                    Debug.Log("Load Scene 2");
                }
                else
                {
                    Application.Quit();
                }
                break;
            case 4:
                if (currScenePlayed == 0)
                {
                    currScenePlayed += 1;
                    SceneManager.LoadScene(sceneNames[1]);
                    Debug.Log("Load Scene 1");
                }
                else if (currScenePlayed == 1)
                {
                    currScenePlayed += 1;
                    SceneManager.LoadScene(sceneNames[2]);
                    Debug.Log("Load Scene 2");
                }
                else if (currScenePlayed == 2)
                {
                    currScenePlayed += 1;
                    SceneManager.LoadScene(sceneNames[0]);
                    Debug.Log("Load Scene 0");
                }
                else
                {
                    Application.Quit();
                }
                break;
            case 5:
                if (currScenePlayed == 0)
                {
                    currScenePlayed += 1;
                    SceneManager.LoadScene(sceneNames[0]);
                    Debug.Log("Load Scene 0");
                }
                else if (currScenePlayed == 1)
                {
                    currScenePlayed += 1;
                    SceneManager.LoadScene(sceneNames[1]);
                    Debug.Log("Load Scene 1");
                }
                else if (currScenePlayed == 2)
                {
                    currScenePlayed += 1;
                    SceneManager.LoadScene(sceneNames[2]);
                    Debug.Log("Load Scene 2");
                }
                else
                {
                    Application.Quit();
                }
                break;
            default:
                break;

        }

    }

    private void OnApplicationQuit()
    {
        SaveGameData();
        PlayerPrefs.SetInt("SaveCount", saveCount);
        if(currScenePlayed == 3)
            PlayerPrefs.SetInt("SceneSeed", ++loadSceneSeed);
    }
}