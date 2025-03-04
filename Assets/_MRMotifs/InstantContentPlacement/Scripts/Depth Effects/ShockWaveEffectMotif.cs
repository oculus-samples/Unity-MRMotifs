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

using System.Collections;
using UnityEngine;

/// <summary>
/// Expands the scan wave effect over a set duration, then destroys the object.
/// </summary>
public class ShockWaveEffectMotif : MonoBehaviour
{
    [Header("Scan Wave Settings")]
    [Tooltip("The maximum scale the scan wave will reach.")]
    [SerializeField] private float endScale = 25.0f;

    [Tooltip("The duration over which the scan wave expands.")]
    [SerializeField] private float duration = 3.5f;

    [Tooltip("Controls the growth rate of the scan wave expansion.")]
    [SerializeField] private float growthRate = 2.0f;

    private const float StartScale = 0.0f;
    private float _currentTimer;

    private void Awake()
    {
        transform.localScale = Vector3.one * StartScale;
        StartCoroutine(ExpandAndDestroy());
    }

    private IEnumerator ExpandAndDestroy()
    {
        while (_currentTimer <= duration)
        {
            _currentTimer += Time.deltaTime;
            var scale = Mathf.Lerp(StartScale, endScale, Mathf.Pow(_currentTimer / duration, growthRate));
            transform.localScale = Vector3.one * scale;
            yield return null;
        }

        Destroy(gameObject);
    }
}
