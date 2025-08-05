using UnityEngine;
using System.Collections;

public class SkyboxBlendController : MonoBehaviour
{
    [SerializeField] private Material blendedSkyboxMaterial;
    [SerializeField] private float blendSpeed = 1f;

    private Coroutine blendRoutine;

    public void BlendToSkyboxB() => StartBlend(1f); // Fully Skybox B
    public void BlendToSkyboxA() => StartBlend(0f); // Fully Skybox A

    private void StartBlend(float target)
    {
        if (blendedSkyboxMaterial == null)
        {
            Debug.LogWarning("Skybox material not assigned!");
            return;
        }

        if (blendRoutine != null)
            StopCoroutine(blendRoutine);

        blendRoutine = StartCoroutine(BlendTo(target));
    }

    private IEnumerator BlendTo(float target)
    {
        float current = blendedSkyboxMaterial.GetFloat("_BlendAmount");

        while (Mathf.Abs(current - target) > 0.01f)
        {
            current = Mathf.MoveTowards(current, target, Time.deltaTime * blendSpeed);
            blendedSkyboxMaterial.SetFloat("_BlendAmount", current);
            yield return null;
        }

        blendedSkyboxMaterial.SetFloat("_BlendAmount", target);
    }
}
