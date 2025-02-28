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

public class AudioController : MonoBehaviour
{
    [Tooltip("GameObject that contains the renderer with the fader material and usually contains the fader logic.")]
    [SerializeField] private GameObject alphaFader;

    [Tooltip("AudioSource that we use to adjust the volume base on the inverted alpha value.")]
    [SerializeField] private AudioSource audioSource;

    private Material _material;
    private float _maxVolume;
    private static readonly int InvertedAlpha = Shader.PropertyToID("_InvertedAlpha");

    private void Awake()
    {
        _material = alphaFader.GetComponent<Renderer>().material;
        _maxVolume = audioSource.volume;
    }

    private void Update()
    {
        var invertedAlpha = _material.GetFloat(InvertedAlpha);
        audioSource.volume = Mathf.Lerp(_maxVolume, 0.0f, invertedAlpha);
    }
}
