// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using UnityEngine;

namespace MRMotifs.InstantContentPlacement.DepthEffects
{
    /// <summary>
    /// Expands the scan wave effect over a set duration, then destroys the object.
    /// </summary>
    public class ShockWaveEffectMotif : MonoBehaviour
    {
        [Header("Scan Wave Settings")]
        [Tooltip("The maximum scale the scan wave will reach.")]
        [SerializeField]
        private float endScale = 25.0f;

        [Tooltip("The duration over which the scan wave expands.")]
        [SerializeField]
        private float duration = 3.5f;

        [Tooltip("Controls the growth rate of the scan wave expansion.")]
        [SerializeField]
        private float growthRate = 2.0f;

        private const float START_SCALE = 0.0f;
        private float m_currentTimer;

        private void Awake()
        {
            transform.localScale = Vector3.one * START_SCALE;
            StartCoroutine(ExpandAndDestroy());
        }

        private IEnumerator ExpandAndDestroy()
        {
            while (m_currentTimer <= duration)
            {
                m_currentTimer += Time.deltaTime;
                var scale = Mathf.Lerp(START_SCALE, endScale, Mathf.Pow(m_currentTimer / duration, growthRate));
                transform.localScale = Vector3.one * scale;
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
