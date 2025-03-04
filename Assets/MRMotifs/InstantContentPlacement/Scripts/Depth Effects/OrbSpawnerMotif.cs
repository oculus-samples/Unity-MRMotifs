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

using Meta.XR;
using static OVRInput;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbSpawnerMotif : MonoBehaviour
{
    [Tooltip("Handles environment raycasting for orbs placement.")]
    [SerializeField] private EnvironmentRaycastManager environmentRaycast;

    [Tooltip("Audio clip played when a scan is triggered.")]
    [SerializeField] private AudioClip scanTrigger;

    [Header("Orb Settings")]
    [Tooltip("Maximum orbs in the scene at a time.")]
    [SerializeField] private int maxOrbs = 5;

    [Tooltip("Force applied when launching an orb.")]
    [SerializeField] private float launchForce = 6.0f;

    [Tooltip("Prefab of the orb.")]
    [SerializeField] private ShockWaveOrbMotif orbMotifPrefab;

    [Tooltip("Prefab of the depth scan effect.")]
    [SerializeField] private AudioSource shockWaveEffectPrefab;

    [Header("Controller References")]
    [Tooltip("Reference to the left controller or hand.")]
    [SerializeField] private Controller leftController;

    [Tooltip("Reference to the right controller or hand.")]
    [SerializeField] private Controller rightController;

    [Tooltip("Button to launch an orb.")]
    [SerializeField] private Button launchOrbButton;

    [Tooltip("Button to detonate all active orbs.")]
    [SerializeField] private Button detonateAllOrbsButton;

    private Coroutine _detonationCoroutine;
    private readonly Vector3 _launchOffset = new(0, 0, 0.1f);
    private readonly List<ShockWaveOrbMotif> _spawnedOrbs = new();
    private float _pitchLevel = 1.0f;
    private Transform _cameraTransform;

    private void Awake()
    {
        if (Camera.main != null)
        {
            _cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        _pitchLevel = Mathf.Max(1.0f, _pitchLevel - Time.deltaTime);

        if (GetDown(launchOrbButton))
        {
            if (_spawnedOrbs.Count >= maxOrbs)
            {
                return;
            }

            LaunchOrb();
        }

        if (GetDown(detonateAllOrbsButton) && _detonationCoroutine == null)
        {
            TriggerDetonation();
        }
    }

    public void LaunchOrb()
    {
        var cameraTransform = _cameraTransform;
        var launchPosition = cameraTransform.position + cameraTransform.forward * _launchOffset.z;
        var launchDirection = cameraTransform.forward * launchForce;

        var orb = Instantiate(orbMotifPrefab, launchPosition, Quaternion.identity);
        orb.Initialize(environmentRaycast);
        orb.onDetonate.AddListener(HandleDepthScan);
        orb.Launch(launchDirection);

        _spawnedOrbs.Add(orb);
    }

    public void TriggerDetonation()
    {
        _detonationCoroutine = StartCoroutine(DetonateAllOrbs());
    }

    private IEnumerator DetonateAllOrbs()
    {
        var wait = new WaitForSeconds(0.1f);
        foreach (var shockWaveOrb in new List<ShockWaveOrbMotif>(_spawnedOrbs))
        {
            shockWaveOrb.Detonate();
            yield return wait;
        }
        _detonationCoroutine = null;
    }

    private void HandleDepthScan(Vector3 position, ShockWaveOrbMotif shockWaveOrbMotif)
    {
        var scanEffectAudioSrc = Instantiate(shockWaveEffectPrefab, position, Quaternion.identity);
        scanEffectAudioSrc.clip = scanTrigger;

        _pitchLevel = Mathf.Clamp(_pitchLevel + 0.25f, 1.0f, 3.0f);
        scanEffectAudioSrc.pitch = _pitchLevel;
        scanEffectAudioSrc.Play();

        _spawnedOrbs.Remove(shockWaveOrbMotif);
        Destroy(shockWaveOrbMotif.gameObject);
    }
}
