using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RocketUIManager : MonoBehaviour
{
    public Text scoreText = null;
    public Image scorePanel = null;
    public Image progressImage = null;
    public Transform scoreTr = null;
    public Transform playerTr = null;
    public RectTransform astroman = null;
    public VRUIManager vRUIManager = null;
    public RocketSoundManager soundManager = null;
    public ParticleSystem perfectEffect = null;

    public int currStage = 1;
    public GameObject[] Stars = null;


    

    /// <summary>
    /// 로켓 객체의 RocketBehavior 스크립트로부터 호흡량에 의해 계산된 높이 float변수를 받아 점수화 후 점수 UI 알파값 조절하여 표시
    /// </summary>
    /// <param name="height"></param>
    public void ScoreUI(float height)
    {
        scoreTr.position = playerTr.position + ((playerTr.right+playerTr.forward) * 2f);
        scoreTr.LookAt(playerTr.position);
        scoreTr.Rotate(new Vector3(0f, 180f, 0f));
        if (height <= vRUIManager.maxRocketHeight)
        {
            scoreText.text = (height.ToString() + "미터까지 비행에 성공했어요!");
            scoreTr.gameObject.SetActive(true);
            StartCoroutine(StarsAndProgress());
        }
        else
        {
            scoreText.text = (vRUIManager.maxRocketHeight + "미터까지 비행에 성공했어요!\n최고높이에요! 굉장해요!");
            scoreTr.gameObject.SetActive(true);
            perfectEffect.gameObject.SetActive(true);
            perfectEffect.Play();
            soundManager.OnPerfectGame();
            StartCoroutine(StarsAndProgress());
        }
    }

    public void ResetScoreUI()
    {
        scoreTr.gameObject.SetActive(false);
    }

    public void SetStage(int stage)
    {
        currStage = stage;
    }

    IEnumerator StarsAndProgress()
    {
        yield return new WaitForSeconds(1f);

        //단계별 별 보이기
        for (int i=0; i < currStage; i++)
        {
            Stars[i].SetActive(true);
            Stars[i].GetComponent<Animation>().Play();
            Stars[i].GetComponent<AudioSource>().Play();
            yield return new WaitForSeconds(0.5f);
        }

        //진행바. 별 갯수와 동일하게 진행
        float nowX = -0.375f;
        float target = (currStage * 0.15f) + nowX;
        float moveTerm = 60f;
        Vector3 pos = astroman.position;
        while (nowX <= target)
        {
            progressImage.fillAmount += ((((float)currStage / 5f) / moveTerm));
            nowX += ((0.15f * currStage) / moveTerm);
            astroman.localPosition = new Vector3(nowX, -0.09f, 0f);
            yield return null;
        }
    }

    public void ResetUI()
    {
        for (int i = 0; i < currStage; i++)
        {
            Stars[i].SetActive(false);
            Stars[i].GetComponent<Animation>().Rewind();
            Stars[i].GetComponent<Animation>().Stop();
        }
        progressImage.fillAmount = 0f;
        astroman.localPosition = new Vector3(-0.375f, -0.09f, 0f);
    }
}
