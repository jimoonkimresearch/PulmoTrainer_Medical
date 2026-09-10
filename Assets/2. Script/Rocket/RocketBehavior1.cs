using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class RocketBehavior1 : MonoBehaviour
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
    private float deformY = 0.25f;
    private float deformX = 0.05f;
    private float deformZ = 0.05f;

    private float clicktime = 0f;
    private float input1perframe = 10f;
    private float input2perframe = 5f;
    private bool launched = false;
    private bool deformed = false;
    private float y;
    private float x;
    private float z;


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

        StartCoroutine("BreathInput");
    }

    
    IEnumerator BreathInput()
    {
        while (true)
        {

            if (PulmoInput.AHeld)
            {
                clicktime += 1 / 60f;
                if (clicktime >= 1f)
                    fvcInput.text = (clicktime * input1perframe * 60).ToString();
                else
                {
                    fev1Input.text = (clicktime * input1perframe * 60).ToString();
                    if (deformed == false)
                    {
                        deformed = true;
                        StartCoroutine(SetDeform());
                    }
                }
            }

            if (PulmoInput.SHeld)
            {
                clicktime += 1 / 60f;
                if (clicktime >= 1f)
                    fvcInput.text = (clicktime * input2perframe * 60).ToString();
                else
                {
                    fev1Input.text = (clicktime * input2perframe * 60).ToString();
                    if (deformed == false)
                    {
                        deformed = true;
                        StartCoroutine(SetDeform());
                    }
                }

            }

            if (clicktime >= 1f)
            {
                if (!deformed && launched)
                    StartCoroutine(PreLaunchEffect());

                if (!deformed && !launched)
                    StartCoroutine(LaunchBehavior());
            }

            yield return 1 / 60f;
        }
    }


    /// <summary>
    /// 찌그러진 로켓을 되돌림. 1/6초
    /// </summary>
    /// <returns></returns>
    IEnumerator SetDeform()
    {
        while (deformed == true && clicktime <= 1f)
        {
            y = 0.5f - ((float.Parse(fev1Input.text) / maxFev1) * deformY);
            x = 0.5f + ((float.Parse(fev1Input.text) / maxFev1) * deformX);
            z = 0.5f + ((float.Parse(fev1Input.text) / maxFev1) * deformZ);

            rocketTr.localScale = new Vector3(x, y, z);
            yield return 1 / 60f;
        }
        deformed = false;
        launched = true;
        StopCoroutine(SetDeform());
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
        launched = false;

        float ydiff = 0.5f - rocketTr.localScale.y;     //-0.5
        float xdiff = rocketTr.localScale.x - 0.5f;     //0.5
        float zdiff = rocketTr.localScale.z - 0.5f;     //0.5

        Vector3 newScale;
        int iter = 0;

        while (iter < 10)
        {
            newScale = new Vector3(rocketTr.localScale.x - (xdiff / 10), rocketTr.localScale.y + (ydiff / 10), rocketTr.localScale.z - (zdiff / 10));
            rocketTr.localScale = newScale;
            iter++;
            yield return 1 / 60f;
        }

        StopCoroutine(PreLaunchEffect());
    }

    /// <summary>
    /// 발사효과 이후 실제 로켓 움직임
    /// </summary>
    /// <returns></returns>
    IEnumerator LaunchBehavior()
    {
        deformed = true;
        launched = true;
        UIManager.SendMessage("HideInhaleHud");

        Ceiling.SendMessage("CeilingOpening");
        EffectCtrl.SendMessage("Boost");

        while (PulmoInput.AHeld || PulmoInput.SHeld)
        {
            rocketRb.AddForce(Vector3.up * 100f, ForceMode.Acceleration);

            if (PulmoInput.DHeld)
            {
                StartCoroutine("FinishRocket");

            }

            yield return 1 / 60f;


        }

    }

    IEnumerator FinishRocket()
    {
        StopCoroutine(LaunchBehavior());

        
        rocketRb.useGravity = false;
        rocketRb.linearVelocity = Vector3.zero;
        float height = rocketTr.position.y;
        Debug.Log(height.ToString());
        endEffect.SetActive(true);
        



        while (endEffect != null)
            yield return 0.1f;

        UIManager.SendMessage("ScoreUI", height);
        rocketCam.gameObject.SetActive(false);
        this.gameObject.SetActive(false);
    }

}