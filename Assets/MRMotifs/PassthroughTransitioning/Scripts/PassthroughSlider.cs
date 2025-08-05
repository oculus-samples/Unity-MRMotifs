// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using MRMotifs.SharedAssets;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace MRMotifs.PassthroughTransitioning
{
    [MetaCodeSample("MRMotifs-PassthroughTransitioning")]
    public class PassthroughSlider : MonoBehaviour
    {
        [Tooltip("The passthrough layer used for the fade effect.")]
        [SerializeField] private OVRPassthroughLayer oVRPassthroughLayer;

        [Tooltip("The direction of the fade effect.")]
        [SerializeField] private FadeDirection fadeDirection = FadeDirection.InsideOut;

        [Tooltip("The range of the passthrough fader sphere.")]
        [SerializeField] private float selectiveDistance = 2f;

        [Tooltip("The inverted alpha value at which the contextual boundary should be enabled/disabled.")]
        [SerializeField] private float boundaryThreshold = 0.75f;

        [Header("Fade Loop Settings")]
        [SerializeField] private float loopSpeed = 1f;

        private Camera m_mainCamera;
        private Material m_material;
        private MeshRenderer m_meshRenderer;
        private MenuPanel m_menuPanel;
        private Slider m_alphaSlider;
        private bool m_hasCrossedThreshold;
        private Coroutine m_fadeCoroutine;
        private float m_loopTime = 0f;

        private static readonly int s_invertedAlpha = Shader.PropertyToID("_InvertedAlpha");
        private static readonly int s_direction = Shader.PropertyToID("_FadeDirection");

        [Header("One-Shot Fade Settings")]
        [SerializeField] private float oneShotFadeDuration = 1f;


        /// <summary>
        /// Defines the direction of the fade effect.
        /// </summary>
        private enum FadeDirection
        {
            Normal,
            RightToLeft,
            TopToBottom,
            InsideOut
        }

        private void Awake()
        {
            m_mainCamera = Camera.main;
            if (m_mainCamera != null)
            {
                m_mainCamera.clearFlags = CameraClearFlags.Skybox;
            }

            OVRManager.eyeFovPremultipliedAlphaModeEnabled = false;

            m_meshRenderer = GetComponent<MeshRenderer>();
            m_material = m_meshRenderer.material;

            m_material.SetFloat(s_invertedAlpha, 1);
            oVRPassthroughLayer.enabled = true;
            m_meshRenderer.enabled = true;

            SetSphereSize(selectiveDistance);
            SetFadeDirection((int)fadeDirection);

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
            if (m_menuPanel != null && m_alphaSlider != null)
            {
                m_alphaSlider.onValueChanged.RemoveListener(HandleSliderChange);
            }
        }

        private void SetSphereSize(float size)
        {
            transform.localScale = new Vector3(size, size, size);
        }

        private void SetFadeDirection(int direction)
        {
            m_material.SetInt(s_direction, direction);
        }

        private void CheckIfPassthroughIsRecommended()
        {
            float val = OVRManager.IsPassthroughRecommended() ? 1f : 0f;
            m_material.SetFloat(s_invertedAlpha, val);

            if (m_menuPanel != null)
            {
                m_alphaSlider.value = OVRManager.IsPassthroughRecommended() ? 0 : 1;
            }
        }

        private void HandleSliderChange(float value)
        {
            float normalizedAlpha = (fadeDirection == FadeDirection.InsideOut)
                ? 1.1f - value * 0.45f
                : 1.0f - value;

            m_material.SetFloat(s_invertedAlpha, normalizedAlpha);

            if (value > boundaryThreshold * m_alphaSlider.maxValue && !m_hasCrossedThreshold)
            {
                OVRManager.instance.shouldBoundaryVisibilityBeSuppressed = false;
                m_hasCrossedThreshold = true;
            }
            else if (value < boundaryThreshold * m_alphaSlider.maxValue && m_hasCrossedThreshold)
            {
                OVRManager.instance.shouldBoundaryVisibilityBeSuppressed = true;
                m_hasCrossedThreshold = false;
            }
        }

        // Public method: starts coroutine loop
        public void StartFadeLoop()
        {
            if (m_fadeCoroutine == null)
            {
                m_fadeCoroutine = StartCoroutine(FadeRoutine());
            }
        }

        // Public method: stops coroutine loop
        public void StopFadeLoop()
        {
            if (m_fadeCoroutine != null)
            {
                StopCoroutine(m_fadeCoroutine);
                m_fadeCoroutine = null;

                m_material.SetFloat(s_invertedAlpha, 1f); // Reset
                if (m_alphaSlider != null)
                {
                    m_alphaSlider.SetValueWithoutNotify(0f);
                }

                OVRManager.instance.shouldBoundaryVisibilityBeSuppressed = true;
            }
        }

        private IEnumerator FadeRoutine()
        {
            m_loopTime = 0f;

            while (true)
            {
                m_loopTime += Time.deltaTime * loopSpeed;
                float sliderValue = Mathf.PingPong(m_loopTime, 1f);

                float normalizedAlpha = (fadeDirection == FadeDirection.InsideOut)
                    ? 1.1f - sliderValue * 0.45f
                    : 1.0f - sliderValue;

                m_material.SetFloat(s_invertedAlpha, normalizedAlpha);

                if (m_alphaSlider != null)
                {
                    m_alphaSlider.SetValueWithoutNotify(sliderValue);
                }

                OVRManager.instance.shouldBoundaryVisibilityBeSuppressed = sliderValue < boundaryThreshold;

                yield return null;
            }
        }

        public void FadeInOnce()
        {
            StopFadeLoop(); // cancel any ongoing loop
            StartCoroutine(FadeOnceRoutine(true));
        }

        public void FadeOutOnce()
        {
            StopFadeLoop(); // cancel any ongoing loop
            StartCoroutine(FadeOnceRoutine(false));
        }

        private IEnumerator FadeOnceRoutine(bool fadeIn)
        {
            float time = 0f;

            while (time < oneShotFadeDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / oneShotFadeDuration);

                float sliderValue = fadeIn ? t : (1f - t);

                float normalizedAlpha = (fadeDirection == FadeDirection.InsideOut)
                    ? 1.1f - sliderValue * 0.45f
                    : 1.0f - sliderValue;

                m_material.SetFloat(s_invertedAlpha, normalizedAlpha);

                if (m_alphaSlider != null)
                {
                    m_alphaSlider.SetValueWithoutNotify(sliderValue);
                }

                OVRManager.instance.shouldBoundaryVisibilityBeSuppressed = sliderValue < boundaryThreshold;

                yield return null;
            }
        }

    }
}
