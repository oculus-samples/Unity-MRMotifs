// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.Events;
using Meta.XR;

namespace MRMotifs.InstantContentPlacement.DepthEffects
{
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
        [SerializeField]
        private AudioClip spawnAudio;

        [Tooltip("Audio clip played when the orb sticks to a surface.")]
        [SerializeField]
        private AudioClip stickAudio;

        [Header("Collision Settings")]
        [Tooltip("Proximity threshold to detect collision with surfaces.")]
        [SerializeField]
        private float proximityCheckValue = 0.35f;

        [Tooltip("The Depth Relighting Effect on the orb to light up the surroundings.")]
        [SerializeField]
        private GameObject depthRelightingEffect;

        [Header("Events")]
        [Tooltip("Event triggered when the orb detonates.")]
        public UnityEvent<Vector3, ShockWaveOrbMotif> onDetonate;

        private bool m_isLaunched;
        private Rigidbody m_rb;
        private AudioSource m_audioSource;
        private EnvironmentRaycastManager m_raycastManager;

        private void Awake()
        {
            m_audioSource = GetComponent<AudioSource>();
            m_rb = GetComponent<Rigidbody>();
            m_isLaunched = false;
        }

        /// <summary>
        /// Sets up the orb with a reference to the raycast manager.
        /// </summary>
        /// <param name="raycastManager">Reference to the EnvironmentRaycastManager for collision detection.</param>
        public void Initialize(EnvironmentRaycastManager raycastManager)
        {
            m_raycastManager = raycastManager;
            PlayAudio(spawnAudio);
        }

        /// <summary>
        /// Launches the orb with the specified force.
        /// </summary>
        /// <param name="force">Force vector applied to the orb on launch.</param>
        public void Launch(Vector3 force)
        {
            m_rb.isKinematic = false;
            m_rb.AddForce(force, ForceMode.Impulse);
            m_isLaunched = true;
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
            if (!m_isLaunched || m_rb.linearVelocity.magnitude <= 0)
            {
                return;
            }

            var ray = new Ray(transform.position, m_rb.linearVelocity.normalized);
            if (m_raycastManager.Raycast(ray, out var hit, maxDistance: proximityCheckValue)
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
            m_rb.isKinematic = true;
            m_isLaunched = false;
            transform.position = position;
            PlayAudio(stickAudio);
        }

        private void PlayAudio(AudioClip clip)
        {
            if (!clip || !m_audioSource)
            {
                return;
            }

            m_audioSource.clip = clip;
            m_audioSource.pitch = Random.Range(0.5f, 2.0f);
            m_audioSource.Play();
        }
    }
}
