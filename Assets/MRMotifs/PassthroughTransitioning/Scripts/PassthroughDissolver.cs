// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using MRMotifs.SharedAssets;
using UnityEngine;
using UnityEngine.UI;

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
            m_material.SetFloat(s_dissolutionLevel, OVRManager.IsPassthroughRecommended() ? 1 : 0);
            OVRManager.instance.shouldBoundaryVisibilityBeSuppressed = OVRManager.IsPassthroughRecommended();

            if (m_menuPanel != null)
            {
                m_alphaSlider.value = OVRManager.IsPassthroughRecommended() ? 1 : 0;
            }
        }

        private void HandleSliderChange(float value)
        {
            m_material.SetFloat(s_dissolutionLevel, value);

            if (value > boundaryThreshold || value < boundaryThreshold)
            {
                OVRManager.instance.shouldBoundaryVisibilityBeSuppressed = value > boundaryThreshold;
            }
        }
    }
}
