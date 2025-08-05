using System.Collections;
using UnityEngine;

public class ObjectScaler : MonoBehaviour
{
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private bool useOvershoot = true;
    [SerializeField] private float overshootMultiplier = 1.05f;

    private Vector3 initialScale;

    private void Start()
    {
        initialScale = transform.localScale;
    }

    public void ScaleIn()
    {
        StopAllCoroutines();
        StartCoroutine(ScaleOverTime(0f, 1f));
    }

    public void ScaleOut()
    {
        StopAllCoroutines();
        StartCoroutine(ScaleOverTime(1f, 0f));
    }

    private IEnumerator ScaleOverTime(float from, float to)
    {
        float elapsed = 0f;
        float overshootTime = duration * 0.8f;
        float settleTime = duration * 0.2f;

        if (useOvershoot)
        {
            // Phase 1: Scale from start to overshoot target
            while (elapsed < overshootTime)
            {
                float t = elapsed / overshootTime;
                float scaleAmount = Mathf.Lerp(from, to, t);
                float overshoot = (to > from) ? overshootMultiplier : 1f / overshootMultiplier;
                transform.localScale = initialScale * Mathf.Lerp(from, overshoot, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Phase 2: Ease back to final target
            elapsed = 0f;
            Vector3 currentScale = transform.localScale;
            while (elapsed < settleTime)
            {
                float t = elapsed / settleTime;
                transform.localScale = Vector3.Lerp(currentScale, initialScale * to, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            // Simple Lerp without overshoot
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float scaleAmount = Mathf.Lerp(from, to, t);
                transform.localScale = initialScale * scaleAmount;
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        transform.localScale = initialScale * to;
    }
}
