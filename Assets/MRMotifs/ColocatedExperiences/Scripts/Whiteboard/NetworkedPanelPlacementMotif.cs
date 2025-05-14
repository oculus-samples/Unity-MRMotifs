// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Meta.XR;
using Meta.XR.Samples;
using UnityEngine;

namespace MRMotifs.ColocatedExperiences.Whiteboard
{
    [MetaCodeSample("MRMotifs-ColocatedExperiences")]
    public class NetworkedPanelPlacementMotif : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform leftRaycastAnchor;
        [SerializeField] private Transform rightRaycastAnchor;
        [SerializeField] private GameObject panelHighlight;

        [Header("Input Settings Hands")]
        [SerializeField] private OVRInput.RawButton leftGrabButton = OVRInput.RawButton.LHandTrigger;
        [SerializeField] private OVRInput.RawButton rightGrabButton = OVRInput.RawButton.RHandTrigger;

        [Header("Input Settings Controllers")]
        [SerializeField] private OVRInput.RawAxis2D leftAxis = OVRInput.RawAxis2D.LThumbstick;
        [SerializeField] private OVRInput.RawAxis2D rightAxis = OVRInput.RawAxis2D.RThumbstick;

        [Header("Microgestures")]
        [SerializeField] private OVRMicrogestureEventSource leftGestureSource;
        [SerializeField] private OVRMicrogestureEventSource rightGestureSource;

        [Header("Manually Assigned OVRHand Objects")]
        [Tooltip("Assign your actual left OVRHand here in the Inspector.")]
        [SerializeField] private OVRHand leftHand;
        [Tooltip("Assign your actual right OVRHand here in the Inspector.")]
        [SerializeField] private OVRHand rightHand;

        [Header("Panel Settings")]
        private const float PANEL_ASPECT_RATIO = 0.823f;
        private const float MIN_DISTANCE = 0.3f;
        private const float MAX_DISTANCE = 10f;
        private const float MOVE_SPEED = 2.5f;
        private const float SCALE_SPEED = 1.5f;
        private const float MIN_SCALE = 0.2f;
        private const float MAX_SCALE = 1.5f;
        private const float SMOOTH_TIME = 0.13f;
        private const float SWIPE_MOVE_DISTANCE = 0.25f;

        private Pose? m_targetPose;
        private Pose? m_environmentPose;
        private bool m_isReady;
        private bool m_isGrabbing;
        private float m_distanceFromController;
        private Transform m_panel;
        private Transform m_centerEyeAnchor;
        private Transform m_activeRaycastAnchor;
        private OVRInput.RawButton m_activeGrabButton;
        private OVRInput.RawAxis2D m_activeMoveAxis;
        private NetworkTransform m_networkTransform;
        private EnvironmentRaycastManager m_raycastManager;
        private readonly RollingAverage m_rollingAverageFilter = new();

        public override void Spawned()
        {
            base.Spawned();
            if (Camera.main != null)
            {
                m_centerEyeAnchor = Camera.main.transform;
            }

            m_panel = transform;
            m_raycastManager = FindAnyObjectByType<EnvironmentRaycastManager>();

            if (leftGestureSource != null)
            {
                leftGestureSource.GestureRecognizedEvent.AddListener(gesture =>
                    OnGestureRecognized(OVRPlugin.Hand.HandLeft, gesture));
            }
            if (rightGestureSource != null)
            {
                rightGestureSource.GestureRecognizedEvent.AddListener(gesture =>
                    OnGestureRecognized(OVRPlugin.Hand.HandRight, gesture));
            }

            StartCoroutine(WaitForWhiteboardManager());
        }

        private IEnumerator WaitForWhiteboardManager()
        {
            yield return new WaitUntil(() => WhiteboardManagerMotif.Instance);
            m_isReady = true;
        }

        private IEnumerator Start()
        {
            yield return new WaitUntil(() => WhiteboardManagerMotif.Instance != null);
            m_isReady = true;
        }

        private void Update()
        {
            if (!m_isReady || !Application.isFocused)
            {
                return;
            }

            var leftHit = CheckPanelHitCombined(true);
            var rightHit = CheckPanelHitCombined(false);
            panelHighlight.SetActive(leftHit || rightHit);

            if (m_isGrabbing)
            {
                UpdateTargetPose();

                if (OVRInput.GetUp(m_activeGrabButton))
                {
                    Object.ReleaseStateAuthority();
                    EndGrabbing();
                }
            }
            else
            {
                if (!InteractionStateManagerMotif.Instance.CanManipulatePanel())
                {
                    panelHighlight.SetActive(false);
                    return;
                }

                if (leftHit)
                {
                    var inputY = OVRInput.Get(leftAxis).y;
                    if (Mathf.Abs(inputY) > 0.1f)
                    {
                        Object.RequestStateAuthority();
                        ScalePanel(inputY);
                    }

                    if (OVRInput.GetDown(leftGrabButton))
                    {
                        Object.RequestStateAuthority();
                        BeginGrabbing(leftRaycastAnchor, leftGrabButton, leftAxis, fromMicroGesture: false);
                    }
                }
                else if (rightHit)
                {
                    var inputY = OVRInput.Get(rightAxis).y;
                    if (Mathf.Abs(inputY) > 0.1f)
                    {
                        Object.RequestStateAuthority();
                        ScalePanel(inputY);
                    }

                    if (OVRInput.GetDown(rightGrabButton))
                    {
                        Object.RequestStateAuthority();
                        BeginGrabbing(rightRaycastAnchor, rightGrabButton, rightAxis, fromMicroGesture: false);
                    }
                }
            }

            AnimatePanelPose();
        }

