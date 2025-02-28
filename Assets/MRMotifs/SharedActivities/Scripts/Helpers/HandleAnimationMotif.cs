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
using Oculus.Interaction;
using UnityEngine;

/// <summary>
/// Manages scaling and alpha transitions for an object over time, using a coroutine to animate the object smoothly.
/// </summary>
public class HandleAnimationMotif : MonoBehaviour
{
    [Tooltip("Interactable Unity Event Wrapper to call the animation methods from.")]
    [SerializeField] private InteractableUnityEventWrapper interactableUnityEventWrapper;

    [Tooltip("The target scale of the object when scaling up.")]
    [SerializeField] private Vector3 targetScale = new(0.03f, 0.25f, 0.03f);

    [Tooltip("The duration (in seconds) it takes to scale the object.")]
    [SerializeField] private float scalingDuration = 0.5f;

    [Tooltip("The material used to modify the object's alpha transparency.")]
    [SerializeField] private Material material;

    private Vector3 _initialScale;
    private Coroutine _currentCoroutine;

    private void Awake()
    {
        _initialScale = transform.localScale;
        interactableUnityEventWrapper.WhenHover.AddListener(ScaleUp);
        interactableUnityEventWrapper.WhenUnhover.AddListener(ScaleDown);
    }

    private void OnDestroy()
    {
        interactableUnityEventWrapper.WhenHover.RemoveListener(ScaleUp);
        interactableUnityEventWrapper.WhenUnhover.RemoveListener(ScaleDown);
    }

    private void ScaleUp()
    {
        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
        }

        _currentCoroutine = StartCoroutine(ScaleObject(targetScale, 180));
    }

    private void ScaleDown()
    {
        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
        }

        _currentCoroutine = StartCoroutine(ScaleObject(_initialScale, 0));
    }

    private IEnumerator ScaleObject(Vector3 target, float targetAlpha)
    {
        var startScale = transform.localScale;
        var startAlpha = material.color.a * 255f;
        float timeElapsed = 0;

        while (timeElapsed < scalingDuration)
        {
            var t = timeElapsed / scalingDuration;
            transform.localScale = Vector3.Lerp(startScale, target, t);
            SetMaterialAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = target;
        SetMaterialAlpha(targetAlpha);
    }

    private void SetMaterialAlpha(float alpha)
    {
        var color = material.color;
        color.a = alpha / 255f;
        material.color = color;
    }
}
