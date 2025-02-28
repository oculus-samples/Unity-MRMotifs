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

public class PerlinNoiseTexture : MonoBehaviour
{
    [Header("Texture Resolution")]
    [Tooltip("Width of the texture in pixels.")]
    [SerializeField] private int pixWidth = 1024;

    [Tooltip("Height of the texture in pixels.")]
    [SerializeField] private int pixHeight = 1024;

    [Header("Texture Pattern")]
    [Tooltip("The x origin of the sampled area in the plane.")]
    [SerializeField] private float xOrg = 0.2f;

    [Tooltip("The y origin of the sampled area in the plane.")]
    [SerializeField] private float yOrg = 0.5f;

    [Tooltip("The number of cycles of the basic noise pattern that are repeated over the width and height of the texture.")]
    [SerializeField] private float scale = 10.0f;

    private Texture2D _noiseTex;
    private Color[] _pix;
    private Renderer _rend;
    private static readonly int NoiseTex = Shader.PropertyToID("_NoiseTex");

    private void Awake()
    {
        _rend = GetComponent<Renderer>();

        if (_rend == null)
        {
            Debug.LogError("Renderer component missing from this GameObject. Please add a Renderer component.");
            return;
        }

        _noiseTex = new Texture2D(pixWidth, pixHeight);
        _pix = new Color[_noiseTex.width * _noiseTex.height];

        CalcNoise();
        _noiseTex.SetPixels(_pix);
        _noiseTex.Apply();

        _rend.material.SetTexture(NoiseTex, _noiseTex);
    }

    private void CalcNoise()
    {
        for (var y = 0; y < _noiseTex.height; y++)
        {
            for (var x = 0; x < _noiseTex.width; x++)
            {
                var xCoord = xOrg + x / (float)_noiseTex.width * scale;
                var yCoord = yOrg + y / (float)_noiseTex.height * scale;
                var sample = Mathf.PerlinNoise(xCoord, yCoord);
                _pix[y * _noiseTex.width + x] = new Color(sample, sample, sample);
            }
        }
    }
}
