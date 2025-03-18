// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using Fusion;
using System;
using UnityEngine;
using System.Collections;
using Oculus.Interaction;
using System.Collections.Generic;
using Meta.XR.MultiplayerBlocks.Fusion;
using Meta.XR.MultiplayerBlocks.Shared;
using Meta.XR.Samples;
using MRMotifs.SharedActivities.Spawning;

namespace MRMotifs.SharedActivities.Avatars
{
    /// <summary>
    /// Handles synchronization of avatar positions and rotations across networked clients.
    /// Manages the interaction between avatars and the object of interest, ensuring avatars
    /// are correctly positioned relative to the object and updated whenever the object is moved.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class AvatarMovementHandlerMotif : NetworkBehaviour
    {
        /// <summary>
        /// Stores the relative positions of avatars. Each entry represents an avatar's position relative
        /// to the object of interest, and these positions are synchronized across networked clients.
        /// </summary>
        [Networked, Capacity(8)]
        private NetworkArray<Vector3> AvatarPositions => default;

        /// <summary>
        /// Stores the relative rotations of avatars. Each entry represents an avatar's rotation relative
        /// to the object of interest, and these rotations are synchronized across networked clients.
        /// </summary>
        [Networked, Capacity(8)]
        private NetworkArray<Quaternion> AvatarRotations => default;

        private SpawnManagerMotif m_spawnManagerMotif;
        private GameObject m_objectOfInterest;
        private NetworkRunner m_networkRunner;
        private AvatarBehaviourFusion m_localAvatar;
        private InteractableUnityEventWrapper m_interactableUnityEventWrapper;
        private bool m_hasObjectMoved;

        public bool HasSpawned { get; private set; }
        public bool AvatarsInitialized { get; private set; }

        public event Action<AvatarBehaviourFusion> OnRemoteAvatarAdded;
        private readonly List<AvatarBehaviourFusion> m_remoteAvatars = new();

        public override void Spawned()
        {
            base.Spawned();

            HasSpawned = true;
            m_networkRunner = Runner;
            m_spawnManagerMotif = FindAnyObjectByType<SpawnManagerMotif>();
            m_objectOfInterest = m_spawnManagerMotif.ObjectOfInterest;
            m_interactableUnityEventWrapper = m_objectOfInterest.GetComponent<InteractableUnityEventWrapper>();

            if (m_spawnManagerMotif == null || m_interactableUnityEventWrapper == null)
            {
                Debug.LogError("Either SpawnManagerMotif or InteractableUnityEventWrapper is missing.");
                return;
            }

            AvatarEntity.OnSpawned += AddAvatarToList;
            m_interactableUnityEventWrapper.WhenSelect.AddListener(() => ToggleObjectMoved(true));
            m_interactableUnityEventWrapper.WhenUnselect.AddListener(() => ToggleObjectMoved(false));

            InitializeAvatars();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);

            AvatarEntity.OnSpawned -= AddAvatarToList;
            m_interactableUnityEventWrapper.WhenSelect.RemoveListener(() => ToggleObjectMoved(true));
            m_interactableUnityEventWrapper.WhenUnselect.RemoveListener(() => ToggleObjectMoved(false));
        }

        private void InitializeAvatars()
        {
            var avatars = FindObjectsByType<AvatarBehaviourFusion>(FindObjectsSortMode.None);
            foreach (var avatar in avatars)
            {
                if (avatar.Object.HasStateAuthority)
                {
                    m_localAvatar = avatar;
                }
                else
                {
                    m_remoteAvatars.Add(avatar);
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
                m_localAvatar = avatar;
                SendAvatarOffset();
            }
            else
            {
                m_remoteAvatars.Add(avatar);
                ParentAvatarToObjectOfInterest(avatar);
                StartCoroutine(SetAvatarToSpawnLocation(avatar));
                DisableNetworkTransform(avatar);
            }
        }

        private void ParentAvatarToObjectOfInterest(AvatarBehaviourFusion avatar)
        {
            avatar.transform.parent = m_objectOfInterest.transform;
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
            if (!m_spawnManagerMotif)
            {
                yield break;
            }

            var clientId = 0;
            foreach (var unused in m_networkRunner.ActivePlayers)
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

            for (var i = 0; i < m_spawnManagerMotif.OccupyingPlayers.Length; i++)
            {
                if (m_spawnManagerMotif.OccupyingPlayers.Get(i) != PlayerRef.None)
                {
                    occupiedCount++;
                }
            }

            return occupiedCount;
        }

        private void ToggleObjectMoved(bool hasMoved)
        {
            m_hasObjectMoved = hasMoved;
        }

        private void Update()
        {
            if (m_hasObjectMoved)
            {
                SendAvatarOffset();
            }

            UpdateRemoteAvatars();
        }

        private void SendAvatarOffset()
        {
            var relativePosition = m_objectOfInterest.transform.InverseTransformPoint(m_localAvatar.transform.position);
            var relativeRotation = Quaternion.Inverse(m_objectOfInterest.transform.rotation) *
                                   m_localAvatar.transform.rotation;

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
            for (var i = m_remoteAvatars.Count - 1; i >= 0; i--)
            {
                var remoteAvatar = m_remoteAvatars[i];
                if (!remoteAvatar || !remoteAvatar.Object)
                {
                    m_remoteAvatars.RemoveAt(i);
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

                var worldPosition = m_objectOfInterest.transform.TransformPoint(newPosition);
                var worldRotation = m_objectOfInterest.transform.rotation * newRotation;

                remoteAvatar.transform.position = worldPosition;
                remoteAvatar.transform.rotation = worldRotation;
            }
        }

        private int GetLocalPlayerIndex()
        {
            for (var i = 0; i < m_spawnManagerMotif.OccupyingPlayers.Length; i++)
            {
                if (m_spawnManagerMotif.OccupyingPlayers.Get(i) == m_networkRunner.LocalPlayer)
                {
                    return i;
                }
            }

            return -1;
        }

        private int GetAvatarIndex(AvatarBehaviourFusion avatar)
        {
            for (var i = 0; i < m_spawnManagerMotif.OccupyingPlayers.Length; i++)
            {
                if (m_spawnManagerMotif.OccupyingPlayers.Get(i) == avatar.Object.StateAuthority)
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

            foreach (var avatar in m_remoteAvatars)
            {
                if (avatar == null || avatar.Object == null || avatar.Object.StateAuthority != player) continue;
                avatarToRemove = avatar;
                break;
            }

            if (avatarToRemove != null)
            {
                m_remoteAvatars.Remove(avatarToRemove);
            }
        }
    }
}
#endif
