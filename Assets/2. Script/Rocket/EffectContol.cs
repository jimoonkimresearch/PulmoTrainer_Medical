using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectContol : MonoBehaviour
{
    private Transform tr;

    public Transform RocketTr;
    public GameObject boostEffect;
    public GameObject startEffect = null;

    // Start is called before the first frame update
    void Start()
    {
        tr = GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update()
    {
        tr.position = RocketTr.position;
    }

    public void Boost()
    {
        boostEffect.SetActive(true);
        startEffect.SetActive(true);
    }


    public void ResetEffects()
    {
        StopAllCoroutines();
        CancelInvoke();
        ResetEffect(boostEffect);
        ResetEffect(startEffect);
    }
    private static void ResetEffect(GameObject effect)
    {
        if (effect == null) return;
        foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>(true))
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        effect.SetActive(false);
    }
}
