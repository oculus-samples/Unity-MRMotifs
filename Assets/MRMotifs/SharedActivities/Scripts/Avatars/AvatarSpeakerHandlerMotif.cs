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

#if FUSION2 && PHOTON_VOICE_DEFINED
using Fusion;
using System.Collections;
using System.Collections.Generic;
using Meta.XR.MultiplayerBlocks.Fusion;
using UnityEngine;
using Photon.Voice.Unity;

/// <summary>
/// Handles attaching speakers to remote avatars by matching state authority and retrying unassigned speakers after a delay.
/// </summary>
public class AvatarSpeakerHandlerMotif : NetworkBehaviour
{
    private AvatarMovementHandlerMotif _avatarMovementHandlerMotif;
    private readonly List<AvatarBehaviourFusion> _unassignedSpeakers = new();

    public override void Spawned()
    {
        base.Spawned();
        StartCoroutine(InitializeAvatarHandler());
    }

    private IEnumerator InitializeAvatarHandler()
    {
        while (!_avatarMovementHandlerMotif)
        {
            _avatarMovementHandlerMotif = FindAnyObjectByType<AvatarMovementHandlerMotif>();
            yield return null;
        }

        _avatarMovementHandlerMotif.OnRemoteAvatarAdded += HandleNewRemoteAvatar;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);

        if (_avatarMovementHandlerMotif != null)
        {
            _avatarMovementHandlerMotif.OnRemoteAvatarAdded -= HandleNewRemoteAvatar;
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

        _unassignedSpeakers.Add(remoteAvatar);
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
#endif
