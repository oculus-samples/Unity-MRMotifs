// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2 && PHOTON_VOICE_DEFINED
using Fusion;
using System.Collections;
using System.Collections.Generic;
using Meta.XR.MultiplayerBlocks.Fusion;
using Meta.XR.Samples;
using UnityEngine;
using Photon.Voice.Unity;

namespace MRMotifs.SharedActivities.Avatars
{
    /// <summary>
    /// Handles attaching speakers to remote avatars by matching state authority and retrying unassigned speakers after a delay.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class AvatarSpeakerHandlerMotif : NetworkBehaviour
    {
        private AvatarMovementHandlerMotif m_avatarMovementHandlerMotif;
        private readonly List<AvatarBehaviourFusion> m_unassignedSpeakers = new();

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
            AssignSpeakerToAvatar(remoteAvatar);
        }

        private void AssignSpeakerToAvatar(AvatarBehaviourFusion remoteAvatar)
        {
            var speakers = FindObjectsByType<Speaker>(FindObjectsSortMode.None);
            foreach (var speaker in speakers)
            {
                var networkObject = speaker.GetComponent<NetworkObject>();
                if (!networkObject || networkObject.StateAuthority != remoteAvatar.Object.StateAuthority)
                {
                    continue;
                }

                DisableNetworkTransform(speaker);
                ParentSpeakerToAvatar(remoteAvatar, speaker.gameObject);
                return;
            }

            m_unassignedSpeakers.Add(remoteAvatar);
        }

        private void DisableNetworkTransform(Speaker speaker)
        {
            var networkTransform = speaker.GetComponent<NetworkTransform>();
            if (networkTransform)
            {
                networkTransform.enabled = false;
            }
        }

        private void ParentSpeakerToAvatar(AvatarBehaviourFusion remoteAvatar, GameObject speakerObject)
        {
            var jointHead = remoteAvatar.transform.Find("Joint Head");

            if (jointHead)
            {
                speakerObject.transform.SetParent(jointHead);
                speakerObject.transform.localPosition = Vector3.zero;
                speakerObject.transform.localRotation = Quaternion.identity;
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

                speakerObject.transform.SetParent(fallbackJoint);
                speakerObject.transform.localPosition = Vector3.zero;
                speakerObject.transform.localRotation = Quaternion.identity;
            }
        }
    }
}
#endif
