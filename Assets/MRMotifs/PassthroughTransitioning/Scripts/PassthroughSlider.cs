/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using UnityEngine;
using UnityEngine.UI;

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

    private Camera _mainCamera;
    private Material _material;
    private MeshRenderer _meshRenderer;
    private MenuPanel _menuPanel;
    private Slider _alphaSlider;
    private bool _hasCrossedThreshold;

    private static readonly int InvertedAlpha = Shader.PropertyToID("_InvertedAlpha");
    private static readonly int Direction = Shader.PropertyToID("_FadeDirection");

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

        _material.SetFloat(InvertedAlpha, 1);
        oVRPassthroughLayer.enabled = true;
        _meshRenderer.enabled = true;

        SetSphereSize(selectiveDistance);
        SetFadeDirection((int)fadeDirection);

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

    private void SetFadeDirection(int direction)
    {
        _material.SetInt(Direction, direction);
    }

    private void CheckIfPassthroughIsRecommended()
    {
        _material.SetFloat(InvertedAlpha, OVRManager.IsPassthroughRecommended() ? 1 : 0);

        if (_menuPanel != null)
        {
            _alphaSlider.value = OVRManager.IsPassthroughRecommended() ? 0 : 1;
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

        _material.SetFloat(InvertedAlpha, normalizedAlpha);

        if (value > boundaryThreshold * _alphaSlider.maxValue && !_hasCrossedThreshold)
        {
            OVRManager.instance.shouldBoundaryVisibilityBeSuppressed = false;
            _hasCrossedThreshold = true;
        }
        else if (value < boundaryThreshold * _alphaSlider.maxValue && _hasCrossedThreshold)
        {
            OVRManager.instance.shouldBoundaryVisibilityBeSuppressed = true;
            _hasCrossedThreshold = false;
        }
    }
}
