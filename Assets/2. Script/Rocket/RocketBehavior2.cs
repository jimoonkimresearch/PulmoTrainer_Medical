using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ArduinoBluetoothAPI;


public class RocketBehavior2 : MonoBehaviour
{
    private float ratio = 0f;
    private float chargeRaito = 0f;
    private float flyRatio = 0.01f;
    private float chargeTime = 0f;
    private float flyTime = 0f;
    private float startFly = 0f;
    private float effectTime = 0f;

    private bool launchReady = false;
    private bool launchStarted = false;

    //찌그러지는 비율. fev1 최대 1000 시 y 절반, x, y 10프로 증가토록함.
    private float deformY = 0.25f;
    private float deformX = 0.05f;
    private float deformZ = 0.05f;

    private float y;
    private float x;
    private float z;
    private int returnCount = 0;
    private float outtake = 0f;



    public GameObject EffectCtrl = null;
    public GameObject Ceiling = null;
    public Transform rocketTr = null;
    public Rigidbody rocketRb = null;
    public GameObject startEffect = null;
    public GameObject endEffect = null;
    public GameObject boostEffect = null;
    public InputField IntakeInput = null;
    public InputField fev1Input = null;
    public InputField fvcInput = null;
    public GameObject UIManager = null;
    public RawImage rocketCam = null;
    public Image intakeGuage = null;
    public GameObject networkManager = null;
    public RocketGameManager rocketGameManager = null;
    public GameManager gameManager = null;
    public RocketPlayerBehavior playerCtrl = null;
    

    private void Start()
    {
        if (rocketTr == null)
            rocketTr = this.GetComponent<Transform>();
        if (rocketRb == null)
            rocketRb = this.GetComponent<Rigidbody>();

        rocketGameManager = RocketGameManager.instance;
        gameManager = GameManager.instance;

        IntakeInput.text = "0";
        fev1Input.text = "0";
        fvcInput.text = "0";

        
    }

    private void Update()
    {
        //Debug.Log("rocket speed: " + rocketRb.velocity.magnitude);
    }


    /// <summary>
    /// 로켓 행동 컨트롤 위한 Flag 변환. 그 중에서 불기 중인데 속도가 1f 이하이면 끝내기 시퀀스 시작.
    /// </summary>
    /// <returns></returns>
    IEnumerator toExhaleAndFinish()
    {
        while(true)
        {
            if (rocketRb.linearVelocity.magnitude <= 1.05f || rocketTr.position.y >= 1500f)
            {
                rocketGameManager.SendMessage("toFinishState");
                StartCoroutine(FinishRocket());
                break;
            }
            yield return null;
        }
        StopCoroutine(toExhaleAndFinish());
    }


    /// <summary>
    /// 통신으로 데이터 받아옴
    /// </summary>
    public void InHaleStart()
    {
        rocketGameManager.currState = RocketGameManager.RocketState.INHALE;
    }

    /// <summary>
    /// 게임매니저에서 게임 시작버튼 클릭 시 흡기 시작. 입력값 들어올 때 마다 로켓형태변화
    /// </summary>
    void Intake(float sensorInput)
    {
        if ((sensorInput <= gameManager.sensorActionPotential * -1f))   //흡기압력이 -1 이하일 때.
        {
            //입력값과 기존값 더해 총흡기량 계산 및 UI에 표시
            float intake = (sensorInput * gameManager.sensorToIntakeRatio) + float.Parse(IntakeInput.text);

            IntakeInput.text = intake.ToString();

            //흡기 게이지 세팅
            float ratio = intake / gameManager.maxIntake;
            //Debug.Log(ratio);


            //로켓형태변화
            y = 0.5f - (ratio * deformY);
            x = 0.5f + (ratio * deformX);
            z = 0.5f + (ratio * deformZ);
            rocketTr.localScale = new Vector3(x, y, z);

        }

    }

    void ReadyForLaunch()
    {
        Ceiling.SendMessage("CeilingOpening");
    }


    /// <summary>
    /// 시간 카운팅은 통신에서 한 뒤 초기1초 동안 실행.
    /// </summary>
    /// <param name="sensorInput"></param>
    void Fev1Outtake(float sensorInput)
    {
        outtake = sensorInput * gameManager.sensorToOuttakeRatio * gameManager.outtakeToSpeedRatio;
        fev1Input.text = (float.Parse(fev1Input.text) + outtake).ToString();
        fvcInput.text = fev1Input.text;
        rocketRb.AddForce(Vector3.up * outtake * gameManager.accelerationRatio, ForceMode.Acceleration);
    }

    void FvcOuttake(float sensorInput)
    {
        outtake = sensorInput * gameManager.sensorToOuttakeRatio * gameManager.outtakeToSpeedRatio;
        fvcInput.text = (float.Parse(fvcInput.text) + outtake).ToString();
        rocketRb.AddForce(Vector3.up * outtake * gameManager.accelerationRatio, ForceMode.Acceleration);
    }

    /// <summary>
    /// 발사효과 이후 실제 로켓 움직임. 천장에 열기 메시지, 이펙트 시작 메시지, 로켓모양복원. 종료시 종료코루틴 시작.
    /// </summary>
    /// <returns></returns>
    void startLaunching()
    {
        EffectCtrl.SendMessage("Boost");
        StartCoroutine(SetDeform());
        Invoke("OnExhale", 2f);
    }

    void OnExhale()
    {
        StartCoroutine(toExhaleAndFinish());
    }


    /// <summary>
    /// 찌그러진 로켓을 되돌림. 1/6초
    /// </summary>
    /// <returns></returns>
    IEnumerator SetDeform()
    {
        float changeX = (rocketTr.localScale.x - 0.5f) / 10f;
        float changeY = (0.5f - rocketTr.localScale.y) / 10f;
        float changeZ = (rocketTr.localScale.z - 0.5f) / 10f;
        int itter = 0;

        while (itter<10)
        {
            y = rocketTr.localScale.y + changeY;
            x = rocketTr.localScale.x - changeX;
            z = rocketTr.localScale.z - changeZ;

            rocketTr.localScale = new Vector3(x, y, z);
            itter++;
            yield return 1 / 60f;
        }
        StopCoroutine(SetDeform());
    }

    IEnumerator FinishRocket()
    {
       StopCoroutine(toExhaleAndFinish());

        playerCtrl.EndFollowing();
        float height = rocketTr.position.y;
        rocketRb.useGravity = false;
        rocketRb.AddForce(Vector3.up * 100f, ForceMode.Acceleration);
        endEffect.SetActive(true);
        UIManager.SendMessage("ScoreUI", height);



        while (endEffect != null)
            yield return 0.1f;


        //GameManager.instance.SendMessage("SaveGameData");

        //rocketCam.gameObject.SetActive(false);
        //this.gameObject.SetActive(false);
    }

    public void ResetRocket()
    {
        rocketTr.position = new Vector3(1.11f, 1.46f, 2.2f);
        rocketRb.useGravity = true;
        x = 0.5f;
        y = 0.5f;
        z = 0.5f;
        playerCtrl.StartFollowing();
    }
}