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

#if FUSION2
using System;
using TMPro;
using Fusion;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;

/// <summary>
/// Handles user interactions with video player controls, such as play/pause, volume, settings, and timeline adjustments,
/// and synchronizes these states across a networked multiplayer environment.
/// </summary>
public class MovieControlsHandlerMotif : NetworkBehaviour, IStateAuthorityChanged
{
    /// <summary>
    /// The current value of the volume slider.
    /// </summary>
    [Networked] private float VolumeSliderValue { get; set; }

    /// <summary>
    /// The current value of the timeline slider.
    /// </summary>
    [Networked] private float TimelineSliderValue { get; set; }

    /// <summary>
    /// The current playback time of the video player.
    /// </summary>
    [Networked] private float CurrentTime { get; set; }

    /// <summary>
    /// The current value of viewport content below the controls.
    /// </summary>
    [Networked] private float TileViewportValue { get; set; }

    /// <summary>
    /// The play/pause state of the video.
    /// </summary>
    [Networked] private NetworkBool PlayPauseState { get; set; }

    /// <summary>
    /// The state of the volume button (shown or hidden).
    /// </summary>
    [Networked] private NetworkBool VolumeButtonState { get; set; }

    /// <summary>
    /// The state of the settings button (shown or hidden).
    /// </summary>
    [Networked] private NetworkBool SettingsButtonState { get; set; }

    [Header("UI Elements")]
    [Tooltip("Reference to the volume slider UI element.")]
    [SerializeField] private Slider volumeSlider;

    [Tooltip("Reference to the timeline slider UI element.")]
    [SerializeField] private Slider timelineSlider;

    [Tooltip("Rect Transform of the viewport content.")]
    [SerializeField] private RectTransform viewportContent;

    [Tooltip("Reference to the play/pause button UI element.")]
    [SerializeField] private Button playPauseButton;

    [Tooltip("Reference to the skip 10 seconds button.")]
    [SerializeField] private Button skipButton;

    [Tooltip("Reference to the reverse 10 seconds button.")]
    [SerializeField] private Button reverseButton;

    [Tooltip("Reference to the settings button UI element.")]
    [SerializeField] private Button settingsButton;

    [Tooltip("Reference to the volume button UI element.")]
    [SerializeField] private Button volumeButton;

    [Tooltip("Canvas group for controlling the visibility of the volume slider.")]
    [SerializeField] private CanvasGroup volumeSliderCanvasGroup;

    [Tooltip("Canvas group for controlling the visibility of the settings menu.")]
    [SerializeField] private CanvasGroup settingsMenuCanvasGroup;

    [Header("Icons and Labels")]
    [Tooltip("The sprite used when the video is paused.")]
    [SerializeField] private Sprite playIcon;

    [Tooltip("The sprite used when the video is playing.")]
    [SerializeField] private Sprite pauseIcon;

    [Tooltip("Reference to the image component of the play/pause button.")]
    [SerializeField] private Image playButtonImage;

    [Tooltip("Reference to the left-side label displaying the current time.")]
    [SerializeField] private TextMeshProUGUI leftLabel;

    [Tooltip("Reference to the right-side label displaying the total duration.")]
    [SerializeField] private TextMeshProUGUI rightLabel;

    [Header("Video Player")]
    [Tooltip("The video player component for controlling movie playback.")]
    [SerializeField] private VideoPlayer videoPlayer;

    private Animator _playPauseButtonAnimator;
    private Animator _skipButtonAnimator;
    private Animator _reverseButtonAnimator;
    private Animator _settingsButtonAnimator;
    private Animator _volumeButtonAnimator;
    private Animator _timelineSliderAnimator;
    private Animator _volumeSliderAnimator;
    private bool _isPlaying;
    private bool _hasSpawned;
    private const float UIFadeDuration = 0.35f;

    /// <summary>
    /// Enum representing the pending actions for control state updates after
    /// the State Authority has been changed.
    /// </summary>
    private enum PendingAction
    {
        None,
        PlayPauseToggle,
        VolumeButtonToggle,
        SettingsButtonToggle,
        TimelineSliderChange,
        VolumeSliderChange,
        TileViewportChange
    }

    private PendingAction _pendingAction = PendingAction.None;
    private float _pendingSliderValue;

