// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace MRMotifs.PassthroughTransitioning
{
    [MetaCodeSample("MRMotifs-PassthroughTransitioning")]
    public class PerlinNoiseTexture : MonoBehaviour
    {
        [Header("Texture Resolution")]
        [Tooltip("Width of the texture in pixels.")]
        [SerializeField]
        private int pixWidth = 1024;

        [Tooltip("Height of the texture in pixels.")]
        [SerializeField]
        private int pixHeight = 1024;

        [Header("Texture Pattern")]
        [Tooltip("The x origin of the sampled area in the plane.")]
        [SerializeField]
        private float xOrg = 0.2f;

        [Tooltip("The y origin of the sampled area in the plane.")]
        [SerializeField]
        private float yOrg = 0.5f;

        [Tooltip("The number of cycles of the basic noise pattern that are repeated over the width and height of the texture.")]
        [SerializeField]
        private float scale = 10.0f;

        private Texture2D m_noiseTex;
        private Color[] m_pix;
        private Renderer m_rend;
        private static readonly int s_noiseTex = Shader.PropertyToID("_NoiseTex");

        private void Awake()
        {
            m_rend = GetComponent<Renderer>();

            if (m_rend == null)
            {
                Debug.LogError("Renderer component missing from this GameObject. Please add a Renderer component.");
                return;
            }

            m_noiseTex = new Texture2D(pixWidth, pixHeight);
            m_pix = new Color[m_noiseTex.width * m_noiseTex.height];

            CalcNoise();
            m_noiseTex.SetPixels(m_pix);
            m_noiseTex.Apply();

            m_rend.material.SetTexture(s_noiseTex, m_noiseTex);
        }

        private void CalcNoise()
        {
            for (var y = 0; y < m_noiseTex.height; y++)
            {
                for (var x = 0; x < m_noiseTex.width; x++)
                {
                    var xCoord = xOrg + x / (float)m_noiseTex.width * scale;
                    var yCoord = yOrg + y / (float)m_noiseTex.height * scale;
                    var sample = Mathf.PerlinNoise(xCoord, yCoord);
                    m_pix[y * m_noiseTex.width + x] = new Color(sample, sample, sample);
                }
            }
        }
    }
}
