// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using Fusion;
using UnityEngine;
using System.Collections;
using Meta.XR.MultiplayerBlocks.Shared;
using Meta.XR.MultiplayerBlocks.Fusion;
using Meta.XR.Samples;
using MRMotifs.SharedActivities.Spawning;

namespace MRMotifs.SharedActivities.Avatars
{
    /// <summary>
    /// Handles the spawning of avatars in the scene, managing their positions using the spawn manager.
    /// Also, responsible for releasing spawn locations when players leave the scene.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class AvatarSpawnerHandlerMotif : MonoBehaviour
    {
        [Tooltip("Reference to the SpawnManagerMotif, which manages the spawn locations and queues.")]
        [SerializeField]
        private SpawnManagerMotif spawnManagerMotif;

        private NetworkRunner m_networkRunner;

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
            m_networkRunner = networkRunner;
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
                spawnManagerMotif.EnqueuePlayerForSpawn(m_networkRunner.LocalPlayer, avatarNetworkObj));
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

                var avatarHandler = FindAnyObjectByType<AvatarMovementHandlerMotif>();
                if (avatarHandler != null)
                {
                    avatarHandler.RemoveRemoteAvatarByPlayer(player);
                }
            }
        }
    }
}
#endif
