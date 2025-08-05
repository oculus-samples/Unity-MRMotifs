using UnityEngine;
using System.Collections;

public class ObjectFader : MonoBehaviour
{
    [SerializeField] private Material fadeMaterial;
    [SerializeField] private float fadeSpeed = 1f;

    private Coroutine fadeRoutine;

    public void FadeIn()
    {
        StartFade(0f); // fully visible
    }

    public void FadeOut()
    {
        StartFade(1f); // fully transparent
    }

    private void StartFade(float target)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeTo(target));
    }

    private IEnumerator FadeTo(float target)
    {
        float current = fadeMaterial.GetFloat("_FadeAmount");

        while (Mathf.Abs(current - target) > 0.01f)
        {
            current = Mathf.MoveTowards(current, target, Time.deltaTime * fadeSpeed);
            fadeMaterial.SetFloat("_FadeAmount", current);
            yield return null;
        }

        fadeMaterial.SetFloat("_FadeAmount", target);
    }
}
