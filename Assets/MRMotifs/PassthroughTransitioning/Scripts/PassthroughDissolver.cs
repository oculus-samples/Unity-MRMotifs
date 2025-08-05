// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using MRMotifs.SharedAssets;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace MRMotifs.PassthroughTransitioning
{
    [MetaCodeSample("MRMotifs-PassthroughTransitioning")]
    public class PassthroughDissolver : MonoBehaviour
    {
        [Tooltip("The range of the passthrough dissolver sphere.")]
        [SerializeField]
        private float distance = 20f;

        [Tooltip("The inverted alpha value at which the contextual boundary should be enabled/disabled.")]
        [SerializeField]
        private float boundaryThreshold = 0.25f;

        private Camera m_mainCamera;
        private Material m_material;
        private MeshRenderer m_meshRenderer;
        private MenuPanel m_menuPanel;
        private Slider m_alphaSlider;

        private static readonly int s_dissolutionLevel = Shader.PropertyToID("_Level");

        // Debug loop settings for coroutine-based looping
        [Header("Debug Loop Settings")]
        [SerializeField]
        private float m_loopSpeed = 1f;
        private float m_loopTime = 0f;
        private Coroutine m_dissolveCoroutine;

        private void Awake()
        {
            m_mainCamera = Camera.main;
            if (m_mainCamera != null)
            {
                m_mainCamera.clearFlags = CameraClearFlags.Skybox;
            }

            // Determines whether premultiplied alpha blending is used for the eye field of view layer.
            OVRManager.eyeFovPremultipliedAlphaModeEnabled = false;

            m_meshRenderer = GetComponent<MeshRenderer>();
            m_material = m_meshRenderer.material;
            m_material.SetFloat(s_dissolutionLevel, 0);
            m_meshRenderer.enabled = true;

            SetSphereSize(distance);

            m_menuPanel = FindAnyObjectByType<MenuPanel>();

            if (m_menuPanel != null)
            {
                m_alphaSlider = m_menuPanel.PassthroughFaderSlider;
                m_alphaSlider.onValueChanged.AddListener(HandleSliderChange);
            }

#if UNITY_ANDROID
            CheckIfPassthroughIsRecommended();
#endif
        }

        private void OnDestroy()
        {
            if (m_menuPanel != null)
            {
                m_alphaSlider.onValueChanged.RemoveListener(HandleSliderChange);
            }
        }

        private void SetSphereSize(float size)
        {
            transform.localScale = new Vector3(size, size, size);
        }

        private void CheckIfPassthroughIsRecommended()
        {
            bool recommended = OVRManager.IsPassthroughRecommended();
            m_material.SetFloat(s_dissolutionLevel, recommended ? 1f : 0f);
            OVRManager.instance.shouldBoundaryVisibilityBeSuppressed = recommended;

            if (m_menuPanel != null)
            {
                m_alphaSlider.value = recommended ? 1f : 0f;
            }
        }

        private void HandleSliderChange(float value)
        {
            m_material.SetFloat(s_dissolutionLevel, value);

            // Update boundary visibility based on the slider value.
            if (value > boundaryThreshold || value < boundaryThreshold)
            {
                OVRManager.instance.shouldBoundaryVisibilityBeSuppressed = value > boundaryThreshold;
            }
        }

        // The coroutine method for the dissolve loop.
        private IEnumerator DissolveRoutine()
        {
            m_loopTime = 0f;
            while (true)
            {
                m_loopTime += Time.deltaTime * m_loopSpeed;
                float lerpValue = Mathf.PingPong(m_loopTime, 1f);
                m_material.SetFloat(s_dissolutionLevel, lerpValue);

                if (m_alphaSlider != null)
                {
                    m_alphaSlider.SetValueWithoutNotify(lerpValue);
                }

                OVRManager.instance.shouldBoundaryVisibilityBeSuppressed = lerpValue > boundaryThreshold;
                yield return null;
            }
        }

        // This method starts the dissolve loop.
        [ContextMenu("Start Dissolve Loop")]
        public void StartDissolveLoop()
        {
            if (m_dissolveCoroutine == null)
            {
                m_dissolveCoroutine = StartCoroutine(DissolveRoutine());
            }
        }

        // This method stops the dissolve loop.
        [ContextMenu("Stop Dissolve Loop")]
        public void StopDissolveLoop()
        {
            if (m_dissolveCoroutine != null)
            {
                StopCoroutine(m_dissolveCoroutine);
                m_dissolveCoroutine = null;

                // Optionally reset the effect to its default state.
                m_material.SetFloat(s_dissolutionLevel, 0f);
                if (m_alphaSlider != null)
                {
                    m_alphaSlider.SetValueWithoutNotify(0f);
                }
                OVRManager.instance.shouldBoundaryVisibilityBeSuppressed = false;
            }
        }
    }
}
