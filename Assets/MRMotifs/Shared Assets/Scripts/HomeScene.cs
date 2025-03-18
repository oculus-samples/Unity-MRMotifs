// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace MRMotifs.SharedAssets
{
    [MetaCodeSample("MRMotifs-SharedAssets")]
    public class HomeScene : MonoBehaviour
    {
        [SerializeField] private OVRPassthroughLayer oVRPassthroughLayer;

        private Camera m_mainCamera;
        private Color m_skyboxBackgroundColor;

        private void Awake()
        {
            m_mainCamera = Camera.main;

            if (m_mainCamera != null)
            {
                m_skyboxBackgroundColor = m_mainCamera.backgroundColor;
            }

#if UNITY_ANDROID
        CheckIfPassthroughIsRecommended();
#endif
        }

        private void CheckIfPassthroughIsRecommended()
        {
            if (m_mainCamera == null)
            {
                return;
            }

            if (OVRManager.IsPassthroughRecommended())
            {
                oVRPassthroughLayer.enabled = true;
                m_mainCamera.clearFlags = CameraClearFlags.SolidColor;
                m_mainCamera.backgroundColor = Color.clear;
            }
            else
            {
                oVRPassthroughLayer.enabled = false;
                m_mainCamera.clearFlags = CameraClearFlags.Skybox;
                m_mainCamera.backgroundColor = m_skyboxBackgroundColor;
            }
        }
    }
}
