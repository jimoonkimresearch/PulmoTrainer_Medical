using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CamRayCast : MonoBehaviour
{
    public Transform tr = null;
    public GameObject SelectBG = null;
    public Image SelectImg = null;

    [SerializeField]
    private float SelectionTime = 50f;

    [SerializeField]
    private float fps = 60f;

    private LineRenderer line;
    private GameObject prevHit;

    Ray ray = new Ray();
    private RaycastHit hit;

    public bool messageSended = false;


    // Start is called before the first frame update
    void Start()
    {
        tr = GetComponent<Transform>();
        line = GetComponent<LineRenderer>();
        line.enabled = false;
        line.startWidth = 0.1f;
        line.endWidth = 0.05f;

        StartCoroutine(this.ShowLaserBeam());
        ray = new Ray(tr.position, tr.forward);
    }

    // Update is called once per frame
    void Update()
    {
        
        line.SetPosition(0, tr.position);
        if (Physics.Raycast(tr.position, tr.forward, out hit, 100f))
        {
            line.SetPosition(1, hit.point);
            if (hit.collider.gameObject.layer == 9)
            {
                hit.collider.gameObject.SendMessage("OnRayHit");
            }
        }
        else line.SetPosition(1, ray.GetPoint(100.0f));
    }

    IEnumerator ShowLaserBeam()
    {
        line.enabled = true;
        yield return new WaitForSeconds(Random.Range(0.01f, 0.2f));
        line.enabled = false;
    }

    public void StartSelection()
    {
        StartCoroutine(SelectGame());
    }

    IEnumerator SelectGame()
    {
        SelectBG.SetActive(true);
        prevHit = hit.collider.gameObject;
        while (hit.collider.gameObject.layer == 9)
        {
            if (SelectImg.fillAmount >= 0.99)
            {
                switch (hit.collider.gameObject.tag)
                {
                    case ("TOMAIN"):
                        SceneManager.LoadScene("0. StartScene", LoadSceneMode.Single);
                        break;
                    case ("ROCKETGAME"):
                        SceneManager.LoadScene("1-1. RocketGame", LoadSceneMode.Single);
                        break;
                    case ("ROCKET2"):
                        SceneManager.LoadScene("1-2. RocketStage2", LoadSceneMode.Single);
                        break;
                    case ("ROCKET3"):
                        SceneManager.LoadScene("1-345. RocketStage345", LoadSceneMode.Single);
                        break;
                    case ("ROCKET45"):
                        if (!messageSended)
                        {
                            GameObject.Find("RocketGameManager").SendMessage("toNextStage");
                            messageSended = true;
                        }
                        else
                        {
                            //messageSended = false;
                        }
                        break;
                    case ("CANDLENEXTSTAGE"):
                        if (!messageSended)
                        {
                            GameObject.Find("CandleGameManager").SendMessage("toNextStage");
                            messageSended = true;
                        }
                        break;
                    case ("FOODGAME"):
                        if (!messageSended)
                        {
                            GameObject.Find("InhaleGameManager").SendMessage("toNextStage");
                            messageSended = true;
                        }
                        break;
                    case ("ENDGAME"):
#if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;

#else
                        Application.Quit();
#endif

                        break;
                    default:
                        break;
                }
            }
            SelectImg.fillAmount += 1 / (fps * SelectionTime);
            yield return Time.deltaTime;
        }
        prevHit.SendMessage("OutLineOff");
        SelectBG.SetActive(false);
        SelectImg.fillAmount = 0f;
    }

    public void ResetFlag()
    {
        messageSended = false;
        SelectImg.fillAmount = 0f;
    }
}
