// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using Meta.XR.Samples;
using MRMotifs.SharedAssets;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MRMotifs.PassthroughTransitioning
{
    /// <summary>
    /// A unified passthrough fader that supports both Selective and Underlay modes.
    /// Select the mode in the inspector via the Passthrough Viewing Mode property.
    /// </summary>
    [MetaCodeSample("MRMotifs-PassthroughTransitioning")]
    public class PassthroughFader : MonoBehaviour
    {
        /// <summary>
        /// The direction in which the fade effect will occur.
        /// </summary>
        private enum FadeDirection
        {
            Normal,
            RightToLeft,
            TopToBottom,
            InsideOut
        }

        /// <summary>
        /// The viewing mode for passthrough. Select "Underlay" to have the effect apply over the entire view
        /// or "Selective" to limit it to a sphere defined by selectiveDistance.
        /// </summary>
        private enum PassthroughViewingMode
        {
            Underlay,
            Selective
        }

        /// <summary>
        /// Internal state of the fader determined by the target alpha.
        /// </summary>
        private enum FaderState
        {
            MR,
            VR,
            InTransition
        }

        /// <summary>
        /// The fader state is derived from _targetAlpha.
        /// </summary>
        private FaderState State => Mathf.Approximately(m_targetAlpha, 1f) ? FaderState.MR :
            Mathf.Approximately(m_targetAlpha, 0f) ? FaderState.VR :
            FaderState.InTransition;

        [Header("Passthrough Fader Settings")]
        [Tooltip("The passthrough layer used for the fade effect.")]
        [SerializeField]
        private OVRPassthroughLayer oVRPassthroughLayer;

        [Tooltip("Select Underlay to fade the entire view or Selective for a limited sphere.")]
        [SerializeField]
        private PassthroughViewingMode passthroughViewingMode = PassthroughViewingMode.Selective;

        [Tooltip("Size/range of the passthrough fader sphere (used in Selective mode).")]
        [Range(0.01f, 100f)]
        [SerializeField]
        private float selectiveDistance = 5f;

        [Tooltip("The speed of the fade effect.")]
        [SerializeField]
        private float fadeSpeed = 1f;

        [Tooltip("The direction of the fade effect.")]
        [SerializeField]
        private FadeDirection fadeDirection = FadeDirection.TopToBottom;

        [Header("Fade Events")]
        [Tooltip("Event triggered when the fade in starts.")]
        [SerializeField]
        private UnityEvent onStartFadeIn = new();

        [Tooltip("Event triggered when the fade out starts.")]
        [SerializeField]
        private UnityEvent onStartFadeOut = new();

        [Tooltip("Event triggered when the fade in has ended.")]
        [SerializeField]
        private UnityEvent onFadeInComplete = new();

        [Tooltip("Event triggered when the fade out has ended.")]
        [SerializeField]
        private UnityEvent onFadeOutComplete = new();

        private Camera m_mainCamera;
        private Material m_material;
        private MeshRenderer m_meshRenderer;
        private MenuPanel m_menuPanel;
        private Button m_passthroughButton;
        private Color m_skyboxBackgroundColor;
        private float m_targetAlpha;
        private const float FADE_TOLERANCE = 0.001f;

        private static readonly int s_invertedAlpha = Shader.PropertyToID("_InvertedAlpha");
        private static readonly int s_direction = Shader.PropertyToID("_FadeDirection");

        private void Awake()
        {
            m_mainCamera = Camera.main;
            if (m_mainCamera != null)
            {
                m_skyboxBackgroundColor = m_mainCamera.backgroundColor;
            }

            // Disable premultiplied alpha blending for better underlay blending.
            OVRManager.eyeFovPremultipliedAlphaModeEnabled = false;

            m_meshRenderer = GetComponent<MeshRenderer>();
            m_material = m_meshRenderer.material;

            m_menuPanel = FindAnyObjectByType<MenuPanel>();
            if (m_menuPanel != null)
            {
                m_passthroughButton = m_menuPanel.PassthroughFaderButton;
                m_passthroughButton.onClick.AddListener(TogglePassthrough);
            }

            oVRPassthroughLayer.passthroughLayerResumed.AddListener(OnPassthroughLayerResumed);

            SetupPassthrough();

#if UNITY_ANDROID
            CheckIfPassthroughIsRecommended();
#endif
        }

        private void OnDestroy()
        {
            if (m_menuPanel != null)
            {
                m_passthroughButton.onClick.RemoveListener(TogglePassthrough);
            }

            oVRPassthroughLayer.passthroughLayerResumed.RemoveListener(OnPassthroughLayerResumed);
        }

        /// <summary>
        /// Sets up the passthrough based on the selected viewing mode.
        /// </summary>
        private void SetupPassthrough()
        {
            if (passthroughViewingMode == PassthroughViewingMode.Underlay)
            {
                var maxCamView = m_mainCamera.farClipPlane - 0.01f;
                transform.localScale = new Vector3(maxCamView, maxCamView, maxCamView);
                m_meshRenderer.enabled = false;
            }
            else // Selective
            {
                transform.localScale = new Vector3(selectiveDistance, selectiveDistance, selectiveDistance);
                m_meshRenderer.enabled = true;
            }
        }

        /// <summary>
        /// Checks if passthrough is recommended and adjusts the camera and material settings.
        /// </summary>
        private void CheckIfPassthroughIsRecommended()
        {
            if (m_mainCamera == null) return;

            if (OVRManager.IsPassthroughRecommended())
            {
                if (passthroughViewingMode == PassthroughViewingMode.Underlay)
                {
                    m_mainCamera.clearFlags = CameraClearFlags.SolidColor;
                    m_mainCamera.backgroundColor = Color.clear;
                }
                else
                {
                    m_mainCamera.clearFlags = CameraClearFlags.Skybox;
                    m_mainCamera.backgroundColor = m_skyboxBackgroundColor;
                }

                m_material.SetFloat(s_invertedAlpha, 1);
            }
            else
            {
                oVRPassthroughLayer.enabled = false;
                m_mainCamera.clearFlags = CameraClearFlags.Skybox;
                m_mainCamera.backgroundColor = m_skyboxBackgroundColor;
                m_material.SetFloat(s_invertedAlpha, 0);
            }
        }

        /// <summary>
        /// Called (for example, by a UI button) to toggle between passthrough states.
        /// </summary>
        public void TogglePassthrough()
        {
            UpdateFadeDirection();

            switch (State)
            {
                case FaderState.MR:
                    if (passthroughViewingMode == PassthroughViewingMode.Underlay)
                    {
                        m_meshRenderer.enabled = true;
                        m_mainCamera.clearFlags = CameraClearFlags.Skybox;
                        m_mainCamera.backgroundColor = m_skyboxBackgroundColor;
                    }

                    m_targetAlpha = 0;
                    onStartFadeOut?.Invoke();
                    StopAllCoroutines();
                    StartCoroutine(FadeToTarget());
                    break;

                case FaderState.VR:
                    oVRPassthroughLayer.enabled = true;
                    onStartFadeIn?.Invoke();
                    break;

                case FaderState.InTransition:
                    m_targetAlpha = Mathf.Approximately(m_targetAlpha, 0f) ? 1f : 0f;
                    var fadeEvent = Mathf.Approximately(m_targetAlpha, 0f) ? onStartFadeOut : onStartFadeIn;
                    fadeEvent?.Invoke();
                    StartCoroutine(FadeToTarget());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Updates the shader’s fade direction property.
        /// </summary>
        private void UpdateFadeDirection()
        {
            m_material.SetInt(s_direction, (int)fadeDirection);
        }

        /// <summary>
        /// Listener for when the passthrough layer is resumed.
        /// </summary>
        private void OnPassthroughLayerResumed(OVRPassthroughLayer passthroughLayer)
        {
            if (passthroughViewingMode == PassthroughViewingMode.Underlay)
            {
                m_meshRenderer.enabled = true;
            }

            m_targetAlpha = 1;
            StopAllCoroutines();
            StartCoroutine(FadeToTarget());
        }

        /// <summary>
        /// Fades the material’s alpha toward the target value.
        /// </summary>
        private IEnumerator FadeToTarget()
        {
            var currentAlpha = m_material.GetFloat(s_invertedAlpha);
            while (Mathf.Abs(currentAlpha - m_targetAlpha) > FADE_TOLERANCE)
            {
                currentAlpha = Mathf.MoveTowards(currentAlpha, m_targetAlpha, fadeSpeed * Time.deltaTime);
                m_material.SetFloat(s_invertedAlpha, currentAlpha);
                yield return null;
            }

            if (Mathf.Abs(m_targetAlpha - 1f) < FADE_TOLERANCE)
            {
                if (passthroughViewingMode == PassthroughViewingMode.Underlay)
                {
                    m_mainCamera.clearFlags = CameraClearFlags.SolidColor;
                    m_mainCamera.backgroundColor = Color.clear;
                }

                onFadeInComplete?.Invoke();
            }
            else
            {
                oVRPassthroughLayer.enabled = false;
                if (passthroughViewingMode == PassthroughViewingMode.Underlay)
                {
                    m_mainCamera.clearFlags = CameraClearFlags.Skybox;
                    m_mainCamera.backgroundColor = m_skyboxBackgroundColor;
                }

                onFadeOutComplete?.Invoke();
            }

            m_meshRenderer.enabled = (passthroughViewingMode != PassthroughViewingMode.Underlay);
        }
    }
}