        /// <summary>
        /// Checks if the panel is hit by either the hand pointer (if available) OR the controller raycast.
        /// Passing true means left side, false means right side.
        /// </summary>
        private bool CheckPanelHitCombined(bool isLeftSide)
        {
            var handRef = isLeftSide ? leftHand : rightHand;
            var handIsTracked = (handRef && handRef.IsTracked && handRef.PointerPose);

            var handHit = false;
            if (handIsTracked)
            {
                handHit = CheckPanelHitHand(handRef);
            }

            var anchor = isLeftSide ? leftRaycastAnchor : rightRaycastAnchor;
            var controllerHit = CheckPanelHitController(anchor);

            return handHit || controllerHit;
        }

        private bool CheckPanelHitController(Transform anchor)
        {
            var ray = new Ray(anchor.position + anchor.forward * 0.1f, anchor.forward);
            return Physics.Raycast(ray, out var hit) && (hit.transform == m_panel || hit.transform.IsChildOf(m_panel));
        }

        private bool CheckPanelHitHand(OVRHand handRef)
        {
            var pose = handRef.PointerPose;
            var ray = new Ray(pose.position, pose.forward);
            return Physics.Raycast(ray, out var hit) && (hit.transform == m_panel || hit.transform.IsChildOf(m_panel));
        }

