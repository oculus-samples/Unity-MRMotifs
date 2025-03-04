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
using Meta.XR;

/// <summary>
/// Positions and snaps an interactable object to the nearest detected surface upon release.
/// Uses ray casting to find horizontal surfaces below the object and smooths the object's position
/// and rotation towards the target surface if within a specified snap distance.
/// Displays a placement indicator and line from the object to the surface while grabbed and in range.
/// </summary>
public class SurfacePlacementMotif : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private InteractableUnityEventWrapper interactableUnityEventWrapper;

    [Header("Placement Settings")]
    [SerializeField] private float placementDistance = 0.4f;
    [SerializeField] private float hoverDistance = 0.1f;
    [SerializeField] private GameObject lineIndicatorPrefab;
    [SerializeField] private bool showLineIndicator = true;

    private bool _isGrabbed;
    private const float PlacementSmoothTime = 1.5f;
    private const float HitPointLagSmoothTime = 0.15f;

    private Vector3 _hitPoint;
    private Vector3 _targetPosition;
    private Vector3 _laggedHitPoint;
    private Vector3 _positionVelocity;
    private Quaternion _targetRotation;
    private Quaternion _rotationVelocity;

    private Transform _trackedObject;
    private Renderer _trackedObjectRenderer;
    private LineRenderer _lineRenderer;
    private Coroutine _placementCoroutine;
    private GroundingShadowMotif _groundingShadow;
    private EnvironmentRaycastManager _raycastManager;

    private void Awake()
    {
        _trackedObject = transform;
        _trackedObjectRenderer = _trackedObject.GetComponent<Renderer>();
        _groundingShadow = GetComponent<GroundingShadowMotif>();
        _raycastManager = FindAnyObjectByType<EnvironmentRaycastManager>();

        if (_raycastManager == null)
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
        _isGrabbed = true;

        if (_placementCoroutine != null)
        {
            StopCoroutine(_placementCoroutine);
        }

        if (_groundingShadow != null)
        {
            _groundingShadow.EnableShadowUpdates(true);
        }
    }

    private void OnUnselect()
    {
        _isGrabbed = false;

        if (!PerformRaycastAndSnap())
        {
            _targetPosition = _trackedObject.position;
            _targetRotation = _trackedObject.rotation;
        }

        UpdateIndicatorVisibility(false);
    }

    private void InitializeLineRenderer()
    {
        _lineRenderer = Instantiate(lineIndicatorPrefab).GetComponent<LineRenderer>();
        _lineRenderer.enabled = false;
    }

    private bool PerformRaycastAndSnap()
    {
        if (!_raycastManager.Raycast(new Ray(_trackedObject.position, Vector3.down), out var hitInfo))
        {
            return false;
        }

        var hitPoint = hitInfo.point;
        _targetPosition = new Vector3(hitPoint.x, hitPoint.y + hoverDistance, hitPoint.z);
        _targetRotation = Quaternion.Euler(0, _trackedObject.rotation.eulerAngles.y, 0);

        if (Vector3.Distance(_trackedObject.position, hitPoint) >= placementDistance)
        {
            if (_groundingShadow)
            {
                _groundingShadow.EnableShadowUpdates(false);
            }

            return false;
        }

        _placementCoroutine = StartCoroutine(SmoothMoveToTarget());
        return true;
    }

    private IEnumerator SmoothMoveToTarget()
    {
        var elapsedTime = 0f;

        var initialPosition = _trackedObject.position;
        var initialRotation = _trackedObject.rotation;

        while (elapsedTime < PlacementSmoothTime)
        {
            elapsedTime += Time.deltaTime;

            var t = elapsedTime / PlacementSmoothTime;
            var easedT = EaseOutExpo(t);

            _trackedObject.position = Vector3.Lerp(initialPosition, _targetPosition, easedT);
            _trackedObject.rotation = Quaternion.Slerp(initialRotation, _targetRotation, easedT);

            yield return null;
        }

        _trackedObject.position = _targetPosition;
        _trackedObject.rotation = _targetRotation;

        if (_groundingShadow)
        {
            _groundingShadow.EnableShadowUpdates(false);
        }
    }

    private static float EaseOutExpo(float x)
    {
        return Mathf.Approximately(x, 1) ? 1 : 1 - Mathf.Pow(2, -10 * x);
    }

    private void Update()
    {
        if (!_isGrabbed)
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
        if (!_raycastManager.Raycast(new Ray(_trackedObject.position, Vector3.down), out var hitInfo))
        {
            UpdateIndicatorVisibility(false);
            return;
        }

        _hitPoint = hitInfo.point;
        var distanceToSurface = Vector3.Distance(_trackedObject.position, _hitPoint);

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
        var bounds = _trackedObjectRenderer.bounds;
        var bottomPosition = bounds.center - new Vector3(0, bounds.extents.y, 0);

        _laggedHitPoint = Vector3.Lerp(_laggedHitPoint, _hitPoint, Time.deltaTime / HitPointLagSmoothTime);

        _lineRenderer.SetPosition(0, _laggedHitPoint);
        _lineRenderer.SetPosition(1, bottomPosition);
    }

    private void UpdateIndicatorVisibility(bool isVisible)
    {
        if (_lineRenderer)
        {
            _lineRenderer.enabled = isVisible;
        }
    }
}
