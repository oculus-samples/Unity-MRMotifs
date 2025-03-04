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
using UnityEngine.Events;
using Meta.XR;

/// <summary>
/// Represents a throwable orb that can be launched, attach to surfaces upon impact,
/// and trigger a scanning effect. The orb plays audio clips on spawn and when it sticks
/// to a surface. It integrates with an EnvironmentRaycastManager for collision detection.
/// When detonated, it invokes a UnityEvent to notify listeners of the event and destroys itself.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ShockWaveOrbMotif : MonoBehaviour
{
    [Header("Audio Feedback")]
    [Tooltip("Audio clip played when the orb is spawned.")]
    [SerializeField] private AudioClip spawnAudio;

    [Tooltip("Audio clip played when the orb sticks to a surface.")]
    [SerializeField] private AudioClip stickAudio;

    [Header("Collision Settings")]
    [Tooltip("Proximity threshold to detect collision with surfaces.")]
    [SerializeField] private float proximityCheckValue = 0.35f;

    [Tooltip("The Depth Relighting Effect on the orb to light up the surroundings.")]
    [SerializeField] private GameObject depthRelightingEffect;

    [Header("Events")]
    [Tooltip("Event triggered when the orb detonates.")]
    public UnityEvent<Vector3, ShockWaveOrbMotif> onDetonate;

    private bool _isLaunched;

    private Rigidbody _rb;
    private AudioSource _audioSource;
    private EnvironmentRaycastManager _raycastManager;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _rb = GetComponent<Rigidbody>();
        _isLaunched = false;
    }

    /// <summary>
    /// Sets up the orb with a reference to the raycast manager.
    /// </summary>
    /// <param name="raycastManager">Reference to the EnvironmentRaycastManager for collision detection.</param>
    public void Initialize(EnvironmentRaycastManager raycastManager)
    {
        _raycastManager = raycastManager;
        PlayAudio(spawnAudio);
    }

    /// <summary>
    /// Launches the orb with the specified force.
    /// </summary>
    /// <param name="force">Force vector applied to the orb on launch.</param>
    public void Launch(Vector3 force)
    {
        _rb.isKinematic = false;
        _rb.AddForce(force, ForceMode.Impulse);
        _isLaunched = true;
    }

    /// <summary>
    /// Detonates the orb, triggering any subscribed events and destroying the object.
    /// </summary>
    public void Detonate()
    {
        onDetonate?.Invoke(transform.position, this);
        Destroy(gameObject);
    }

    private void FixedUpdate()
    {
        if (!_isLaunched || _rb.velocity.magnitude <= 0)
        {
            return;
        }

        var ray = new Ray(transform.position, _rb.velocity.normalized);
        if (_raycastManager.Raycast(ray, out var hit, maxDistance: proximityCheckValue)
            || hit.status == EnvironmentRaycastHitStatus.HitPointOccluded)
        {
            Attach(hit.point);
            depthRelightingEffect.SetActive(false);
        }
        else if (hit.status == EnvironmentRaycastHitStatus.RayOccluded)
        {
            Attach(transform.position);
            depthRelightingEffect.SetActive(false);
        }
    }

    private void Attach(Vector3 position)
    {
        _rb.isKinematic = true;
        _isLaunched = false;
        transform.position = position;
        PlayAudio(stickAudio);
    }

    private void PlayAudio(AudioClip clip)
    {
        if (!clip || !_audioSource)
        {
            return;
        }

        _audioSource.clip = clip;
        _audioSource.pitch = Random.Range(0.5f, 2.0f);
        _audioSource.Play();
    }
}