        /// <summary>
        /// Called by OVRMicrogestureEventSource for left and right hands.
        /// </summary>
        private void OnGestureRecognized(OVRPlugin.Hand hand, OVRHand.MicrogestureType gesture)
        {
            var isLeft = (hand == OVRPlugin.Hand.HandLeft);
            var handRef = isLeft ? leftHand : rightHand;
            var anchor = m_centerEyeAnchor;
            var axis = isLeft ? leftAxis : rightAxis;
            var grabButton = isLeft ? leftGrabButton : rightGrabButton;

            if (!CheckPanelHitHand(handRef))
            {
                if (!CheckPanelHitController(anchor))
                {
                    return;
                }
            }
            
            switch (gesture)
            {
                case OVRHand.MicrogestureType.ThumbTap:
                {
                    switch (m_isGrabbing)
                    {
                        case false when InteractionStateManagerMotif.Instance.CanManipulatePanel():
                            Object.RequestStateAuthority();
                            BeginGrabbing(anchor, grabButton, axis, fromMicroGesture: true);
                            break;
                        case true:
                            Object.ReleaseStateAuthority();
                            EndGrabbing();
                            break;
                    }
                    break;
                }

                case OVRHand.MicrogestureType.SwipeLeft:
                {
                    Object.RequestStateAuthority();
                    ScalePanel(-5f);
                    break;
                }

                case OVRHand.MicrogestureType.SwipeRight:
                {
                    Object.RequestStateAuthority();
                    ScalePanel(5f);
                    break;
                }

                case OVRHand.MicrogestureType.SwipeForward:
                {
                    Object.RequestStateAuthority();
                    AdjustDistance(SWIPE_MOVE_DISTANCE);
                    break;
                }

                case OVRHand.MicrogestureType.SwipeBackward:
                {
                    Object.RequestStateAuthority();
                    AdjustDistance(-SWIPE_MOVE_DISTANCE);
                    break;
                }

                case OVRHand.MicrogestureType.NoGesture:
                case OVRHand.MicrogestureType.Invalid:
                default:
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Scales the panel up/down with a continuous factor.
        /// Negative = scale down, positive = scale up.
        /// </summary>
        private void ScalePanel(float scaleInput)
        {
            var currentScale = m_panel.localScale.x;
            var newScale = currentScale * (1f + scaleInput * SCALE_SPEED * Time.deltaTime);
            newScale = Mathf.Clamp(newScale, MIN_SCALE, MAX_SCALE);
            m_panel.localScale = new Vector3(newScale, newScale * PANEL_ASPECT_RATIO, 1f);
        }

        /// <summary>
        /// Adjusts the distance from the anchor by delta, then re-clamps.
        /// This effectively moves the panel closer/farther from the user.
        /// </summary>
        private void AdjustDistance(float delta)
        {
            m_distanceFromController += delta;
            m_distanceFromController = Mathf.Clamp(m_distanceFromController, MIN_DISTANCE, MAX_DISTANCE);
        }

        /// <summary>
        /// Begin grabbing the panel.
        /// If fromMicroGesture=true, we skip re-initializing the distance so no jump occurs.
        /// If from controller, we do recalc, so you can position precisely from the controller anchor.
        /// </summary>
        private void BeginGrabbing(Transform rayAnchor, OVRInput.RawButton grabButton, OVRInput.RawAxis2D moveAxis, bool fromMicroGesture = false)
        {
            m_isGrabbing = true;
            m_activeRaycastAnchor = rayAnchor;
            m_activeGrabButton = grabButton;
            m_activeMoveAxis = moveAxis;

            if (!fromMicroGesture)
            {
                m_distanceFromController = Vector3.Distance(rayAnchor.position, m_panel.position);
            }

            panelHighlight.SetActive(false);
            InteractionStateManagerMotif.Instance.SetMode(InteractionMode.PanelManipulation);
        }

        private void EndGrabbing()
        {
            m_isGrabbing = false;
            m_targetPose = null;
            m_environmentPose = null;
            m_activeRaycastAnchor = null;
            InteractionStateManagerMotif.Instance.ResetMode(InteractionMode.PanelManipulation);
        }

        private void UpdateTargetPose()
        {
            var moveInput = OVRInput.Get(m_activeMoveAxis).y;
            m_distanceFromController += moveInput * MOVE_SPEED * Time.deltaTime;
            m_distanceFromController = Mathf.Clamp(m_distanceFromController, MIN_DISTANCE, MAX_DISTANCE);

            var newEnvPose = TryGetEnvironmentPose(m_activeRaycastAnchor);
            m_environmentPose = newEnvPose;

            var manualPosition = m_activeRaycastAnchor.position + m_activeRaycastAnchor.forward * m_distanceFromController;
            var manualForward = Vector3.ProjectOnPlane(m_centerEyeAnchor.position - manualPosition, Vector3.up).normalized;
            var manualPose = new Pose(manualPosition, Quaternion.LookRotation(manualForward, Vector3.up));

            var chooseEnvPose = m_environmentPose.HasValue &&
                                (Vector3.Distance(manualPose.position, m_environmentPose.Value.position) /
                                    Vector3.Distance(manualPose.position, m_centerEyeAnchor.position) < 0.5f);

            m_targetPose = chooseEnvPose ? m_environmentPose.Value : manualPose;
        }

        private Pose? TryGetEnvironmentPose(Transform rayAnchor)
        {
            var ray = new Ray(rayAnchor.position + rayAnchor.forward * 0.1f, rayAnchor.forward);
            if (!m_raycastManager.Raycast(ray, out var hit) || hit.normalConfidence < 0.5f)
            {
                return null;
            }

            var isCeiling = Vector3.Dot(hit.normal, Vector3.down) > 0.7f;
            if (isCeiling)
            {
                return null;
            }

            const float SIZE_TOLERANCE = 0.2f;
            var panelSize = new Vector3(m_panel.localScale.x, m_panel.localScale.y, 0f) * (1f - SIZE_TOLERANCE);
            var isVerticalSurface = Mathf.Abs(Vector3.Dot(hit.normal, Vector3.up)) < 0.3f;

            if (isVerticalSurface)
            {
                if (!m_raycastManager.PlaceBox(ray, panelSize, Vector3.up, out var result)) return null;
                var smoothedNormal = m_rollingAverageFilter.UpdateRollingAverage(result.normal);
                return new Pose(result.point, Quaternion.LookRotation(smoothedNormal, Vector3.up));
            }

            var pos = hit.point + Vector3.up * (m_panel.localScale.y * 0.5f);
            var halfExtents = panelSize * 0.5f;
            var forward = Vector3.ProjectOnPlane(m_centerEyeAnchor.position - pos, Vector3.up).normalized;
            var orient = Quaternion.LookRotation(forward, Vector3.up);

            const float OFFSET = 0.1f;
            if (!m_raycastManager.CheckBox(pos + Vector3.up * OFFSET, halfExtents, orient))
            {
                return new Pose(pos, orient);
            }

            return null;
        }

        private void AnimatePanelPose()
        {
            if (!m_targetPose.HasValue)
            {
                return;
            }

            m_panel.position = Vector3.Lerp(
                m_panel.position,
                m_targetPose.Value.position,
                Time.deltaTime / SMOOTH_TIME);

            m_panel.rotation = Quaternion.Slerp(
                m_panel.rotation,
                m_targetPose.Value.rotation,
                Time.deltaTime / SMOOTH_TIME);
        }

        private class RollingAverage
        {
            private List<Vector3> m_normals;
            private int m_index;

            public Vector3 UpdateRollingAverage(Vector3 current)
            {
                if (m_normals == null)
                {
                    const int FILTER_SIZE = 10;
                    m_normals = Enumerable.Repeat(current, FILTER_SIZE).ToList();
                }

                m_index = (m_index + 1) % m_normals.Count;
                m_normals[m_index] = current;
                var sum = Vector3.zero;
                foreach (var n in m_normals) sum += n;
                return sum.normalized;
            }
        }
    }
}
#endif
