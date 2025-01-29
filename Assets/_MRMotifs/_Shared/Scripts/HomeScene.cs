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

public class HomeScene : MonoBehaviour
{
    [SerializeField] private OVRPassthroughLayer oVRPassthroughLayer;

    private Camera _mainCamera;
    private Color _skyboxBackgroundColor;

    private void Awake()
    {
        _mainCamera = Camera.main;

        if (_mainCamera != null)
        {
            _skyboxBackgroundColor = _mainCamera.backgroundColor;
        }

#if UNITY_ANDROID
        CheckIfPassthroughIsRecommended();
#endif
    }

    private void CheckIfPassthroughIsRecommended()
    {
        if (_mainCamera == null)
        {
            return;
        }

        if (OVRManager.IsPassthroughRecommended())
        {
            oVRPassthroughLayer.enabled = true;
            _mainCamera.clearFlags = CameraClearFlags.SolidColor;
            _mainCamera.backgroundColor = Color.clear;
        }
        else
        {
            oVRPassthroughLayer.enabled = false;
            _mainCamera.clearFlags = CameraClearFlags.Skybox;
            _mainCamera.backgroundColor = _skyboxBackgroundColor;
        }
    }
}
