// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace MRMotifs.PassthroughTransitioning
{
    [MetaCodeSample("MRMotifs-PassthroughTransitioning")]
    public class AudioController : MonoBehaviour
    {
        [Tooltip("GameObject that contains the renderer with the fader material and usually contains the fader logic.")]
        [SerializeField]
        private GameObject alphaFader;

        [Tooltip("AudioSource that we use to adjust the volume base on the inverted alpha value.")]
        [SerializeField]
        private AudioSource audioSource;

        private Material m_material;
        private float m_maxVolume;
        private static readonly int s_invertedAlpha = Shader.PropertyToID("_InvertedAlpha");

        private void Awake()
        {
            m_material = alphaFader.GetComponent<Renderer>().material;
            m_maxVolume = audioSource.volume;
        }

        private void Update()
        {
            var invertedAlpha = m_material.GetFloat(s_invertedAlpha);
            audioSource.volume = Mathf.Lerp(m_maxVolume, 0.0f, invertedAlpha);
        }
    }
}
