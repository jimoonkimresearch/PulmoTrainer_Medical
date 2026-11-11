using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System;
using UnityEngine.Serialization;
using PulmoTrainer.Calibration;
using PulmoTrainer.Training;


[RequireComponent(typeof(TrainingEndSequence))]
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
    [FormerlySerializedAs("maxIntake"), SerializeField]
    private float calibratedInhaleCapacity = -4000f;
    public float maxIntake => calibratedInhaleCapacity;
    [FormerlySerializedAs("maxFev1"), SerializeField]
    private float calibratedFev1 = 1000f;
    public float maxFev1 => calibratedFev1;
    [FormerlySerializedAs("maxFvc"), SerializeField]
    private float calibratedExhaleCapacity = 1100f;
    public float maxFvc => calibratedExhaleCapacity;
    [FormerlySerializedAs("maxInhalePressure"), SerializeField]
    private float calibratedInhalePressure = -300f;
    public float maxInhalePressure => calibratedInhalePressure;
    [FormerlySerializedAs("maxExhalePressure"), SerializeField]
    private float calibratedExhalePressure = 200f;
    public float maxExhalePressure => calibratedExhalePressure;

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

    private TrainingEndSequence trainingEndSequence;
    private bool trainingEndRequested;

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
            EnsureTrainingEndSequence();
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
        calibratedInhaleCapacity = inputMaxInhale;
        calibratedInhalePressure = inputInhalePressure;
    }

    public void ExhaleCapacitySetter(float inputMaxExhale, float inputMaxFEV1, float inputExhalePressure)
    {
        calibratedExhaleCapacity = inputMaxExhale;
        calibratedFev1 = inputMaxFEV1;
        calibratedExhalePressure = inputExhalePressure;
    }

    public void ApplyCalibration(BreathCalibration calibration)
    {
        if (!calibration.IsUsable)
            throw new ArgumentException("Calibration requires finite nonzero inhale/exhale measurements.", nameof(calibration));
        calibratedInhaleCapacity = calibration.InhaleCapacity;
        calibratedInhalePressure = calibration.InhalePressure;
        calibratedExhaleCapacity = calibration.ExhaleCapacity;
        calibratedExhalePressure = calibration.ExhalePressure;
        calibratedFev1 = calibration.Fev1;
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
        if (trainingEndRequested) return;
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
                    CompleteTraining();
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
                    CompleteTraining();
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
                    CompleteTraining();
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
                    CompleteTraining();
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
                    CompleteTraining();
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
                    CompleteTraining();
                }
                break;
            default:
                break;

        }

    }

    private void EnsureTrainingEndSequence()
    {
        if (trainingEndSequence != null) return;
        trainingEndSequence = GetComponent<TrainingEndSequence>();
        // Existing scene instances may predate RequireComponent.
        if (trainingEndSequence == null) trainingEndSequence = gameObject.AddComponent<TrainingEndSequence>();
    }

    private void CompleteTraining()
    {
        if (trainingEndRequested) return;
        trainingEndRequested = true;
        EnsureTrainingEndSequence();
        trainingEndSequence.Begin();
    }

    private void OnApplicationQuit()
    {
        SaveGameData();
        PlayerPrefs.SetInt("SaveCount", saveCount);
        if(currScenePlayed == 3)
            PlayerPrefs.SetInt("SceneSeed", ++loadSceneSeed);
    }
}
