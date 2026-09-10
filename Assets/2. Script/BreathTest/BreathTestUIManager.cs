using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BreathTestUIManager : MonoBehaviour
{
    public RectTransform pressureGuage = null;
    public Text pressureTxt = null;
    public int currStage = 0;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ResetUI()
    {

    }

    public void SetPressureGuage(float pressure)
    {
        pressureGuage.localPosition = new Vector3(0f, pressure * 40f, 0f);
    }

    public void SetPressureTxt(float pressure)
    {
        pressureTxt.text = pressure.ToString();
    }

    public void SetStage(int stage)
    {
        currStage = stage;
    }

    public void ScoreUI()
    {

    }

    public void ResetScoreUI()
    {

    }
}
