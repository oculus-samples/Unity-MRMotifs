// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using MRMotifs.SharedAssets;
using UnityEngine;
using UnityEngine.UI;

namespace MRMotifs.PassthroughTransitioning
{
    [MetaCodeSample("MRMotifs-PassthroughTransitioning")]
    public class PassthroughSlider : MonoBehaviour
    {
        [Tooltip("The passthrough layer used for the fade effect.")]
        [SerializeField]
        private OVRPassthroughLayer oVRPassthroughLayer;

        [Tooltip("The direction of the fade effect.")]
        [SerializeField]
        private FadeDirection fadeDirection = FadeDirection.InsideOut;

        [Tooltip("The range of the passthrough fader sphere.")]
        [SerializeField]
        private float selectiveDistance = 2f;

        [Tooltip("The inverted alpha value at which the contextual boundary should be enabled/disabled.")]
        [SerializeField]
        private float boundaryThreshold = 0.75f;

        private Camera m_mainCamera;
        private Material m_material;
        private MeshRenderer m_meshRenderer;
        private MenuPanel m_menuPanel;
        private Slider m_alphaSlider;
        private bool m_hasCrossedThreshold;

        private static readonly int s_invertedAlpha = Shader.PropertyToID("_InvertedAlpha");
        private static readonly int s_direction = Shader.PropertyToID("_FadeDirection");

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

            // This is a property that determines whether premultiplied alpha blending is used for the eye field of view
            // layer, which can be adjusted to enhance the blending with underlays and potentially improve visual quality.
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
            if (m_menuPanel != null)
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
            m_material.SetFloat(s_invertedAlpha, OVRManager.IsPassthroughRecommended() ? 1 : 0);

            if (m_menuPanel != null)
            {
                m_alphaSlider.value = OVRManager.IsPassthroughRecommended() ? 0 : 1;
            }
        }

        private void HandleSliderChange(float value)
        {
            float normalizedAlpha;
            if (fadeDirection == FadeDirection.InsideOut)
            {
                normalizedAlpha = 1.1f - value * 0.45f;
            }
            else
            {
                normalizedAlpha = 1.0f - value;
            }

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
    }
}
