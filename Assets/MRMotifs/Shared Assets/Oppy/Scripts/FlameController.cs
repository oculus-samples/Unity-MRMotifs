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

using System;
using UnityEngine;
using UnityEngine.Serialization;

public class FlameController : MonoBehaviour
{
    private const int BlendShapesSpeed = 6;


    [SerializeField] private GameObject root;
    [SerializeField] private GameObject mid;
    [SerializeField] private GameObject tip;
    [SerializeField] private Transform attractionTarget;
    [SerializeField] private Light flameLight;
    [SerializeField] private GameObject lookAtTarget;
    [SerializeField] private int attractionForceMultiplier = 2;
    [SerializeField] private float lightIntensityVariance = 0.2f;
    private Vector3 targetOffset = new(0.013f, 0.5f, 0);

    private int _blendShapeCount;
    private Vector3 _distanceFromRoot;
    private GameObject _midGoal;
    private Rigidbody _midRigidbody;
    private GameObject _rootGoal;
    private Rigidbody _rootRigidbody;
    private SkinnedMeshRenderer _skinnedMeshRenderer;
    private float _startingLightIntensity;
    private GameObject _tipGoal;
    private Rigidbody _tipRigidbody;

    private void Awake()
    {
        _distanceFromRoot = targetOffset;
    }

    private void Start()
    {
        CreateTargetObject();

        _skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        _blendShapeCount = _skinnedMeshRenderer.sharedMesh.blendShapeCount;
        _rootRigidbody = root.GetComponent<Rigidbody>();
        _midRigidbody = mid.GetComponent<Rigidbody>();
        _tipRigidbody = tip.GetComponent<Rigidbody>();
        _startingLightIntensity = flameLight.intensity;
    }

    private void Update()
    {
        AnimateBlendShapes();
        RotateFlame();
        MoveFlame();
        AnimateLightIntensity();
    }

    private void FixedUpdate()
    {
        AddAttractionForces();
    }

    private void AddAttractionForces()
    {
        var baseDistance = _rootGoal.transform.position - root.transform.position;
        _rootRigidbody.AddForce(baseDistance * attractionForceMultiplier);
        var midAttractDir = _midGoal.transform.position - mid.transform.position;
        _midRigidbody.AddForce(midAttractDir * attractionForceMultiplier);
        var tipAttractDir = _tipGoal.transform.position - tip.transform.position;
        _tipRigidbody.AddForce(tipAttractDir * attractionForceMultiplier);
    }

    private void MoveFlame()
    {
        _rootGoal.transform.position = attractionTarget.transform.position + _distanceFromRoot;
    }

    private void AnimateLightIntensity()
    {
        flameLight.intensity = Remap(0, 100, _startingLightIntensity - lightIntensityVariance,
            _startingLightIntensity + lightIntensityVariance, _skinnedMeshRenderer.GetBlendShapeWeight(0));
    }

    private void RotateFlame()
    {
        var flameForward =
            Vector3.ProjectOnPlane(lookAtTarget.transform.position - root.transform.position, Vector3.up);

        tip.transform.rotation =
            Quaternion.LookRotation(tip.transform.position - mid.transform.position, flameForward);
        mid.transform.rotation =
            Quaternion.LookRotation(tip.transform.position - mid.transform.position, flameForward);
        root.transform.GetChild(0).rotation =
            Quaternion.LookRotation(mid.transform.position - root.transform.position, flameForward);
    }

    private void CreateTargetObject()
    {
        _rootGoal = new GameObject("baseGoal");
        _midGoal = new GameObject("midGoal");
        _tipGoal = new GameObject("topGoal");
        _midGoal.transform.SetParent(_rootGoal.transform);
        _tipGoal.transform.SetParent(_midGoal.transform);

        _rootGoal.transform.position = root.transform.position;
        _rootGoal.transform.rotation = root.transform.rotation;
        _midGoal.transform.position = mid.transform.position;
        _midGoal.transform.rotation = mid.transform.rotation;
        _tipGoal.transform.position = tip.transform.position;
        _tipGoal.transform.rotation = tip.transform.rotation;
    }

    private void AnimateBlendShapes()
    {
        for (var i = 0; i < _blendShapeCount; i++)
        {
            var noise = Mathf.PerlinNoise(Time.time * BlendShapesSpeed, i + 1);
            _skinnedMeshRenderer.SetBlendShapeWeight(i, noise * 100);
        }

        var lowestBlendShape = Mathf.Infinity;
        for (var i = 0; i < _blendShapeCount; i++)
        {
            if (_skinnedMeshRenderer.GetBlendShapeWeight(i) < lowestBlendShape)
            {
                lowestBlendShape = _skinnedMeshRenderer.GetBlendShapeWeight(i);
            }
        }

        for (var i = 0; i < _blendShapeCount; i++)
        {
            var remappedWeight = Remap(lowestBlendShape, 100, 0, 100,
                _skinnedMeshRenderer.GetBlendShapeWeight(i));
            _skinnedMeshRenderer.SetBlendShapeWeight(i, remappedWeight);
        }
    }

    private float Remap(float a, float b, float c, float d, float x)
    {
        return c + (x - a) * (d - c) / (b - a);
    }
}
