// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;

namespace MRMotifs.SharedActivities.Helpers
{
    /// <summary>
    /// Manages scaling and alpha transitions for an object over time, using a coroutine to animate the object smoothly.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class HandleAnimationMotif : MonoBehaviour
    {
        [Tooltip("Interactable Unity Event Wrapper to call the animation methods from.")]
        [SerializeField]
        private InteractableUnityEventWrapper interactableUnityEventWrapper;

        [Tooltip("The target scale of the object when scaling up.")]
        [SerializeField]
        private Vector3 targetScale = new(0.03f, 0.25f, 0.03f);

        [Tooltip("The duration (in seconds) it takes to scale the object.")]
        [SerializeField]
        private float scalingDuration = 0.5f;

        [Tooltip("The material used to modify the object's alpha transparency.")]
        [SerializeField]
        private Material material;

        private Vector3 m_initialScale;
        private Coroutine m_currentCoroutine;

        private void Awake()
        {
            m_initialScale = transform.localScale;
            interactableUnityEventWrapper.WhenHover.AddListener(ScaleUp);
            interactableUnityEventWrapper.WhenUnhover.AddListener(ScaleDown);
        }

        private void OnDestroy()
        {
            interactableUnityEventWrapper.WhenHover.RemoveListener(ScaleUp);
            interactableUnityEventWrapper.WhenUnhover.RemoveListener(ScaleDown);
        }

        private void ScaleUp()
        {
            if (m_currentCoroutine != null)
            {
                StopCoroutine(m_currentCoroutine);
            }

            m_currentCoroutine = StartCoroutine(ScaleObject(targetScale, 180));
        }

        private void ScaleDown()
        {
            if (m_currentCoroutine != null)
            {
                StopCoroutine(m_currentCoroutine);
            }

            m_currentCoroutine = StartCoroutine(ScaleObject(m_initialScale, 0));
        }

        private IEnumerator ScaleObject(Vector3 target, float targetAlpha)
        {
            var startScale = transform.localScale;
            var startAlpha = material.color.a * 255f;
            float timeElapsed = 0;

            while (timeElapsed < scalingDuration)
            {
                var t = timeElapsed / scalingDuration;
                transform.localScale = Vector3.Lerp(startScale, target, t);
                SetMaterialAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
                timeElapsed += Time.deltaTime;
                yield return null;
            }

            transform.localScale = target;
            SetMaterialAlpha(targetAlpha);
        }

        private void SetMaterialAlpha(float alpha)
        {
            var color = material.color;
            color.a = alpha / 255f;
            material.color = color;
        }
    }
}
