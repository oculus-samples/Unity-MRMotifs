// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using UnityEngine;

namespace MRMotifs.ColocatedExperiences.BouncingBall
{
    public class BouncingBallMotif : MonoBehaviour
    {
        [SerializeField] private AudioClip spawnSound;
        [SerializeField] private AudioClip ballSound;

        private AudioSource m_audioSource;

        private void Awake()
        {
            m_audioSource = GetComponent<AudioSource>();
            m_audioSource.PlayOneShot(spawnSound);
        }

        private void OnCollisionEnter(Collision other)
        {
            var impactStrength = other.relativeVelocity.magnitude;

            if (!(impactStrength > 5.0f))
            {
                return;
            }

            m_audioSource.PlayOneShot(ballSound);
        }
    }
}
#endif
