// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using Fusion;
using System.Collections;
using System.Collections.Generic;
using Meta.XR.MultiplayerBlocks.Fusion;
using Meta.XR.Samples;
using UnityEngine;

namespace MRMotifs.SharedActivities.Avatars
{
    /// <summary>
    /// Handles attaching name tags to remote avatars by matching state authority and retrying unassigned tags after a delay.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class AvatarNameTagHandlerMotif : NetworkBehaviour
    {
        private AvatarMovementHandlerMotif m_avatarMovementHandlerMotif;
        private readonly List<AvatarBehaviourFusion> m_unassignedNameTags = new();
        private const float NAME_TAG_OFFSET = 0.3f;

        public override void Spawned()
        {
            base.Spawned();
            StartCoroutine(InitializeAvatarHandler());
        }

        private IEnumerator InitializeAvatarHandler()
        {
            while (!m_avatarMovementHandlerMotif)
            {
                m_avatarMovementHandlerMotif = FindAnyObjectByType<AvatarMovementHandlerMotif>();
                yield return null;
            }

            m_avatarMovementHandlerMotif.OnRemoteAvatarAdded += HandleNewRemoteAvatar;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);

            if (m_avatarMovementHandlerMotif != null)
            {
                m_avatarMovementHandlerMotif.OnRemoteAvatarAdded -= HandleNewRemoteAvatar;
            }
        }

        private void HandleNewRemoteAvatar(AvatarBehaviourFusion remoteAvatar)
        {
            AssignNameTagToAvatar(remoteAvatar);
        }

        private void AssignNameTagToAvatar(AvatarBehaviourFusion remoteAvatar)
        {
            var networkObject = GetComponent<NetworkObject>();

            if (!networkObject || networkObject.StateAuthority != remoteAvatar.Object.StateAuthority)
            {
                m_unassignedNameTags.Add(remoteAvatar);
                return;
            }

            DisableNetworkTransform();
            ParentNameTagToAvatar(remoteAvatar, gameObject);
        }

        private void DisableNetworkTransform()
        {
            var networkTransform = GetComponent<NetworkTransform>();
            if (networkTransform)
            {
                networkTransform.enabled = false;
            }
        }

        private void ParentNameTagToAvatar(AvatarBehaviourFusion remoteAvatar, GameObject nameTagObject)
        {
            var jointHead = remoteAvatar.transform.Find("Joint Head");

            if (jointHead)
            {
                nameTagObject.transform.SetParent(jointHead);
                nameTagObject.transform.localPosition = new Vector3(NAME_TAG_OFFSET, 0, 0);
                nameTagObject.transform.localRotation = Quaternion.identity;
            }
            else
            {
                var avatarCriticalJointCount = 0;
                Transform fallbackJoint = null;

                for (var i = 0; i < remoteAvatar.transform.childCount; i++)
                {
                    var child = remoteAvatar.transform.GetChild(i);
                    if (child.name != "AvatarCriticalJoint") continue;

                    avatarCriticalJointCount++;

                    // The fourth critical joint on the avatar is equivalent with the "Joint Head"
                    if (avatarCriticalJointCount != 4)
                    {
                        continue;
                    }

                    fallbackJoint = child;
                    break;
                }

                if (!fallbackJoint)
                {
                    return;
                }

                nameTagObject.transform.SetParent(fallbackJoint);
                nameTagObject.transform.localPosition = new Vector3(NAME_TAG_OFFSET, 0, 0);
                nameTagObject.transform.localRotation = Quaternion.identity;
            }
        }
    }
}
#endif
