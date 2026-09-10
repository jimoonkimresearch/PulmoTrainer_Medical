using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InputFieldInput : MonoBehaviour
{
    public Text inputVal;

    private TouchScreenKeyboard defaultKeyboard;
    void Start()
    {
        
    }

    public void StartInput()
    {
        defaultKeyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default);
    }
}
