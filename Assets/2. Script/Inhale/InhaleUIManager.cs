using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InhaleUIManager : MonoBehaviour
{
    public Text scoreText = null;
    public Text foodCountTxt = null;
    public Image progressImage = null;
    public Transform scoreTr = null;
    public Transform playerTr = null;
    public RectTransform astroman = null;
    public ParticleSystem perfectEffect = null;
    public InhaleSoundManager soundManager = null;

    public int currStage = 1;
    public GameObject[] Stars = null;

    private void Update()
    {
        foodCountTxt.text = InhaleGameManager.instance.currFoodeat
            + " / " + 15 + "개";

    }


    public void InhaleScoreUI()
    {
        // Keep the wall-mounted pose authored in the Scene; only content and visibility change here.
        if (currStage != 5)
        {
            scoreText.text = ("음식" + InhaleGameManager.instance.currFoodeat
                + "개를 먹었어요!\n 15개까지 더 먹어볼까요?");
            scoreTr.gameObject.SetActive(true);
            if (VRUIManager.instance != null) VRUIManager.instance.SetScoreVisible(true);
        }
        else
        {
            scoreText.text = ("음식 15개를 다 먹었어요!");
            scoreTr.gameObject.SetActive(true);
            if (VRUIManager.instance != null) VRUIManager.instance.SetScoreVisible(true);
            perfectEffect.Play();
            soundManager.OnPerfectGame();
        }
        
        StartCoroutine(StarsAndProgress());
    }

    public void ResetScoreUI()
    {
        scoreTr.gameObject.SetActive(false);
        if (VRUIManager.instance != null) VRUIManager.instance.SetScoreVisible(false);
    }

    public void SetStage(int stage)
    {
        currStage = stage;
    }

    IEnumerator StarsAndProgress()
    {
        yield return new WaitForSeconds(1f);

        //단계별 별 보이기
        for (int i = 0; i < currStage; i++)
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
