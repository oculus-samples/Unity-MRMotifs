// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using Meta.XR;

namespace MRMotifs.InstantContentPlacement.Placement
{
    /// <summary>
    /// Projects a realistic shadow of a target object onto detected surfaces beneath it.
    /// The shadow adjusts its position, size, and opacity based on the target’s proximity to the surface.
    /// </summary>
    public class GroundingShadowMotif : MonoBehaviour
    {
        [Tooltip("Prefab for the grounding shadow.")]
        [SerializeField]
        private GameObject groundingShadowPrefab;

        private bool m_enableShadowUpdates = true;

        private const float SMOOTH_TIME = 0.05f;
        private const float SHADOW_SCALER = 1.6f;
        private const float MAX_SHADOW_TINT = 0.8f;
        private const float SHADOW_MAX_DISTANCE = 2.0f;
        private const float SHADOW_MIN_DISTANCE = 0.05f;

        private Vector2 m_initialShadowSize;
        private Vector3 m_shadowPositionVelocity;

        private Transform m_trackedObject;
        private Transform m_groundingShadow;
        private Collider m_trackedObjectCollider;
        private SpriteRenderer m_groundingShadowRenderer;
        private EnvironmentRaycastManager m_raycastManager;

        private void Awake()
        {
            m_trackedObject = transform;
            m_trackedObjectCollider = m_trackedObject.GetComponent<Collider>();

            m_raycastManager = FindAnyObjectByType<EnvironmentRaycastManager>();
            if (m_raycastManager == null)
            {
                Debug.LogError("EnvironmentRaycastManager not found in the scene. Ensure it is added to the scene.");
            }

            InstantiateGroundingShadow();
        }

        private void InstantiateGroundingShadow()
        {
            m_groundingShadow = Instantiate(groundingShadowPrefab).transform;
            m_groundingShadowRenderer = m_groundingShadow.GetComponent<SpriteRenderer>();
            m_initialShadowSize = new Vector2(
                m_trackedObjectCollider.bounds.size.x, m_trackedObjectCollider.bounds.size.z);
            m_groundingShadowRenderer.size = m_initialShadowSize;
        }

        private void Update()
        {
            if (!m_enableShadowUpdates)
            {
                return;
            }

            UpdateGroundingShadow();
        }

        private void UpdateGroundingShadow()
        {
            var downwardRay = new Ray(m_trackedObject.position, Vector3.down);
            if (!m_raycastManager.Raycast(downwardRay, out var shadowHitInfo)) return;

            var distanceToSurface = Vector3.Distance(m_trackedObject.position, shadowHitInfo.point);

            if (distanceToSurface < SHADOW_MIN_DISTANCE)
            {
                m_groundingShadowRenderer.enabled = false;
                return;
            }

            m_groundingShadowRenderer.enabled = true;

            var targetShadowPosition = new Vector3(
                m_trackedObject.position.x, shadowHitInfo.point.y + 0, m_trackedObject.position.z);
            m_groundingShadow.position = Vector3.SmoothDamp(
                m_groundingShadow.position, targetShadowPosition, ref m_shadowPositionVelocity, SMOOTH_TIME);
            m_groundingShadow.rotation = Quaternion.Euler(90, m_trackedObject.eulerAngles.y, 0);

            var distanceFactor = Mathf.InverseLerp(SHADOW_MAX_DISTANCE, 0, distanceToSurface);
            var targetSize = Vector2.Lerp(m_initialShadowSize * SHADOW_SCALER, m_initialShadowSize, distanceFactor);
            m_groundingShadowRenderer.size = targetSize;

            var alpha = Mathf.Clamp01(distanceFactor) * MAX_SHADOW_TINT;
            var currentColor = m_groundingShadowRenderer.color;

            currentColor.a = alpha;
            m_groundingShadowRenderer.color = currentColor;
        }

        /// <summary>
        /// When enabled, the shadow position and opacity will be updated in real-time based on the object's proximity to surfaces.
        /// When disabled, the shadow remains static in its last position.
        /// </summary>
        /// <param name="enable">If true, enables shadow updates; if false, disables them.</param>
        public void EnableShadowUpdates(bool enable)
        {
            m_enableShadowUpdates = enable;
        }
    }
}