    public override void Spawned()
    {
        base.Spawned();
        _hasSpawned = true;
        GetTotalDuration();
        InitializeControls();
        SetInitialPlayPauseState();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);
        playPauseButton.onClick.RemoveListener(OnPlayPauseButtonClick);
        skipButton.onClick.RemoveListener(SkipTenSeconds);
        reverseButton.onClick.RemoveListener(ReverseTenSeconds);
        settingsButton.onClick.RemoveListener(OnSettingsButtonClick);
        volumeButton.onClick.RemoveListener(OnVolumeButtonClick);
        timelineSlider.onValueChanged.RemoveListener(OnTimelineSliderValueChanged);
        volumeSlider.onValueChanged.RemoveListener(OnVolumeSliderValueChanged);
    }

    private void GetTotalDuration()
    {
        var totalDuration = (float)videoPlayer.clip.length;
        rightLabel.text = FormatTime(totalDuration);
        timelineSlider.maxValue = 1f;
    }

    private void InitializeControls()
    {
        _playPauseButtonAnimator = playPauseButton.GetComponent<Animator>();
        _skipButtonAnimator = skipButton.GetComponent<Animator>();
        _reverseButtonAnimator = reverseButton.GetComponent<Animator>();
        _settingsButtonAnimator = settingsButton.GetComponent<Animator>();
        _volumeButtonAnimator = volumeButton.GetComponent<Animator>();
        _timelineSliderAnimator = timelineSlider.GetComponent<Animator>();
        _volumeSliderAnimator = volumeSlider.GetComponent<Animator>();

        playPauseButton.onClick.AddListener(OnPlayPauseButtonClick);
        skipButton.onClick.AddListener(SkipTenSeconds);
        reverseButton.onClick.AddListener(ReverseTenSeconds);
        settingsButton.onClick.AddListener(OnSettingsButtonClick);
        volumeButton.onClick.AddListener(OnVolumeButtonClick);

        timelineSlider.onValueChanged.AddListener(OnTimelineSliderValueChanged);
        volumeSlider.onValueChanged.AddListener(OnVolumeSliderValueChanged);

        SetCanvasGroupVisibility(volumeSliderCanvasGroup, VolumeButtonState);
        SetCanvasGroupVisibility(settingsMenuCanvasGroup, SettingsButtonState);

        if (Object.HasStateAuthority)
        {
            VolumeSliderValue = videoPlayer.GetDirectAudioVolume(0);
        }

        videoPlayer.SetDirectAudioVolume(0, VolumeSliderValue);
        volumeSlider.SetValueWithoutNotify(VolumeSliderValue);

        if (Object.HasStateAuthority)
        {
            TileViewportValue = viewportContent.anchoredPosition.x;
        }
    }

    private void SetInitialPlayPauseState()
    {
        if (Object.HasStateAuthority)
        {
            _isPlaying = videoPlayer.isPlaying;
            PlayPauseState = _isPlaying;
        }

        UpdatePlayPauseState(PlayPauseState);
        videoPlayer.time = CurrentTime + 0.5f;
    }

    private void Update()
    {
        if (!_hasSpawned)
        {
            return;
        }

        UpdateTimelineAndLabels();
        UpdateViewportPosition();
    }

    private void UpdateTimelineAndLabels()
    {
        if (Object.HasStateAuthority)
        {
            var currentTime = (float)videoPlayer.time;
            CurrentTime = currentTime;
        }

        var totalDuration = (float)videoPlayer.clip.length;
        var sliderValue = CurrentTime / totalDuration;

        timelineSlider.SetValueWithoutNotify(sliderValue);
        leftLabel.text = FormatTime(CurrentTime);
    }

    private void UpdateViewportPosition()
    {
        if (Object.HasStateAuthority)
        {
            TileViewportValue = viewportContent.anchoredPosition.x;
        }

        viewportContent.anchoredPosition = new Vector2(TileViewportValue, viewportContent.anchoredPosition.y);
    }

    private string FormatTime(float seconds)
    {
        var minutes = Mathf.FloorToInt(seconds / 60);
        var secs = Mathf.FloorToInt(seconds % 60);
        return $"{minutes:D2}:{secs:D2}";
    }

    private void OnPlayPauseButtonClick()
    {
        if (Object.HasStateAuthority)
        {
            TogglePlayPauseState();
            RPC_UpdatePlayPauseState(PlayPauseState);
            RPC_TriggerAnimation(nameof(playPauseButton), "Pressed");
        }
        else
        {
            _pendingAction = PendingAction.PlayPauseToggle;
            Object.RequestStateAuthority();
        }
    }

    private void SkipTenSeconds()
    {
        if (Object.HasStateAuthority)
        {
            var totalDuration = (float)videoPlayer.clip.length;
            var newTime = Mathf.Min((float)videoPlayer.time + 10f, totalDuration);
            videoPlayer.time = newTime;
            CurrentTime = newTime;
            var value = CurrentTime / totalDuration;

            RPC_UpdateTimelineSliderValue(value);
            RPC_TriggerAnimation(nameof(skipButton), "Pressed");
        }
        else
        {
            _pendingAction = PendingAction.TimelineSliderChange;
            Object.RequestStateAuthority();
        }
    }

    private void ReverseTenSeconds()
    {
        if (Object.HasStateAuthority)
        {
            var newTime = Mathf.Max((float)videoPlayer.time - 10f, 0f);
            videoPlayer.time = newTime;
            CurrentTime = newTime;
            var value = CurrentTime / (float)videoPlayer.clip.length;

            RPC_UpdateTimelineSliderValue(value);
            RPC_TriggerAnimation(nameof(reverseButton), "Pressed");
        }
        else
        {
            _pendingAction = PendingAction.TimelineSliderChange;
            Object.RequestStateAuthority();
        }
    }

    private void OnSettingsButtonClick()
    {
        if (Object.HasStateAuthority)
        {
            SettingsButtonState = !SettingsButtonState;
            RPC_UpdateSettingsButtonState(SettingsButtonState);
            RPC_TriggerAnimation(nameof(settingsButton), "Pressed");
        }
        else
        {
            _pendingAction = PendingAction.SettingsButtonToggle;
            Object.RequestStateAuthority();
        }
    }

    private void OnVolumeButtonClick()
    {
        if (Object.HasStateAuthority)
        {
            VolumeButtonState = !VolumeButtonState;
            RPC_UpdateVolumeButtonState(VolumeButtonState);
            RPC_TriggerAnimation(nameof(volumeButton), "Pressed");
        }
        else
        {
            _pendingAction = PendingAction.VolumeButtonToggle;
            Object.RequestStateAuthority();
        }
    }

    private void OnTimelineSliderValueChanged(float value)
    {
        if (Object.HasStateAuthority)
        {
            TimelineSliderValue = value;
            RPC_UpdateTimelineSliderValue(TimelineSliderValue);
            RPC_TriggerAnimation(nameof(timelineSlider), "Pressed");
        }
        else
        {
            _pendingAction = PendingAction.TimelineSliderChange;
            _pendingSliderValue = value;
            Object.RequestStateAuthority();
        }
    }

    private void OnVolumeSliderValueChanged(float value)
    {
        if (Object.HasStateAuthority)
        {
            VolumeSliderValue = value;
            RPC_UpdateVolumeSliderValue(VolumeSliderValue);
            RPC_TriggerAnimation(nameof(volumeSlider), "Pressed");
        }
        else
        {
            _pendingAction = PendingAction.VolumeSliderChange;
            _pendingSliderValue = value;
            Object.RequestStateAuthority();
        }
    }

    /// <summary>
    /// This method comes from the <see cref="IStateAuthorityChanged"/> interface and is called
    /// when the State Authority has been changed. We use to execute commands immediatley
    /// after StateAuthority has been assigned to the client who tried to make an input on the movie.
    /// </summary>
    public void StateAuthorityChanged()
    {
        if (!Object.HasStateAuthority) return;
        switch (_pendingAction)
        {
            case PendingAction.PlayPauseToggle:
                TogglePlayPauseState();
                RPC_UpdatePlayPauseState(PlayPauseState);
                break;
            case PendingAction.VolumeButtonToggle:
                VolumeButtonState = !VolumeButtonState;
                RPC_UpdateVolumeButtonState(VolumeButtonState);
                break;
            case PendingAction.SettingsButtonToggle:
                SettingsButtonState = !SettingsButtonState;
                RPC_UpdateSettingsButtonState(SettingsButtonState);
                break;
            case PendingAction.TimelineSliderChange:
                TimelineSliderValue = _pendingSliderValue;
                RPC_UpdateTimelineSliderValue(TimelineSliderValue);
                break;
            case PendingAction.VolumeSliderChange:
                VolumeSliderValue = _pendingSliderValue;
                RPC_UpdateVolumeSliderValue(VolumeSliderValue);
                break;
            case PendingAction.TileViewportChange:
                TileViewportValue = _pendingSliderValue;
                RPC_UpdateViewportPosition(TileViewportValue);
                break;
            case PendingAction.None:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        var controlName = GetControlName(_pendingAction);
        if (!string.IsNullOrEmpty(controlName))
        {
            var animator = GetAnimatorByName(controlName);
            if (animator != null)
            {
                RPC_TriggerAnimation(controlName, "Pressed");
            }
        }

        _pendingAction = PendingAction.None;
        _pendingSliderValue = 0f;
    }

    private void TogglePlayPauseState()
    {
        PlayPauseState = !PlayPauseState;
        UpdatePlayPauseState(PlayPauseState);
    }

    private string GetControlName(PendingAction action)
    {
        return action switch
        {
            PendingAction.PlayPauseToggle => nameof(playPauseButton),
            PendingAction.VolumeButtonToggle => nameof(volumeButton),
            PendingAction.SettingsButtonToggle => nameof(settingsButton),
            PendingAction.TimelineSliderChange => nameof(timelineSlider),
            PendingAction.VolumeSliderChange => nameof(volumeSlider),
            _ => null
        };
    }

    private Animator GetAnimatorByName(string controlName)
    {
        return controlName switch
        {
            nameof(playPauseButton) => _playPauseButtonAnimator,
            nameof(skipButton) => _skipButtonAnimator,
            nameof(reverseButton) => _reverseButtonAnimator,
            nameof(settingsButton) => _settingsButtonAnimator,
            nameof(volumeButton) => _volumeButtonAnimator,
            nameof(timelineSlider) => _timelineSliderAnimator,
            nameof(volumeSlider) => _volumeSliderAnimator,
            _ => null
        };
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdatePlayPauseState(NetworkBool isPlaying)
    {
        UpdatePlayPauseState(isPlaying);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateVolumeButtonState(NetworkBool isVisible)
    {
        SetCanvasGroupVisibility(volumeSliderCanvasGroup, isVisible);
        VolumeButtonState = isVisible;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateSettingsButtonState(NetworkBool isVisible)
    {
        SetCanvasGroupVisibility(settingsMenuCanvasGroup, isVisible);
        SettingsButtonState = isVisible;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateTimelineSliderValue(float value)
    {
        UpdateTimelineSlider(value);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateVolumeSliderValue(float value)
    {
        UpdateVolumeSlider(value);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateViewportPosition(float value)
    {
        TileViewportValue = value;
        UpdateViewportPosition();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)]
    private void RPC_TriggerAnimation(string controlName, string triggerName)
    {
        var animator = GetAnimatorByName(controlName);
        if (animator != null)
        {
            TriggerAnimation(animator, triggerName);
        }
    }

    private void SetCanvasGroupVisibility(CanvasGroup canvasGroup, bool isVisible)
    {
        var startAlpha = canvasGroup.alpha;
        var endAlpha = isVisible ? 1f : 0f;

        StartCoroutine(FadeCanvasGroup(canvasGroup, startAlpha, endAlpha));
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float startAlpha, float endAlpha)
    {
        var elapsedTime = 0f;
        while (elapsedTime < UIFadeDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / UIFadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
    }

    private void UpdatePlayPauseState(NetworkBool playing)
    {
        if (playing)
        {
            videoPlayer.Play();
            playButtonImage.sprite = pauseIcon;
        }
        else
        {
            videoPlayer.Pause();
            playButtonImage.sprite = playIcon;
        }
    }

    private void UpdateTimelineSlider(float value)
    {
        TimelineSliderValue = value;
        var totalDuration = (float)videoPlayer.clip.length;
        videoPlayer.time = value * totalDuration;
        leftLabel.text = FormatTime((float)videoPlayer.time);
        timelineSlider.SetValueWithoutNotify(value);
    }

    private void UpdateVolumeSlider(float value)
    {
        VolumeSliderValue = value;
        videoPlayer.SetDirectAudioVolume(0, VolumeSliderValue);
        volumeSlider.SetValueWithoutNotify(VolumeSliderValue);
    }

    private void TriggerAnimation(Animator animator, string triggerName)
    {
        animator.SetTrigger(triggerName);
        StartCoroutine(ResetTriggerAfterDelay(animator, "Normal", UIFadeDuration));
    }

    private IEnumerator ResetTriggerAfterDelay(Animator animator, string triggerName, float delay)
    {
        yield return new WaitForSeconds(delay);
        animator.SetTrigger(triggerName);
    }

    /// <summary>
    /// We expose this method, so it can be called by other classes or UI events,
    /// to more easily request StateAuthority for additional commands or controls.
    /// </summary>
    public void RequestCanvasStateAuthority()
    {
        if (_hasSpawned && !Object.HasStateAuthority)
        {
            Object.RequestStateAuthority();
        }
    }
}
#endif
