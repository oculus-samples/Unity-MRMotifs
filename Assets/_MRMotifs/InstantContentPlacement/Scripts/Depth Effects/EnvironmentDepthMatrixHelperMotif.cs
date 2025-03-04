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
using System.Collections.Generic;

public class EnvironmentDepthMatrixHelperMotif : MonoBehaviour
{
    private static readonly Matrix4x4[] EnvDepthDisplayInverseMatrices = new Matrix4x4[2];
    private static readonly List<Matrix4x4> DisplayMatrices = new();
    private static readonly int InverseReprojectionMatricesID = Shader.PropertyToID("_EnvironmentDepthInverseReprojectionMatrices");
    private static readonly int ReprojectionMatricesID = Shader.PropertyToID("_EnvironmentDepthReprojectionMatrices");

    private void Update()
    {
        DisplayMatrices.Clear();
        Shader.GetGlobalMatrixArray(ReprojectionMatricesID, DisplayMatrices);

        for (var i = 0; i < EnvDepthDisplayInverseMatrices.Length; i++)
        {
            EnvDepthDisplayInverseMatrices[i] = Matrix4x4.Inverse(DisplayMatrices[i]);
        }

        Shader.SetGlobalMatrixArray(InverseReprojectionMatricesID, EnvDepthDisplayInverseMatrices);
    }
}
