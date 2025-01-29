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
using System.Collections;

public class LazyFollowUIPanel : MonoBehaviour
{
    [Header("Panel Controls")]

    [Tooltip("The speed at which the panel moves towards the target position and rotation.")]
    [SerializeField] private float followSpeed = 0.75f;

    [Tooltip("The distance in front of the camera where the panel will position itself.")]
    [SerializeField] private float distance = 0.75f;

    [Tooltip("The tilt angle applied to the panel relative to the forward direction.")]
    [SerializeField] private float tiltAngle = -30f;

    [Tooltip("The vertical offset applied to the panel's position.")]
    [SerializeField] private float yOffset = -0.3f;

    [Tooltip("The duration over which the panel smoothly slows down to a stop upon entering the camera's view.")]
    [SerializeField] private float slowdownDuration = 1f;

    [Tooltip("The current speed factor used to control the panel's movement speed (for debugging purposes).")]
    [SerializeField] private float speedFactor = 1f;

    private Camera _mainCamera;
    private Transform _headTransform;
    private Coroutine _slowdownCoroutine;
    private Vector3 _targetPosition;
    private Quaternion _targetRotation;

    private void Awake()
    {
        _mainCamera = Camera.main;

        if (_mainCamera != null)
        {
            _headTransform = _mainCamera.transform;
        }
        else
        {
            StartCoroutine(FetchMainCamera());
        }
    }

    private IEnumerator FetchMainCamera()
    {
        while (!(_mainCamera = Camera.main))
        {
            yield return null;
        }

        _headTransform = _mainCamera.transform;
    }

    private void Update()
    {
        if (!_mainCamera)
        {
            return;
        }

        var viewportPoint = _mainCamera.WorldToViewportPoint(transform.position);
        var inFrustum = viewportPoint is { z: > 0, x: >= 0 and <= 1, y: >= 0 and <= 1 };
        var distanceToCamera = Vector3.Distance(_headTransform.position, transform.position);

        if (inFrustum && distanceToCamera < distance)
        {
            _slowdownCoroutine ??= StartCoroutine(SlowDown());
        }
        else
        {
            if (_slowdownCoroutine != null)
            {
                StopCoroutine(_slowdownCoroutine);
                _slowdownCoroutine = null;
            }

            speedFactor = 1f;
        }

        var forward = _headTransform.forward;
        forward.y = 0;
        forward.Normalize();

        _targetPosition = _headTransform.position + forward * distance + Vector3.up * yOffset;
        _targetRotation = Quaternion.LookRotation(forward) * Quaternion.Euler(-tiltAngle, 0, 0);

        transform.position = Vector3.Lerp(transform.position, _targetPosition, followSpeed * speedFactor * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, followSpeed * speedFactor * Time.deltaTime);
    }

    private IEnumerator SlowDown()
    {
        var elapsed = 0f;
        var initialSpeed = speedFactor;

        while (elapsed < slowdownDuration)
        {
            elapsed += Time.deltaTime;
            speedFactor = Mathf.Lerp(initialSpeed, 0f, elapsed / slowdownDuration);
            yield return null;
        }

        speedFactor = 0f;
        _slowdownCoroutine = null;
    }
}
