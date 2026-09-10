using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialUIManager : MonoBehaviour
{
    public RectTransform pressureGuage = null;
    public Text pressureTxt = null;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
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
}
