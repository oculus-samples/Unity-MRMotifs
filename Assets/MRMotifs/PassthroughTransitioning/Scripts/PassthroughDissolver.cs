/************************************************************************************
Copyright (c) Meta Platforms, Inc. and affiliates.
All rights reserved.

Licensed under the Oculus SDK License Agreement (the "License");
you may not use the Oculus SDK except in compliance with the License,
which is provided at the time of installation or download, or which
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Oculus SDK
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
************************************************************************************/

using UnityEngine;
using UnityEngine.UI;

public class PassthroughDissolver : MonoBehaviour
{
    [Tooltip("The range of the passthrough dissolver sphere.")]
    [SerializeField]
    private float distance = 20f;

    [Tooltip("The inverted alpha value at which the contextual boundary should be enabled/disabled.")]
    [SerializeField]
    private float boundaryThreshold = 0.25f;

    private Camera _mainCamera;
    private Material _material;
    private MeshRenderer _meshRenderer;
    private MenuPanel _menuPanel;
    private Slider _alphaSlider;

    private static readonly int DissolutionLevel = Shader.PropertyToID("_Level");

    private void Awake()
    {
        _mainCamera = Camera.main;
        if (_mainCamera != null)
        {
            _mainCamera.clearFlags = CameraClearFlags.Skybox;
        }

        // This is a property that determines whether premultiplied alpha blending is used for the eye field of view
        // layer, which can be adjusted to enhance the blending with underlays and potentially improve visual quality.
        OVRManager.eyeFovPremultipliedAlphaModeEnabled = false;

        _meshRenderer = GetComponent<MeshRenderer>();
        _material = _meshRenderer.material;
        _material.SetFloat(DissolutionLevel, 0);
        _meshRenderer.enabled = true;

        SetSphereSize(distance);

        _menuPanel = FindAnyObjectByType<MenuPanel>();

        if (_menuPanel != null)
        {
            _alphaSlider = _menuPanel.PassthroughFaderSlider;
            _alphaSlider.onValueChanged.AddListener(HandleSliderChange);
        }

#if UNITY_ANDROID
        CheckIfPassthroughIsRecommended();
#endif
    }

    private void OnDestroy()
    {
        if (_menuPanel != null)
        {
            _alphaSlider.onValueChanged.RemoveListener(HandleSliderChange);
        }
    }

    private void SetSphereSize(float size)
    {
        transform.localScale = new Vector3(size, size, size);
    }

    private void CheckIfPassthroughIsRecommended()
    {
        _material.SetFloat(DissolutionLevel, OVRManager.IsPassthroughRecommended() ? 1 : 0);
        OVRManager.instance.shouldBoundaryVisibilityBeSuppressed = OVRManager.IsPassthroughRecommended();

        if (_menuPanel != null)
        {
            _alphaSlider.value = OVRManager.IsPassthroughRecommended() ? 1 : 0;
        }
    }

    private void HandleSliderChange(float value)
    {
        _material.SetFloat(DissolutionLevel, value);

        if (value > boundaryThreshold || value < boundaryThreshold)
        {
            OVRManager.instance.shouldBoundaryVisibilityBeSuppressed = value > boundaryThreshold;
        }
    }
}
