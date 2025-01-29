/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * You may not use the Oculus SDK except in compliance with the License,
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

using Oculus.Interaction;
using UnityEngine;

/// <summary>
/// Injects rotation constraints into the <see cref="GrabFreeTransformer"/> component at runtime.
/// Used to limit the rotation of the chess board and movie screen in the samples scenes of this MR Motif.
/// </summary>
public class ConstraintInjectorMotif : MonoBehaviour
{
    [Tooltip("Optional rotation constraints to be injected into the GrabFreeTransformer component.")]
    [SerializeField] private TransformerUtils.RotationConstraints rotationConstraints;

    private GrabFreeTransformer _grabFreeTransformer;

    private void Update()
    {
        if (_grabFreeTransformer)
        {
            return;
        }

        _grabFreeTransformer = gameObject.GetComponent<GrabFreeTransformer>();

        if (!_grabFreeTransformer)
        {
            return;
        }

        _grabFreeTransformer.InjectOptionalRotationConstraints(rotationConstraints);
        enabled = false;
    }
}
