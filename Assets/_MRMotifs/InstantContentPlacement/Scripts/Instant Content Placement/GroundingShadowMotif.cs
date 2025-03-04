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
using Meta.XR;

/// <summary>
/// Projects a realistic shadow of a target object onto detected surfaces beneath it.
/// The shadow adjusts its position, size, and opacity based on the target’s proximity to the surface.
/// </summary>
public class GroundingShadowMotif : MonoBehaviour
{
    [Tooltip("Prefab for the grounding shadow.")]
    [SerializeField] private GameObject groundingShadowPrefab;

    private bool _enableShadowUpdates = true;

    private const float SmoothTime = 0.05f;
    private const float ShadowScaler = 1.6f;
    private const float MaxShadowTint = 0.8f;
    private const float ShadowMaxDistance = 2.0f;
    private const float ShadowMinDistance = 0.05f;

    private Vector2 _initialShadowSize;
    private Vector3 _shadowPositionVelocity;

    private Transform _trackedObject;
    private Transform _groundingShadow;
    private Collider _trackedObjectCollider;
    private SpriteRenderer _groundingShadowRenderer;
    private EnvironmentRaycastManager _raycastManager;

    private void Awake()
    {
        _trackedObject = transform;
        _trackedObjectCollider = _trackedObject.GetComponent<Collider>();

        _raycastManager = FindAnyObjectByType<EnvironmentRaycastManager>();
        if (_raycastManager == null)
        {
            Debug.LogError("EnvironmentRaycastManager not found in the scene. Ensure it is added to the scene.");
        }

        InstantiateGroundingShadow();
    }

    private void InstantiateGroundingShadow()
    {
        _groundingShadow = Instantiate(groundingShadowPrefab).transform;
        _groundingShadowRenderer = _groundingShadow.GetComponent<SpriteRenderer>();
        _initialShadowSize = new Vector2(_trackedObjectCollider.bounds.size.x, _trackedObjectCollider.bounds.size.z);
        _groundingShadowRenderer.size = _initialShadowSize;
    }

    private void Update()
    {
        if (!_enableShadowUpdates)
        {
            return;
        }

        UpdateGroundingShadow();
    }

    private void UpdateGroundingShadow()
    {
        var downwardRay = new Ray(_trackedObject.position, Vector3.down);
        if (!_raycastManager.Raycast(downwardRay, out var shadowHitInfo)) return;

        var distanceToSurface = Vector3.Distance(_trackedObject.position, shadowHitInfo.point);

        if (distanceToSurface < ShadowMinDistance)
        {
            _groundingShadowRenderer.enabled = false;
            return;
        }

        _groundingShadowRenderer.enabled = true;

        var targetShadowPosition = new Vector3(_trackedObject.position.x, shadowHitInfo.point.y + 0, _trackedObject.position.z);
        _groundingShadow.position = Vector3.SmoothDamp(_groundingShadow.position, targetShadowPosition, ref _shadowPositionVelocity, SmoothTime);
        _groundingShadow.rotation = Quaternion.Euler(90, _trackedObject.eulerAngles.y, 0);

        var distanceFactor = Mathf.InverseLerp(ShadowMaxDistance, 0, distanceToSurface);
        var targetSize = Vector2.Lerp(_initialShadowSize * ShadowScaler, _initialShadowSize, distanceFactor);
        _groundingShadowRenderer.size = targetSize;

        var alpha = Mathf.Clamp01(distanceFactor) * MaxShadowTint;
        var currentColor = _groundingShadowRenderer.color;

        currentColor.a = alpha;
        _groundingShadowRenderer.color = currentColor;
    }

    /// <summary>
    /// When enabled, the shadow position and opacity will be updated in real-time based on the object's proximity to surfaces.
    /// When disabled, the shadow remains static in its last position.
    /// </summary>
    /// <param name="enable">If true, enables shadow updates; if false, disables them.</param>
    public void EnableShadowUpdates(bool enable)
    {
        _enableShadowUpdates = enable;
    }
}
