// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using System.Collections;
using Fusion;
using Meta.XR.Samples;
using UnityEngine;

namespace MRMotifs.ColocatedExperiences.Whiteboard
{
    [MetaCodeSample("MRMotifs-ColocatedExperiences")]
    [RequireComponent(typeof(PointerHandlerMotif))]
    public class PointerDrawingMotif : NetworkBehaviour
    {
        [SerializeField] private Color penColor = Color.blue;
        [SerializeField] private int brushRadius = 6;

        private bool m_isReady;
        private bool m_leftDrawing;
        private bool m_rightDrawing;
        private Vector2 m_leftLastUV;
        private Vector2 m_rightLastUV;
        private PointerHandlerMotif m_pointerHandlerMotif;
        private WhiteboardManagerMotif m_whiteboardManagerMotif;

        public override void Spawned()
        {
            base.Spawned();
            m_pointerHandlerMotif = GetComponent<PointerHandlerMotif>();
            StartCoroutine(WaitForManager());
            m_isReady = true;
        }

        private IEnumerator WaitForManager()
        {
            yield return new WaitUntil(() => WhiteboardManagerMotif.Instance);
            m_whiteboardManagerMotif = WhiteboardManagerMotif.Instance;
        }

        private void Update()
        {
            if (!m_isReady)
            {
                return;
            }
            
            if (!m_pointerHandlerMotif || !m_whiteboardManagerMotif || !InteractionStateManagerMotif.Instance)
            {
                return;
            }

            HandlePointerDrawing(
                m_pointerHandlerMotif.LeftHit.point,
                m_pointerHandlerMotif.LeftActiveSource,
                m_pointerHandlerMotif.leftHand,
                m_pointerHandlerMotif.leftController,
                ref m_leftDrawing,
                ref m_leftLastUV,
                InteractionMode.DrawingPointer
            );

            HandlePointerDrawing(
                m_pointerHandlerMotif.RightHit.point,
                m_pointerHandlerMotif.RightActiveSource,
                m_pointerHandlerMotif.rightHand,
                m_pointerHandlerMotif.rightController,
                ref m_rightDrawing,
                ref m_rightLastUV,
                InteractionMode.DrawingPointer
            );
        }

        private void HandlePointerDrawing(Vector3 hitPoint,
            PointerSource source,
            OVRHand hand,
            OVRInput.Controller controller,
            ref bool isDrawing,
            ref Vector2 lastUV,
            InteractionMode mode)
        {
            bool inputDown;
            bool inputHeld;
            bool inputUp;

            if (source == PointerSource.Hand && hand)
            {
                var isPinching = hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
                inputDown = isPinching && !isDrawing;
                inputHeld = isPinching;
                inputUp = !isPinching && isDrawing;
            }
            else
            {
                inputDown = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, controller);
                inputHeld = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, controller);
                inputUp = OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, controller);
            }

            if (inputDown && InteractionStateManagerMotif.Instance.CanDrawWithPointer())
            {
                Object.RequestStateAuthority();
                InteractionStateManagerMotif.Instance.SetMode(mode);
                isDrawing = true;
                lastUV = m_whiteboardManagerMotif.WorldToUV(hitPoint);
                m_whiteboardManagerMotif.RPC_DrawLine(lastUV, lastUV, penColor, brushRadius);
            }
            else if (inputHeld && isDrawing)
            {
                Object.RequestStateAuthority();
                var currentUV = m_whiteboardManagerMotif.WorldToUV(hitPoint);
                if (!(Vector2.Distance(lastUV, currentUV) > 0.001f))
                {
                    return;
                }

                m_whiteboardManagerMotif.RPC_DrawLine(lastUV, currentUV, penColor, brushRadius);
                lastUV = currentUV;
            }
            else if (inputUp && isDrawing)
            {
                isDrawing = false;
                InteractionStateManagerMotif.Instance.ResetMode(mode);
            }
        }
    }
}
#endif
