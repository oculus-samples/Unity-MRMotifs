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
using System.Collections;
using Meta.XR.MultiplayerBlocks.Shared;
using Meta.XR.MultiplayerBlocks.Fusion;

/// <summary>
/// Handles the spawning of avatars in the scene, managing their positions using the spawn manager.
/// Also, responsible for releasing spawn locations when players leave the scene.
/// </summary>
public class AvatarSpawnerHandlerMotif : MonoBehaviour
{
    [Tooltip("Reference to the SpawnManagerMotif, which manages the spawn locations and queues.")]
    [SerializeField] private SpawnManagerMotif spawnManagerMotif;

    private NetworkRunner _networkRunner;

    private void Awake()
    {
        FusionBBEvents.OnSceneLoadDone += OnLoaded;
        AvatarEntity.OnSpawned += HandleAvatarSpawned;
        FusionBBEvents.OnPlayerLeft += FreeSpawnLocation;
    }

    private void OnDestroy()
    {
        FusionBBEvents.OnSceneLoadDone -= OnLoaded;
        AvatarEntity.OnSpawned -= HandleAvatarSpawned;
        FusionBBEvents.OnPlayerLeft -= FreeSpawnLocation;
    }

    private void OnLoaded(NetworkRunner networkRunner)
    {
        _networkRunner = networkRunner;
    }

    private void HandleAvatarSpawned(AvatarEntity avatarEntity)
    {
        StartCoroutine(WaitForSpawnedAndEnqueue(avatarEntity));
    }

    private IEnumerator WaitForSpawnedAndEnqueue(AvatarEntity avatarEntity)
    {
        // Additional delay required since Avatars v28+ require some additional time to be loaded
        // No event to await the full "readiness" of the avatar is available yet
        yield return new WaitForSeconds(1.5f);

        while (!spawnManagerMotif.HasSpawned)
        {
            yield return null;
        }

        var avatarNetworkObj = avatarEntity.gameObject.GetComponent<AvatarBehaviourFusion>();
        if (!avatarNetworkObj.HasStateAuthority)
        {
            yield break;
        }

        yield return spawnManagerMotif.StartCoroutine(
            spawnManagerMotif.EnqueuePlayerForSpawn(_networkRunner.LocalPlayer, avatarNetworkObj));
    }

    private void FreeSpawnLocation(NetworkRunner runner, PlayerRef player)
    {
        for (var i = 0; i < spawnManagerMotif.OccupyingPlayers.Length; i++)
        {
            if (spawnManagerMotif.OccupyingPlayers.Get(i) != player)
            {
                continue;
            }

            spawnManagerMotif.ReleaseLocationRpc(i, player);

            var avatarHandler = FindObjectOfType<AvatarMovementHandlerMotif>();
            if (avatarHandler != null)
            {
                avatarHandler.RemoveRemoteAvatarByPlayer(player);
            }
        }
    }
}
#endif
