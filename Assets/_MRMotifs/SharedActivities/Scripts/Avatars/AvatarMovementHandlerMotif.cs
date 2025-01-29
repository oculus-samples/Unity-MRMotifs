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
using System;
using UnityEngine;
using System.Collections;
using Oculus.Interaction;
using System.Collections.Generic;
using Meta.XR.MultiplayerBlocks.Fusion;
using Meta.XR.MultiplayerBlocks.Shared;

/// <summary>
/// Handles synchronization of avatar positions and rotations across networked clients.
/// Manages the interaction between avatars and the object of interest, ensuring avatars
/// are correctly positioned relative to the object and updated whenever the object is moved.
/// </summary>
public class AvatarMovementHandlerMotif : NetworkBehaviour
{
    /// <summary>
    /// Stores the relative positions of avatars. Each entry represents an avatar's position relative
    /// to the object of interest, and these positions are synchronized across networked clients.
    /// </summary>
    [Networked, Capacity(8)] private NetworkArray<Vector3> AvatarPositions => default;

    /// <summary>
    /// Stores the relative rotations of avatars. Each entry represents an avatar's rotation relative
    /// to the object of interest, and these rotations are synchronized across networked clients.
    /// </summary>
    [Networked, Capacity(8)] private NetworkArray<Quaternion> AvatarRotations => default;

    private SpawnManagerMotif _spawnManagerMotif;
    private GameObject _objectOfInterest;
    private NetworkRunner _networkRunner;
    private AvatarBehaviourFusion _localAvatar;
    private InteractableUnityEventWrapper _interactableUnityEventWrapper;
    private bool _hasObjectMoved;

    public bool HasSpawned { get; private set; }
    public bool AvatarsInitialized { get; private set; }

    public event Action<AvatarBehaviourFusion> OnRemoteAvatarAdded;
    private readonly List<AvatarBehaviourFusion> _remoteAvatars = new();

    public override void Spawned()
    {
        base.Spawned();

        HasSpawned = true;
        _networkRunner = Runner;
        _spawnManagerMotif = FindObjectOfType<SpawnManagerMotif>();
        _objectOfInterest = _spawnManagerMotif.ObjectOfInterest;
        _interactableUnityEventWrapper = _objectOfInterest.GetComponent<InteractableUnityEventWrapper>();

        if (_spawnManagerMotif == null || _interactableUnityEventWrapper == null)
        {
            Debug.LogError("Either SpawnManagerMotif or InteractableUnityEventWrapper is missing.");
            return;
        }

        AvatarEntity.OnSpawned += AddAvatarToList;
        _interactableUnityEventWrapper.WhenSelect.AddListener(() => ToggleObjectMoved(true));
        _interactableUnityEventWrapper.WhenUnselect.AddListener(() => ToggleObjectMoved(false));

        InitializeAvatars();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);

