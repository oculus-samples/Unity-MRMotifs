// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using System.Collections;
using Fusion;
using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;

namespace MRMotifs.ColocatedExperiences.Whiteboard
{
    public enum RayDirectionOption { Forward, Backward, Up, Down }

    [MetaCodeSample("MRMotifs-ColocatedExperiences")]
    public class RaycastDrawingMotif : NetworkBehaviour
    {
        [Header("Drawing Settings")]
        [SerializeField] private Transform penTip;
        [SerializeField] private Color penColor = Color.blue;
        [SerializeField] private int baseBrushRadius = 7;
        [SerializeField] private float maxDrawingDistance = 0.02f;
        [SerializeField] private LayerMask whiteboardLayerMask;
        [SerializeField] private RayDirectionOption rayDirectionOption = RayDirectionOption.Forward;

        private bool m_isDrawing;
        private Vector2 m_lastUV;
        private WhiteboardManagerMotif m_whiteboardManagerMotif;
        private NetworkedPanelPlacementMotif m_networkedPanelPlacementMotif;
        private InteractableUnityEventWrapper m_interactableUnityEventWrapper;

        public override void Spawned()
        {
            base.Spawned();
            StartCoroutine(WaitForWhiteboardManager());
            m_interactableUnityEventWrapper = GetComponentInChildren<InteractableUnityEventWrapper>();
            m_interactableUnityEventWrapper.WhenSelect.AddListener(StartGrabbing);
            m_interactableUnityEventWrapper.WhenUnselect.AddListener(StopGrabbing);
            m_interactableUnityEventWrapper.WhenHover.AddListener(DisableWhiteboardInteraction);
            m_interactableUnityEventWrapper.WhenUnhover.AddListener(EnableWhiteboardInteraction);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            m_interactableUnityEventWrapper.WhenSelect.RemoveListener(StartGrabbing);
            m_interactableUnityEventWrapper.WhenUnselect.RemoveListener(StopGrabbing);
            m_interactableUnityEventWrapper.WhenHover.RemoveListener(DisableWhiteboardInteraction);
            m_interactableUnityEventWrapper.WhenUnhover.RemoveListener(EnableWhiteboardInteraction);
        }

        private IEnumerator WaitForWhiteboardManager()
        {
            yield return new WaitUntil(() => WhiteboardManagerMotif.Instance);
            m_whiteboardManagerMotif = WhiteboardManagerMotif.Instance;
            m_networkedPanelPlacementMotif = FindAnyObjectByType<NetworkedPanelPlacementMotif>();
        }

        private void Update()
        {
            if (!m_whiteboardManagerMotif || !HasStateAuthority || !InteractionStateManagerMotif.Instance)
            {
                return;
            }

            if (InteractionStateManagerMotif.Instance.CurrentMode != InteractionMode.DrawingRaycast)
            {
                return;
            }

            var ray = new Ray(penTip.position, RayDirection());

            if (Physics.Raycast(ray, out var hit, maxDrawingDistance, whiteboardLayerMask))
            {
                var currentUV = m_whiteboardManagerMotif.WorldToUV(hit.point);
                var brushRadius = CalculateBrushRadius(hit.distance);

                if (!m_isDrawing)
                {
                    m_isDrawing = true;
                    m_lastUV = currentUV;
                    m_whiteboardManagerMotif.RPC_DrawLine(currentUV, currentUV, penColor, brushRadius);
                }
                else if (Vector2.Distance(m_lastUV, currentUV) > -0.001f)
                {
                    m_whiteboardManagerMotif.RPC_DrawLine(m_lastUV, currentUV, penColor, brushRadius);
                    m_lastUV = currentUV;
                }
            }
            else
            {
                m_isDrawing = false;
            }
        }
        
        private void EnableWhiteboardInteraction()
        {
            m_networkedPanelPlacementMotif.enabled = true;
        }

        private void DisableWhiteboardInteraction()
        {
            m_networkedPanelPlacementMotif.enabled = false;
        }

        private void StartGrabbing()
        {
            InteractionStateManagerMotif.Instance.SetMode(InteractionMode.DrawingRaycast);
        }

        private void StopGrabbing()
        {
            InteractionStateManagerMotif.Instance.ResetMode(InteractionMode.DrawingRaycast);
        }

        private int CalculateBrushRadius(float hitDistance)
        {
            var thicknessMultiplier = 1f - (hitDistance / maxDrawingDistance);
            return Mathf.Max(1, Mathf.RoundToInt(baseBrushRadius * thicknessMultiplier));
        }

        private Vector3 RayDirection()
        {
            return rayDirectionOption switch
            {
                RayDirectionOption.Backward => -penTip.forward,
                RayDirectionOption.Up => penTip.up,
                RayDirectionOption.Down => -penTip.up,
                _ => penTip.forward
            };
        }
    }
}
#endif
