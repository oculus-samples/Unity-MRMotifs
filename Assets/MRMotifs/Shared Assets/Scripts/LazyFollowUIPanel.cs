// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using System.Collections;
using Meta.XR.Samples;

namespace MRMotifs.SharedAssets
{
    [MetaCodeSample("MRMotifs-SharedAssets")]
    public class LazyFollowUIPanel : MonoBehaviour
    {
        [Header("Panel Controls")]
        [Tooltip("The speed at which the panel moves towards the target position and rotation.")]
        [SerializeField]
        private float followSpeed = 0.75f;

        [Tooltip("The distance in front of the camera where the panel will position itself.")]
        [SerializeField]
        private float distance = 0.75f;

        [Tooltip("The tilt angle applied to the panel relative to the forward direction.")]
        [SerializeField]
        private float tiltAngle = -30f;

        [Tooltip("The vertical offset applied to the panel's position.")]
        [SerializeField]
        private float yOffset = -0.3f;

        [Tooltip("The duration over which the panel smoothly slows down to a stop upon entering the camera's view.")]
        [SerializeField]
        private float slowdownDuration = 1f;

        [Tooltip("The current speed factor used to control the panel's movement speed (for debugging purposes).")]
        [SerializeField]
        private float speedFactor = 1f;

        private Camera m_mainCamera;
        private Transform m_headTransform;
        private Coroutine m_slowdownCoroutine;
        private Vector3 m_targetPosition;
        private Quaternion m_targetRotation;

        private void Awake()
        {
            m_mainCamera = Camera.main;

            if (m_mainCamera != null)
            {
                m_headTransform = m_mainCamera.transform;
            }
            else
            {
                StartCoroutine(FetchMainCamera());
            }
        }

        private IEnumerator FetchMainCamera()
        {
            while (!(m_mainCamera = Camera.main))
            {
                yield return null;
            }

            m_headTransform = m_mainCamera.transform;
        }

        private void Update()
        {
            if (!m_mainCamera)
            {
                return;
            }

            var viewportPoint = m_mainCamera.WorldToViewportPoint(transform.position);
            var inFrustum = viewportPoint is { z: > 0, x: >= 0 and <= 1, y: >= 0 and <= 1 };
            var distanceToCamera = Vector3.Distance(m_headTransform.position, transform.position);

            if (inFrustum && distanceToCamera < distance)
            {
                m_slowdownCoroutine ??= StartCoroutine(SlowDown());
            }
            else
            {
                if (m_slowdownCoroutine != null)
                {
                    StopCoroutine(m_slowdownCoroutine);
                    m_slowdownCoroutine = null;
                }

                speedFactor = 1f;
            }

            var forward = m_headTransform.forward;
            forward.y = 0;
            forward.Normalize();

            m_targetPosition = m_headTransform.position + forward * distance + Vector3.up * yOffset;
            m_targetRotation = Quaternion.LookRotation(forward) * Quaternion.Euler(-tiltAngle, 0, 0);

            transform.position = Vector3.Lerp(
                transform.position, m_targetPosition, followSpeed * speedFactor * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, m_targetRotation, followSpeed * speedFactor * Time.deltaTime);
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
            m_slowdownCoroutine = null;
        }
    }
}