        AvatarEntity.OnSpawned -= AddAvatarToList;
        _interactableUnityEventWrapper.WhenSelect.RemoveListener(() => ToggleObjectMoved(true));
        _interactableUnityEventWrapper.WhenUnselect.RemoveListener(() => ToggleObjectMoved(false));
    }

    private void InitializeAvatars()
    {
        var avatars = FindObjectsOfType<AvatarBehaviourFusion>();
        foreach (var avatar in avatars)
        {
            if (avatar.Object.HasStateAuthority)
            {
                _localAvatar = avatar;
            }
            else
            {
                _remoteAvatars.Add(avatar);
                ParentAvatarToObjectOfInterest(avatar);
                StartCoroutine(SetAvatarToSpawnLocation(avatar));
                DisableNetworkTransform(avatar);
            }
        }
    }

    private void AddAvatarToList(AvatarEntity avatarEntity)
    {
        StartCoroutine(AddAvatarToListWhenReady(avatarEntity));
    }

    private IEnumerator AddAvatarToListWhenReady(AvatarEntity avatarEntity)
    {
        // Additional delay required since Avatars v28+ require some additional time to be loaded
        // No event to await the full "readiness" of the avatar is available yet
        yield return new WaitForSeconds(1.5f);
        while (!avatarEntity)
        {
            yield return null;
        }

        while (avatarEntity && avatarEntity.IsPendingAvatar)
        {
            yield return null;
        }

        if (!avatarEntity)
        {
            yield break;
        }

        var avatar = avatarEntity.GetComponent<AvatarBehaviourFusion>();
        if (!avatar)
        {
            yield break;
        }

        if (avatar.Object.HasStateAuthority)
        {
            _localAvatar = avatar;
            SendAvatarOffset();
        }
        else
        {
            _remoteAvatars.Add(avatar);
            ParentAvatarToObjectOfInterest(avatar);
            StartCoroutine(SetAvatarToSpawnLocation(avatar));
            DisableNetworkTransform(avatar);
        }
    }

    private void ParentAvatarToObjectOfInterest(AvatarBehaviourFusion avatar)
    {
        avatar.transform.parent = _objectOfInterest.transform;
    }

    private void DisableNetworkTransform(AvatarBehaviourFusion avatar)
    {
        var networkTransform = avatar.GetComponent<NetworkTransform>();
        if (networkTransform)
        {
            networkTransform.enabled = false;
        }
    }

    private IEnumerator SetAvatarToSpawnLocation(AvatarBehaviourFusion avatar)
    {
        if (!_spawnManagerMotif)
        {
            yield break;
        }

        var clientId = 0;
        foreach (var unused in _networkRunner.ActivePlayers)
        {
            clientId++;
        }

        var occupiedCount = GetOccupiedPlayerCount();
        while (occupiedCount <= clientId - 1)
        {
            yield return new WaitForEndOfFrame();
            occupiedCount = GetOccupiedPlayerCount();
        }

        var avatarIndex = GetAvatarIndex(avatar);
        if (avatarIndex < 0)
        {
            yield break;
        }

        SendAvatarOffset();
        yield return new WaitForEndOfFrame();
        AvatarsInitialized = true;
        OnRemoteAvatarAdded?.Invoke(avatar);
    }

    private int GetOccupiedPlayerCount()
    {
        var occupiedCount = 0;

        for (var i = 0; i < _spawnManagerMotif.OccupyingPlayers.Length; i++)
        {
            if (_spawnManagerMotif.OccupyingPlayers.Get(i) != PlayerRef.None)
            {
                occupiedCount++;
            }
        }

        return occupiedCount;
    }

    private void ToggleObjectMoved(bool hasMoved)
    {
        _hasObjectMoved = hasMoved;
    }

    private void Update()
    {
        if (_hasObjectMoved)
        {
            SendAvatarOffset();
        }

        UpdateRemoteAvatars();
    }

    private void SendAvatarOffset()
    {
        var relativePosition = _objectOfInterest.transform.InverseTransformPoint(_localAvatar.transform.position);
        var relativeRotation = Quaternion.Inverse(_objectOfInterest.transform.rotation) * _localAvatar.transform.rotation;

        var avatarIndex = GetLocalPlayerIndex();
        if (avatarIndex < 0 || avatarIndex >= AvatarPositions.Length)
        {
            return;
        }

        SendPositionAndRotationRpc(avatarIndex, relativePosition, relativeRotation);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void SendPositionAndRotationRpc(int avatarIndex, Vector3 pos, Quaternion rot)
    {
        AvatarPositions.Set(avatarIndex, pos);
        AvatarRotations.Set(avatarIndex, rot);
    }

    private void UpdateRemoteAvatars()
    {
        for (var i = _remoteAvatars.Count - 1; i >= 0; i--)
        {
            var remoteAvatar = _remoteAvatars[i];
            if (!remoteAvatar || !remoteAvatar.Object)
            {
                _remoteAvatars.RemoveAt(i);
                continue;
            }

            var avatarIndex = GetAvatarIndex(remoteAvatar);
            if (avatarIndex < 0 || avatarIndex >= AvatarPositions.Length)
            {
                // Skip this avatar if the index is invalid or out of bounds
                // to prevent accessing arrays with invalid indices and causing exceptions
                continue;
            }

            var newPosition = AvatarPositions.Get(avatarIndex);
            var newRotation = AvatarRotations.Get(avatarIndex);

            var worldPosition = _objectOfInterest.transform.TransformPoint(newPosition);
            var worldRotation = _objectOfInterest.transform.rotation * newRotation;

            remoteAvatar.transform.position = worldPosition;
            remoteAvatar.transform.rotation = worldRotation;
        }
    }

    private int GetLocalPlayerIndex()
    {
        for (var i = 0; i < _spawnManagerMotif.OccupyingPlayers.Length; i++)
        {
            if (_spawnManagerMotif.OccupyingPlayers.Get(i) == _networkRunner.LocalPlayer)
            {
                return i;
            }
        }

        return -1;
    }

    private int GetAvatarIndex(AvatarBehaviourFusion avatar)
    {
        for (var i = 0; i < _spawnManagerMotif.OccupyingPlayers.Length; i++)
        {
            if (_spawnManagerMotif.OccupyingPlayers.Get(i) == avatar.Object.StateAuthority)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// This method is used by the <see cref="AvatarSpawnerHandlerMotif"/> class, in
    /// order to free up the spawn location again after a player has left the experience.
    /// </summary>
    public void RemoveRemoteAvatarByPlayer(PlayerRef player)
    {
        AvatarBehaviourFusion avatarToRemove = null;

        foreach (var avatar in _remoteAvatars)
        {
            if (avatar == null || avatar.Object == null || avatar.Object.StateAuthority != player) continue;
            avatarToRemove = avatar;
            break;
        }

        if (avatarToRemove != null)
        {
            _remoteAvatars.Remove(avatarToRemove);
        }
    }
}
#endif
