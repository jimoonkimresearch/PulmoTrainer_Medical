using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InhaledFoodsControl : MonoBehaviour
{
    [SerializeField]
    private GameObject[] inhaledFoods = null;
    private Vector3[] inhaledFoodsPos = null;
    private Quaternion[] inhaledFoodsRot = null;
    private Vector3[] inhaledFoodsScale = null;

    public int foodCount = 0;

    private void Start()
    {
        inhaledFoods = new GameObject[5];
        inhaledFoodsPos = new Vector3[5];
        inhaledFoodsRot = new Quaternion[5];
        inhaledFoodsScale = new Vector3[5];
    }

    public void SetInhaledFood(GameObject food)
    {
        inhaledFoods[foodCount] = food;
        inhaledFoodsPos[foodCount] = food.GetComponent<Transform>().position;
        inhaledFoodsRot[foodCount] = food.GetComponent<Transform>().rotation;
        inhaledFoodsScale[foodCount] = food.GetComponent<Transform>().localScale;
        foodCount++;
        
    }

    public void ResetFoods()
    {
        for (int i = 0; i < foodCount; i++)
        {
            inhaledFoods[i].SetActive(true);
            inhaledFoods[i].GetComponent<Transform>().position = inhaledFoodsPos[i];
            inhaledFoods[i].GetComponent<Transform>().rotation = inhaledFoodsRot[i];
            inhaledFoods[i].GetComponent<Transform>().localScale = inhaledFoodsScale[i];
        }
        foodCount = 0;
        inhaledFoods = new GameObject[5];
        inhaledFoodsPos = new Vector3[5];
        inhaledFoodsRot = new Quaternion[5];
        inhaledFoodsScale = new Vector3[5];

    }
}
