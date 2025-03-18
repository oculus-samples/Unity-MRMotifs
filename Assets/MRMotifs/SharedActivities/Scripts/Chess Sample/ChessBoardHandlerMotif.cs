// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using Fusion;
using UnityEngine;
using Oculus.Interaction;
using System.Collections.Generic;
using Meta.XR.MultiplayerBlocks.Fusion;
using Meta.XR.Samples;

namespace MRMotifs.SharedActivities.ChessSample
{
    /// <summary>
    /// Handles synchronization of chess piece positions and rotations across networked clients
    /// and manages interaction events for selecting and moving chess pieces.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class ChessBoardHandlerMotif : NetworkBehaviour, IStateAuthorityChanged
    {
        /// <summary>
        /// Networked array for storing chess piece positions.
        /// </summary>
        [Networked, Capacity(32)]
        private NetworkArray<Vector3> ChessPiecePositions => default;

        /// <summary>
        /// Networked array for storing chess piece rotations.
        /// </summary>
        [Networked, Capacity(32)]
        private NetworkArray<Quaternion> ChessPieceRotations => default;

        private List<InteractableUnityEventWrapper> m_chessPieceInteractables = new();
        private Grabbable m_grabbable;
        private GameObject m_grabbedChessPiece;
        private Vector3 m_pendingPosition;
        private bool m_hasSpawned;
        private bool m_isPieceBeingMoved;

        public override void Spawned()
        {
            base.Spawned();
            m_hasSpawned = true;
            InitializeChessPieces();
            FusionBBEvents.OnSceneLoadDone += SceneLoaded;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            FusionBBEvents.OnSceneLoadDone -= SceneLoaded;
            UnsubscribeChessPieceEvents();
        }

        /// <summary>
        /// This is a callback of the <see cref="FusionBBEvents"/> OnSceneLoadDone event.
        /// This method is called just once per experience, which means it is only called for the
        /// first client that joins. It is used so the first client is populating the networked properties
        /// with the chess piece positions and rotations for every client that joins later.
        /// </summary>
        private void SceneLoaded(NetworkRunner obj)
        {
            UpdateInitialChessPieceStates();
        }

        private void InitializeChessPieces()
        {
            var chessPieces = new List<InteractableUnityEventWrapper>();
            var allChildren = GetComponentsInChildren<Transform>();

            foreach (var child in allChildren)
            {
                if (child == transform)
                {
                    continue;
                }

                var interactable = child.GetComponent<InteractableUnityEventWrapper>();
                if (interactable != null)
                {
                    chessPieces.Add(interactable);
                }
            }

            m_chessPieceInteractables = chessPieces;

            foreach (var interactable in m_chessPieceInteractables)
            {
                interactable.WhenSelect.AddListener(() => TogglePieceMoved(true));
                interactable.WhenUnselect.AddListener(() => TogglePieceMoved(false));
            }
        }

        private void UpdateInitialChessPieceStates()
        {
            SetChessPieceRigidbodyState(false);

            for (var i = 0; i < ChessPiecePositions.Length; i++)
            {
                if (i < m_chessPieceInteractables.Count)
                {
                    ChessPiecePositions.Set(i, m_chessPieceInteractables[i].transform.localPosition);
                    ChessPieceRotations.Set(i, m_chessPieceInteractables[i].transform.localRotation);
                }
                else
                {
                    ChessPiecePositions.Set(i, Vector3.zero);
                    ChessPieceRotations.Set(i, Quaternion.identity);
                }
            }
        }

        private void TogglePieceMoved(bool isBeingMoved)
        {
            if (!Object.HasStateAuthority)
            {
                SetChessPieceRigidbodyState(false);
                Object.RequestStateAuthority();
                return;
            }

            m_isPieceBeingMoved = isBeingMoved;

            if (!m_isPieceBeingMoved)
            {
                SendChessPieceOffset();
            }
        }

        private void FixedUpdate()
        {
            if (!m_hasSpawned)
            {
                return;
            }

            if (Object.HasStateAuthority)
            {
                SendChessPieceOffset();
            }

            UpdateRemoteChessPieces();
        }

        private void SendChessPieceOffset()
        {
            for (var i = 0; i < m_chessPieceInteractables.Count; i++)
            {
                var chessPiece = m_chessPieceInteractables[i];
                var localPosition = chessPiece.transform.localPosition;
                var localRotation = chessPiece.transform.localRotation;

                ChessPiecePositions.Set(i, localPosition);
                ChessPieceRotations.Set(i, localRotation);
            }
        }

        private void UpdateRemoteChessPieces()
        {
            for (var i = 0; i < m_chessPieceInteractables.Count; i++)
            {
                if (i >= ChessPiecePositions.Length)
                {
                    continue;
                }

                var targetLocalPosition = ChessPiecePositions.Get(i);
                var targetLocalRotation = ChessPieceRotations.Get(i);
                var chessPieceTransform = m_chessPieceInteractables[i].transform;

                if (!HasStateAuthority)
                {
                    chessPieceTransform.localPosition = Vector3.Lerp(
                        chessPieceTransform.localPosition, targetLocalPosition,
                        Time.deltaTime * 10f);
                    chessPieceTransform.localRotation = Quaternion.Slerp(
                        chessPieceTransform.localRotation,
                        targetLocalRotation, Time.deltaTime * 10f);
                }
                else
                {
                    chessPieceTransform.localPosition = targetLocalPosition;
                    chessPieceTransform.localRotation = targetLocalRotation;
                }
            }
        }

        private void UnsubscribeChessPieceEvents()
        {
            foreach (var interactable in m_chessPieceInteractables)
            {
                interactable.WhenSelect.RemoveListener(() => TogglePieceMoved(true));
                interactable.WhenUnselect.RemoveListener(() => TogglePieceMoved(false));
            }
        }

        /// <summary>
        /// This method comes from the <see cref="IStateAuthorityChanged"/> interface and is called
        /// when the State Authority has been changed. We use to execute commands immediately
        /// after StateAuthority has been assigned to the client who tried to move a chess piece.
        /// </summary>
        public void StateAuthorityChanged()
        {
            if (!Object.HasStateAuthority)
            {
                SetChessPieceRigidbodyState(true);
            }
        }

        private void SetChessPieceRigidbodyState(bool isKinematic)
        {
            foreach (var chessPiece in m_chessPieceInteractables)
            {
                var chessPieceRb = chessPiece.GetComponent<Rigidbody>();
                chessPieceRb.isKinematic = isKinematic;
                chessPieceRb.useGravity = !isKinematic;
            }
        }
    }
}
#endif
