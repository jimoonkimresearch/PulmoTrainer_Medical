using System.Collections;
using UnityEngine;

public class FireControl : MonoBehaviour
{
    [SerializeField] private GameObject offEffect;
    private Vector3 initialScale;
    private Quaternion initialRotation;
    private bool initialized;
    private bool extinguishing;
    private void Awake() { Initialize(); }
    private void Initialize()
    {
        if (initialized) return;
        initialScale = transform.localScale;
        initialRotation = transform.localRotation;
        initialized = true;
    }
    public void ShrinkAndOff()
    {
        Initialize();
        if (extinguishing || !gameObject.activeInHierarchy) return;
        extinguishing = true;
        StartCoroutine(Shrink());
    }
    private IEnumerator Shrink()
    {
        float duration = 0.15f;
        Vector3 from = transform.localScale;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(from, Vector3.zero, elapsed / duration);
            yield return null;
        }
        transform.localScale = Vector3.zero;
        AudioSource source = GetComponentInParent<AudioSource>();
        if (source != null) source.Play();
        if (offEffect != null) offEffect.SetActive(true);
        yield return new WaitForSeconds(1f);
        gameObject.SetActive(false);
    }
    public void ResetFlame()
    {
        Initialize();
        StopAllCoroutines();
        CancelInvoke();
        extinguishing = false;
        transform.localScale = initialScale;
        transform.localRotation = initialRotation;
        if (offEffect != null)
        {
            foreach (ParticleSystem particles in offEffect.GetComponentsInChildren<ParticleSystem>(true))
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            offEffect.SetActive(false);
        }
        gameObject.SetActive(true);
    }
}
