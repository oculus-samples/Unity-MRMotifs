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

using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PassthroughFaderUnderlay : MonoBehaviour
{
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

    /// <summary>
    /// Determines the current state we are in.
    /// </summary>
    private enum FaderState
    {
        MR,
        VR,
        InTransition
    }

    private FaderState State => Mathf.Approximately(_targetAlpha, 1) ? FaderState.MR :
        Mathf.Approximately(_targetAlpha, 0) ? FaderState.VR :
        FaderState.InTransition;

    [Header("Passthrough Fader Settings")]
    [Tooltip("The passthrough layer used for the fade effect.")]
    [SerializeField] private OVRPassthroughLayer oVRPassthroughLayer;

    [Tooltip("The speed of the fade effect.")]
    [SerializeField] private float fadeSpeed = 1f;

    [Tooltip("The direction of the fade effect.")]
    [SerializeField] private FadeDirection fadeDirection = FadeDirection.TopToBottom;

    [Header("Fade Events")]
    [Tooltip("Event triggered when the fade in starts.")]
    [SerializeField] private UnityEvent onStartFadeIn = new();

    [Tooltip("Event triggered when the fade out starts.")]
    [SerializeField] private UnityEvent onStartFadeOut = new();

    [Tooltip("Event triggered when the fade in has ended.")]
    [SerializeField] private UnityEvent onFadeInComplete = new();

    [Tooltip("Event triggered when the fade out has ended.")]
    [SerializeField] private UnityEvent onFadeOutComplete = new();

    private Camera _mainCamera;
    private Material _material;
    private MeshRenderer _meshRenderer;
    private MenuPanel _menuPanel;
    private Button _passthroughButton;
    private Color _skyboxBackgroundColor;
    private float _targetAlpha;
    private const float FadeTolerance = 0.001f;

    private static readonly int InvertedAlpha = Shader.PropertyToID("_InvertedAlpha");
    private static readonly int Direction = Shader.PropertyToID("_FadeDirection");

    private void Awake()
    {
        _mainCamera = Camera.main;

        if (_mainCamera != null)
        {
            _skyboxBackgroundColor = _mainCamera.backgroundColor;
        }

        // This is a property that determines whether premultiplied alpha blending is used for the eye field of view
        // layer, which can be adjusted to enhance the blending with underlays and potentially improve visual quality.
        OVRManager.eyeFovPremultipliedAlphaModeEnabled = false;

        _meshRenderer = GetComponent<MeshRenderer>();
        _material = _meshRenderer.material;

        _menuPanel = FindAnyObjectByType<MenuPanel>();
        if (_menuPanel != null)
        {
            _passthroughButton = _menuPanel.PassthroughFaderButton;
            _passthroughButton.onClick.AddListener(TogglePassthrough);
        }

        oVRPassthroughLayer.passthroughLayerResumed.AddListener(OnPassthroughLayerResumed);
        SetupPassthrough();

#if UNITY_ANDROID
        CheckIfPassthroughIsRecommended();
#endif
    }

    private void OnDestroy()
    {
        if (_menuPanel != null)
        {
            _passthroughButton.onClick.RemoveListener(TogglePassthrough);
        }

        oVRPassthroughLayer.passthroughLayerResumed.RemoveListener(OnPassthroughLayerResumed);
    }

    private void SetupPassthrough()
    {
        var maxCamView = _mainCamera.farClipPlane - 0.01f;
        transform.localScale = new Vector3(maxCamView, maxCamView, maxCamView);
        _meshRenderer.enabled = false;
    }

    private void CheckIfPassthroughIsRecommended()
    {
        if (_mainCamera == null)
        {
            return;
        }

        if (OVRManager.IsPassthroughRecommended())
        {
            _mainCamera.clearFlags = CameraClearFlags.SolidColor;
            _mainCamera.backgroundColor = Color.clear;
            _material.SetFloat(InvertedAlpha, 1);
        }
        else
        {
            oVRPassthroughLayer.enabled = false;
            _mainCamera.clearFlags = CameraClearFlags.Skybox;
            _mainCamera.backgroundColor = _skyboxBackgroundColor;
            _material.SetFloat(InvertedAlpha, 0);
        }
    }

    public void TogglePassthrough()
    {
        UpdateFadeDirection();

        switch (State)
        {
            case FaderState.MR:
            {
                _meshRenderer.enabled = true;
                _mainCamera.clearFlags = CameraClearFlags.Skybox;
                _mainCamera.backgroundColor = _skyboxBackgroundColor;

                _targetAlpha = 0;
                onStartFadeOut?.Invoke();
                StopAllCoroutines();
                StartCoroutine(FadeToTarget());
                break;
            }
            case FaderState.VR:
                oVRPassthroughLayer.enabled = true;
                onStartFadeIn?.Invoke();
                break;
            case FaderState.InTransition:
            {
                _targetAlpha = Mathf.Approximately(_targetAlpha, 0) ? 1 : 0;
                var fadeEvent = Mathf.Approximately(_targetAlpha, 0) ? onStartFadeOut : onStartFadeIn;
                fadeEvent?.Invoke();
                StartCoroutine(FadeToTarget());
                break;
            }
        }
    }

    private void UpdateFadeDirection()
    {
        _material.SetInt(Direction, (int)fadeDirection);
    }

    private void OnPassthroughLayerResumed(OVRPassthroughLayer passthroughLayer)
    {
        _meshRenderer.enabled = true;
        _targetAlpha = 1;

        StopAllCoroutines();
        StartCoroutine(FadeToTarget());
    }

    private IEnumerator FadeToTarget()
    {
        var currentAlpha = _material.GetFloat(InvertedAlpha);
        while (Mathf.Abs(currentAlpha - _targetAlpha) > 0)
        {
            currentAlpha = Mathf.MoveTowards(currentAlpha, _targetAlpha, fadeSpeed * Time.deltaTime);
            _material.SetFloat(InvertedAlpha, currentAlpha);
            yield return null;
        }

        if (Mathf.Abs(_targetAlpha - 1) < FadeTolerance)
        {
            _mainCamera.clearFlags = CameraClearFlags.SolidColor;
            _mainCamera.backgroundColor = Color.clear;

            onFadeInComplete?.Invoke();
        }
        else
        {
            oVRPassthroughLayer.enabled = false;
            _mainCamera.clearFlags = CameraClearFlags.Skybox;
            _mainCamera.backgroundColor = _skyboxBackgroundColor;

            onFadeOutComplete?.Invoke();
        }

        _meshRenderer.enabled = false;
    }
}
