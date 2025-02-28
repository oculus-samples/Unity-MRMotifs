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

#if FUSION2
using Fusion;
using UnityEngine;
using Oculus.Interaction;
using System.Collections.Generic;
using Meta.XR.MultiplayerBlocks.Fusion;

/// <summary>
/// Handles synchronization of chess piece positions and rotations across networked clients
/// and manages interaction events for selecting and moving chess pieces.
/// </summary>
public class ChessBoardHandlerMotif : NetworkBehaviour, IStateAuthorityChanged
{
    /// <summary>
    /// Networked array for storing chess piece positions.
    /// </summary>
    [Networked, Capacity(32)] private NetworkArray<Vector3> ChessPiecePositions => default;

    /// <summary>
    /// Networked array for storing chess piece rotations.
    /// </summary>
    [Networked, Capacity(32)] private NetworkArray<Quaternion> ChessPieceRotations => default;

    private List<InteractableUnityEventWrapper> _chessPieceInteractables = new();
    private Grabbable _grabbable;
    private GameObject _grabbedChessPiece;
    private Vector3 _pendingPosition;
    private bool _hasSpawned;
    private bool _isPieceBeingMoved;

    public override void Spawned()
    {
        base.Spawned();
        _hasSpawned = true;
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

        _chessPieceInteractables = chessPieces;

        foreach (var interactable in _chessPieceInteractables)
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
            if (i < _chessPieceInteractables.Count)
            {
                ChessPiecePositions.Set(i, _chessPieceInteractables[i].transform.localPosition);
                ChessPieceRotations.Set(i, _chessPieceInteractables[i].transform.localRotation);
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

        _isPieceBeingMoved = isBeingMoved;

        if (!_isPieceBeingMoved)
        {
            SendChessPieceOffset();
        }
    }

    private void FixedUpdate()
    {
        if (!_hasSpawned)
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
        for (var i = 0; i < _chessPieceInteractables.Count; i++)
        {
            var chessPiece = _chessPieceInteractables[i];
            var localPosition = chessPiece.transform.localPosition;
            var localRotation = chessPiece.transform.localRotation;

            ChessPiecePositions.Set(i, localPosition);
            ChessPieceRotations.Set(i, localRotation);
        }
    }

    private void UpdateRemoteChessPieces()
    {
        for (var i = 0; i < _chessPieceInteractables.Count; i++)
        {
            if (i >= ChessPiecePositions.Length)
            {
                continue;
            }

            var targetLocalPosition = ChessPiecePositions.Get(i);
            var targetLocalRotation = ChessPieceRotations.Get(i);
            var chessPieceTransform = _chessPieceInteractables[i].transform;

            if (!HasStateAuthority)
            {
                chessPieceTransform.localPosition = Vector3.Lerp(chessPieceTransform.localPosition, targetLocalPosition,
                    Time.deltaTime * 10f);
                chessPieceTransform.localRotation = Quaternion.Slerp(chessPieceTransform.localRotation,
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
        foreach (var interactable in _chessPieceInteractables)
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
        foreach (var chessPiece in _chessPieceInteractables)
        {
            var chessPieceRb = chessPiece.GetComponent<Rigidbody>();
            chessPieceRb.isKinematic = isKinematic;
            chessPieceRb.useGravity = !isKinematic;
        }
    }
}
#endif
