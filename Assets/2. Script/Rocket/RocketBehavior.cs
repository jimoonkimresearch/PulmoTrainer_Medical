using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class RocketBehavior : MonoBehaviour
{
    private float fev1 = 700f;
    private float maxFev1 = 1000f;
    private float fvc = 1000f;
    private float maxFvc = 1400f;
    private float ratio = 0f;
    private float chargeRaito = 0f;
    private float flyRatio = 0.01f;
    private float chargeTime = 0f;
    private float flyTime = 0f;
    private float startFly = 0f;
    private float effectTime = 0f;

    //찌그러지는 비율. fev1 최대 1000 시 y 절반, x, y 10프로 증가토록함.
    private float deformY = 0.00025f;
    private float deformX = 0.55f;
    private float deformZ = 0.55f;


    public GameObject EffectCtrl = null;
    public GameObject Ceiling = null;
    public Transform rocketTr = null;
    public Rigidbody rocketRb = null;
    public GameObject startEffect = null;
    public GameObject endEffect = null;
    public GameObject boostEffect = null;
    public InputField fev1Input = null;
    public InputField fvcInput = null;
    public GameObject UIManager = null;
    public RawImage rocketCam = null;

    private bool simulated = false;


    /// <summary>
    /// FEV1과 FVC를 받아 필요 수치들 계산.
    /// </summary>
    /// <param name="getFEV1"></param>
    /// <param name="getFVC"></param>
    public void calculate(float getFEV1, float getFVC)
    {
        fev1 = getFEV1;
        fvc = getFVC;
        ratio = fev1 / fvc;
        chargeTime = fev1 * chargeRaito;
        flyTime = fvc * flyRatio;
    }

    private void Start()
    {
        if (rocketTr == null)
            rocketTr = this.GetComponent<Transform>();
        if (rocketRb == null)
            rocketRb = this.GetComponent<Rigidbody>();

    }

    private void Update()
    {
        if (Input.touchCount > 0 || Input.GetKeyDown("space"))
        {
            if (!simulated)
            {
                simulated = true; 
                BreathInput();
                Launch();
            }
        }
    }

    public void BreathInput()
    {
        
        fev1Input.text = "700";
        fvcInput.text = "1000";
        
    }

    /// <summary>
    /// 로켓 자체의 움직임을 컨트롤
    /// </summary>
    public void Launch()
    {
        fev1 = float.Parse(fev1Input.text);
        fvc = float.Parse(fvcInput.text);
        calculate(fev1, fvc);
        effectTime = Time.time;
        StartCoroutine(PreLaunchEffect());
    }

    /// <summary>
    /// fev1에 따라 1초간 로켓의 높이를 찌그러지게. scale.y를 줄이고 scale.z, x를 늘려 찌그러진듯한 효과.
    /// fev1은 최대 1000으로 가정했으므로 적절히 비율을 설정한다.
    /// 최대의 경우 y는 절반, x와 z는 10%증가하게 비율을 설정.
    /// 반복과 동시에 동시진행을 위해 코루틴으로.
    /// </summary>
    /// <returns></returns>
    IEnumerator PreLaunchEffect()
    {
        float yPerFrame = (fev1 * deformY) / 55;
        float xPerFrame = ((fev1 / maxFev1) * (deformX - 0.5f)) / 55;
        float zPerFrame = ((fev1 / maxFev1) * (deformZ - 0.5f)) / 55;


        while (rocketTr.localScale.y>(0.5 - fev1*deformY))
        {
            Vector3 newScale = new Vector3(rocketTr.localScale.x + xPerFrame, rocketTr.localScale.y - yPerFrame, rocketTr.localScale.z + zPerFrame);
            rocketTr.localScale =  newScale;
            yield return 1 / 60f;
        }

        while(rocketTr.localScale.y<0.5f)
        {
            Vector3 newScale = new Vector3(rocketTr.localScale.x - (xPerFrame*10), rocketTr.localScale.y + (yPerFrame*10), rocketTr.localScale.z - (zPerFrame*10));
            rocketTr.localScale = newScale;
            yield return 1 / 60f;
        }
        startEffect.SetActive(true);
        startFly = Time.time;
        StartCoroutine(LaunchBehavior());
    }

    /// <summary>
    /// 발사효과 이후 실제 로켓 움직임
    /// </summary>
    /// <returns></returns>
    IEnumerator LaunchBehavior()
    {
        UIManager.SendMessage("HideInhaleHud");
        Ceiling.SendMessage("CeilingOpening");
        EffectCtrl.SendMessage("Boost");
        while (Time.time <= startFly + flyTime)
        {
            yield return 0.1f;
            rocketRb.AddForce(Vector3.up * flyTime, ForceMode.Force);
        }

        StartCoroutine("FinishRocket");
    }

    IEnumerator FinishRocket()
    {
        StopCoroutine(LaunchBehavior());

        float height = rocketTr.position.y;
        rocketRb.useGravity = false;
        rocketRb.linearVelocity = Vector3.zero;
        endEffect.SetActive(true);
        UIManager.SendMessage("ShowInhaleHud");
        UIManager.SendMessage("ScoreUI", height);

        

        while(endEffect!=null)
            yield return 0.1f;


        rocketCam.gameObject.SetActive(false);
        this.gameObject.SetActive(false);
    }
}
