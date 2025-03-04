// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using System.Collections.Generic;

namespace MRMotifs.InstantContentPlacement.DepthEffects
{
    public class EnvironmentDepthMatrixHelperMotif : MonoBehaviour
    {
        private static readonly Matrix4x4[] s_envDepthDisplayInverseMatrices = new Matrix4x4[2];
        private static readonly List<Matrix4x4> s_displayMatrices = new();

        private static readonly int s_inverseReprojectionMatricesID = Shader.PropertyToID("_EnvironmentDepthInverseReprojectionMatrices");

        private static readonly int s_reprojectionMatricesID = Shader.PropertyToID("_EnvironmentDepthReprojectionMatrices");

        private void Update()
        {
            s_displayMatrices.Clear();
            Shader.GetGlobalMatrixArray(s_reprojectionMatricesID, s_displayMatrices);

            for (var i = 0; i < s_envDepthDisplayInverseMatrices.Length; i++)
            {
                s_envDepthDisplayInverseMatrices[i] = Matrix4x4.Inverse(s_displayMatrices[i]);
            }

            Shader.SetGlobalMatrixArray(s_inverseReprojectionMatricesID, s_envDepthDisplayInverseMatrices);
        }
    }
}
