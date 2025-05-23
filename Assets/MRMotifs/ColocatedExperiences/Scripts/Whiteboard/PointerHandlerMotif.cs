// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace MRMotifs.ColocatedExperiences.Whiteboard
{
    public enum PointerSource
    {
        Hand,
        Controller
    }

    [MetaCodeSample("MRMotifs-ColocatedExperiences")]
    public class PointerHandlerMotif : MonoBehaviour
    {
        [Header("Auto-Switch Settings")]
        [SerializeField] private bool autoSwitch = true;

        [Header("Left Pointer")]
        public OVRHand leftHand;
        public OVRInput.Controller leftController = OVRInput.Controller.LTouch;

        [Header("Right Pointer")]
        public OVRHand rightHand;
        public OVRInput.Controller rightController = OVRInput.Controller.RTouch;

        [Header("Raycast Settings")]
        [SerializeField] private LayerMask targetLayer;

        [Header("Hit Indicator Settings")]
        [Tooltip("Assign a prefab that represents a small black circle.")]
        [SerializeField] private GameObject hitIndicatorPrefab;

        public RaycastHit LeftHit { get; private set; }
        public PointerSource LeftActiveSource { get; private set; }
        public RaycastHit RightHit { get; private set; }
        public PointerSource RightActiveSource { get; private set; }

        private GameObject m_leftHitIndicator;
        private GameObject m_rightHitIndicator;

        private void Awake()
        {
            if (hitIndicatorPrefab == null)
            {
                return;
            }

            m_leftHitIndicator = Instantiate(hitIndicatorPrefab, transform);
            m_leftHitIndicator.SetActive(false);

            m_rightHitIndicator = Instantiate(hitIndicatorPrefab, transform);
            m_rightHitIndicator.SetActive(false);
        }

        private void Update()
        {
            UpdateLeftPointer();
            UpdateRightPointer();
        }

        private void UpdateLeftPointer()
        {
            Ray leftRay;
            if (autoSwitch && leftHand && leftHand.IsTracked && leftHand.PointerPose)
            {
                LeftActiveSource = PointerSource.Hand;
                leftRay = new Ray(leftHand.PointerPose.position, leftHand.PointerPose.forward);
            }
            else
            {
                LeftActiveSource = PointerSource.Controller;
                var pos = OVRInput.GetLocalControllerPosition(leftController);
                var rot = OVRInput.GetLocalControllerRotation(leftController);
                leftRay = new Ray(pos, rot * Vector3.forward);
            }

            if (Physics.Raycast(leftRay, out var hit, Mathf.Infinity, targetLayer))
            {
                LeftHit = hit;
                if (!m_leftHitIndicator)
                {
                    return;
                }

                m_leftHitIndicator.transform.position = hit.point;
                m_leftHitIndicator.transform.rotation = Quaternion.LookRotation(hit.normal);
                m_leftHitIndicator.SetActive(true);
            }
            else
            {
                if (m_leftHitIndicator)
                {
                    m_leftHitIndicator.SetActive(false);
                }
            }
        }

        private void UpdateRightPointer()
        {
            Ray rightRay;
            if (autoSwitch && rightHand && rightHand.IsTracked && rightHand.PointerPose)
            {
                RightActiveSource = PointerSource.Hand;
                rightRay = new Ray(rightHand.PointerPose.position, rightHand.PointerPose.forward);
            }
            else
            {
                RightActiveSource = PointerSource.Controller;
                var pos = OVRInput.GetLocalControllerPosition(rightController);
                var rot = OVRInput.GetLocalControllerRotation(rightController);
                rightRay = new Ray(pos, rot * Vector3.forward);
            }

            if (Physics.Raycast(rightRay, out var hit, Mathf.Infinity, targetLayer))
            {
                RightHit = hit;
                if (!m_rightHitIndicator)
                {
                    return;
                }

                m_rightHitIndicator.transform.position = hit.point;
                m_rightHitIndicator.transform.rotation = Quaternion.LookRotation(hit.normal);
                m_rightHitIndicator.SetActive(true);
            }
            else
            {
                if (m_rightHitIndicator)
                {
                    m_rightHitIndicator.SetActive(false);
                }
            }
        }
    }
}
