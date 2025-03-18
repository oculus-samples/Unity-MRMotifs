// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR;
using static OVRInput;
using System.Collections;
using System.Collections.Generic;
using Meta.XR.Samples;
using UnityEngine;

namespace MRMotifs.InstantContentPlacement.DepthEffects
{
    [MetaCodeSample("MRMotifs-InstantContentPlacement")]
    public class OrbSpawnerMotif : MonoBehaviour
    {
        [Tooltip("Handles environment raycasting for orbs placement.")]
        [SerializeField]
        private EnvironmentRaycastManager environmentRaycast;

        [Tooltip("Audio clip played when a scan is triggered.")]
        [SerializeField]
        private AudioClip scanTrigger;

        [Header("Orb Settings")]
        [Tooltip("Maximum orbs in the scene at a time.")]
        [SerializeField]
        private int maxOrbs = 5;

        [Tooltip("Force applied when launching an orb.")]
        [SerializeField]
        private float launchForce = 6.0f;

        [Tooltip("Prefab of the orb.")]
        [SerializeField]
        private ShockWaveOrbMotif orbMotifPrefab;

        [Tooltip("Prefab of the depth scan effect.")]
        [SerializeField]
        private AudioSource shockWaveEffectPrefab;

        [Header("Controller References")]
        [Tooltip("Reference to the left controller or hand.")]
        [SerializeField]
        private Controller leftController;

        [Tooltip("Reference to the right controller or hand.")]
        [SerializeField]
        private Controller rightController;

        [Tooltip("Button to launch an orb.")]
        [SerializeField]
        private Button launchOrbButton;

        [Tooltip("Button to detonate all active orbs.")]
        [SerializeField]
        private Button detonateAllOrbsButton;

        private float m_pitchLevel = 1.0f;
        private Transform m_cameraTransform;
        private Coroutine m_detonationCoroutine;
        private readonly List<ShockWaveOrbMotif> m_spawnedOrbs = new();
        private readonly Vector3 m_launchOffset = new(0, 0, 0.1f);

        private void Awake()
        {
            if (Camera.main != null)
            {
                m_cameraTransform = Camera.main.transform;
            }
        }

        private void Update()
        {
            m_pitchLevel = Mathf.Max(1.0f, m_pitchLevel - Time.deltaTime);

            if (GetDown(launchOrbButton))
            {
                if (m_spawnedOrbs.Count >= maxOrbs)
                {
                    return;
                }

                LaunchOrb();
            }

            if (GetDown(detonateAllOrbsButton) && m_detonationCoroutine == null)
            {
                TriggerDetonation();
            }
        }

        public void LaunchOrb()
        {
            var cameraTransform = m_cameraTransform;
            var launchPosition = cameraTransform.position + cameraTransform.forward * m_launchOffset.z;
            var launchDirection = cameraTransform.forward * launchForce;

            var orb = Instantiate(orbMotifPrefab, launchPosition, Quaternion.identity);
            orb.Initialize(environmentRaycast);
            orb.onDetonate.AddListener(HandleDepthScan);
            orb.Launch(launchDirection);

            m_spawnedOrbs.Add(orb);
        }

        public void TriggerDetonation()
        {
            m_detonationCoroutine = StartCoroutine(DetonateAllOrbs());
        }

        private IEnumerator DetonateAllOrbs()
        {
            var wait = new WaitForSeconds(0.1f);
            foreach (var shockWaveOrb in new List<ShockWaveOrbMotif>(m_spawnedOrbs))
            {
                shockWaveOrb.Detonate();
                yield return wait;
            }

            m_detonationCoroutine = null;
        }

        private void HandleDepthScan(Vector3 position, ShockWaveOrbMotif shockWaveOrbMotif)
        {
            var scanEffectAudioSrc = Instantiate(shockWaveEffectPrefab, position, Quaternion.identity);
            scanEffectAudioSrc.clip = scanTrigger;

            m_pitchLevel = Mathf.Clamp(m_pitchLevel + 0.25f, 1.0f, 3.0f);
            scanEffectAudioSrc.pitch = m_pitchLevel;
            scanEffectAudioSrc.Play();

            m_spawnedOrbs.Remove(shockWaveOrbMotif);
            Destroy(shockWaveOrbMotif.gameObject);
        }
    }
}
