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
using System.Collections;
using System.Collections.Generic;
using Meta.XR.MultiplayerBlocks.Fusion;
using UnityEngine;

/// <summary>
/// Handles attaching name tags to remote avatars by matching state authority and retrying unassigned tags after a delay.
/// </summary>
public class AvatarNameTagHandlerMotif : NetworkBehaviour
{
    private AvatarMovementHandlerMotif _avatarMovementHandlerMotif;
    private readonly List<AvatarBehaviourFusion> _unassignedNameTags = new();
    private const float NameTagOffset = 0.3f;

    public override void Spawned()
    {
        base.Spawned();
        StartCoroutine(InitializeAvatarHandler());
    }

    private IEnumerator InitializeAvatarHandler()
    {
        while (!_avatarMovementHandlerMotif)
        {
            _avatarMovementHandlerMotif = FindObjectOfType<AvatarMovementHandlerMotif>();
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
        AssignNameTagToAvatar(remoteAvatar);
    }

    private void AssignNameTagToAvatar(AvatarBehaviourFusion remoteAvatar)
    {
        var networkObject = GetComponent<NetworkObject>();

        if (!networkObject || networkObject.StateAuthority != remoteAvatar.Object.StateAuthority)
        {
            _unassignedNameTags.Add(remoteAvatar);
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
            nameTagObject.transform.localPosition = new Vector3(NameTagOffset, 0, 0);
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
            nameTagObject.transform.localPosition = new Vector3(NameTagOffset, 0, 0);
            nameTagObject.transform.localRotation = Quaternion.identity;
        }
    }
}
#endif
