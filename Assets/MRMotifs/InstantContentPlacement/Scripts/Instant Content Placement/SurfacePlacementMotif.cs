// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Oculus.Interaction;
using UnityEngine;
using Meta.XR;

namespace MRMotifs.InstantContentPlacement.Placement
{
    /// <summary>
    /// Positions and snaps an interactable object to the nearest detected surface upon release.
    /// Uses ray casting to find horizontal surfaces below the object and smooths the object's position
    /// and rotation towards the target surface if within a specified snap distance.
    /// Displays a placement indicator and line from the object to the surface while grabbed and in range.
    /// </summary>
    public class SurfacePlacementMotif : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField]
        private InteractableUnityEventWrapper interactableUnityEventWrapper;

        [Header("Placement Settings")]
        [SerializeField]
        private float placementDistance = 0.4f;

        [SerializeField] private float hoverDistance = 0.1f;
        [SerializeField] private GameObject lineIndicatorPrefab;
        [SerializeField] private bool showLineIndicator = true;

        private bool m_isGrabbed;
        private const float PLACEMENT_SMOOTH_TIME = 1.5f;
        private const float HIT_POINT_LAG_SMOOTH_TIME = 0.15f;

        private Vector3 m_hitPoint;
        private Vector3 m_targetPosition;
        private Vector3 m_laggedHitPoint;
        private Vector3 m_positionVelocity;
        private Quaternion m_targetRotation;
        private Quaternion m_rotationVelocity;

        private Transform m_trackedObject;
        private LineRenderer m_lineRenderer;
        private Coroutine m_placementCoroutine;
        private Renderer m_trackedObjectRenderer;
        private GroundingShadowMotif m_groundingShadow;
        private EnvironmentRaycastManager m_raycastManager;

        private void Awake()
        {
            m_trackedObject = transform;
            m_trackedObjectRenderer = m_trackedObject.GetComponent<Renderer>();
            m_groundingShadow = GetComponent<GroundingShadowMotif>();
            m_raycastManager = FindAnyObjectByType<EnvironmentRaycastManager>();

            if (m_raycastManager == null)
            {
                Debug.LogError("EnvironmentRaycastManager not found in the scene.");
            }

            interactableUnityEventWrapper.WhenSelect.AddListener(OnSelect);
            interactableUnityEventWrapper.WhenUnselect.AddListener(OnUnselect);

            if (showLineIndicator && lineIndicatorPrefab)
            {
                InitializeLineRenderer();
            }
        }

        private void OnDestroy()
        {
            interactableUnityEventWrapper.WhenSelect.RemoveListener(OnSelect);
            interactableUnityEventWrapper.WhenUnselect.RemoveListener(OnUnselect);
        }

        private void OnSelect()
        {
            m_isGrabbed = true;

            if (m_placementCoroutine != null)
            {
                StopCoroutine(m_placementCoroutine);
            }

            if (m_groundingShadow != null)
            {
                m_groundingShadow.EnableShadowUpdates(true);
            }
        }

        private void OnUnselect()
        {
            m_isGrabbed = false;

            if (!PerformRaycastAndSnap())
            {
                m_targetPosition = m_trackedObject.position;
                m_targetRotation = m_trackedObject.rotation;
            }

            UpdateIndicatorVisibility(false);
        }

        private void InitializeLineRenderer()
        {
            m_lineRenderer = Instantiate(lineIndicatorPrefab).GetComponent<LineRenderer>();
            m_lineRenderer.enabled = false;
        }

        private bool PerformRaycastAndSnap()
        {
            if (!m_raycastManager.Raycast(new Ray(m_trackedObject.position, Vector3.down), out var hitInfo))
            {
                return false;
            }

            var hitPoint = hitInfo.point;
            m_targetPosition = new Vector3(hitPoint.x, hitPoint.y + hoverDistance, hitPoint.z);
            m_targetRotation = Quaternion.Euler(0, m_trackedObject.rotation.eulerAngles.y, 0);

            if (Vector3.Distance(m_trackedObject.position, hitPoint) >= placementDistance)
            {
                if (m_groundingShadow)
                {
                    m_groundingShadow.EnableShadowUpdates(false);
                }

                return false;
            }

            m_placementCoroutine = StartCoroutine(SmoothMoveToTarget());
            return true;
        }

        private IEnumerator SmoothMoveToTarget()
        {
            var elapsedTime = 0f;

            var initialPosition = m_trackedObject.position;
            var initialRotation = m_trackedObject.rotation;

            while (elapsedTime < PLACEMENT_SMOOTH_TIME)
            {
                elapsedTime += Time.deltaTime;

                var t = elapsedTime / PLACEMENT_SMOOTH_TIME;
                var easedT = EaseOutExpo(t);

                m_trackedObject.position = Vector3.Lerp(initialPosition, m_targetPosition, easedT);
                m_trackedObject.rotation = Quaternion.Slerp(initialRotation, m_targetRotation, easedT);

                yield return null;
            }

            m_trackedObject.position = m_targetPosition;
            m_trackedObject.rotation = m_targetRotation;

            if (m_groundingShadow)
            {
                m_groundingShadow.EnableShadowUpdates(false);
            }
        }

        private static float EaseOutExpo(float x)
        {
            return Mathf.Approximately(x, 1) ? 1 : 1 - Mathf.Pow(2, -10 * x);
        }

        private void Update()
        {
            if (!m_isGrabbed)
            {
                return;
            }

            UpdatePlacementIndicator();

            if (showLineIndicator)
            {
                UpdateLineRenderer();
            }
        }

        private void UpdatePlacementIndicator()
        {
            if (!m_raycastManager.Raycast(new Ray(m_trackedObject.position, Vector3.down), out var hitInfo))
            {
                UpdateIndicatorVisibility(false);
                return;
            }

            m_hitPoint = hitInfo.point;
            var distanceToSurface = Vector3.Distance(m_trackedObject.position, m_hitPoint);

            if (distanceToSurface >= hoverDistance && distanceToSurface < placementDistance)
            {
                UpdateIndicatorVisibility(true);
            }
            else
            {
                UpdateIndicatorVisibility(false);
            }
        }

        private void UpdateLineRenderer()
        {
            var bounds = m_trackedObjectRenderer.bounds;
            var bottomPosition = bounds.center - new Vector3(0, bounds.extents.y, 0);

            m_laggedHitPoint = Vector3.Lerp(m_laggedHitPoint, m_hitPoint, Time.deltaTime / HIT_POINT_LAG_SMOOTH_TIME);

            m_lineRenderer.SetPosition(0, m_laggedHitPoint);
            m_lineRenderer.SetPosition(1, bottomPosition);
        }

        private void UpdateIndicatorVisibility(bool isVisible)
        {
            if (m_lineRenderer)
            {
                m_lineRenderer.enabled = isVisible;
            }
        }
    }
}
