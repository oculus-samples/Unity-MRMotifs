// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using Fusion;
using UnityEngine;
using System.Collections;
using Meta.XR.MultiplayerBlocks.Fusion;
using Meta.XR.Samples;
using MRMotifs.SharedActivities.Avatars;

namespace MRMotifs.SharedActivities.Spawning
{
    /// <summary>
    /// Manages the spawn locations for players in a multiplayer session. Controls the
    /// queuing system for players waiting for an available spawn location and ensures
    /// avatars are placed correctly at available locations.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class SpawnManagerMotif : NetworkBehaviour
    {
        [Tooltip("Stores the PlayerRef of players occupying each spawn location.")]
        [Networked, Capacity(8)]
        public NetworkArray<PlayerRef> OccupyingPlayers => default;

        /// <summary>
        /// Stores the predefined spawn locations in the game scene.
        /// Each position in the array corresponds to a spawn point where players can be placed.
        /// </summary>
        [Networked, Capacity(8)]
        private NetworkArray<Vector3> SpawnLocations => default;

        /// <summary>
        /// Tracks whether each spawn location is currently occupied by a player.
        /// A true value means the location is occupied, false means it is available.
        /// </summary>
        [Networked, Capacity(8)]
        private NetworkArray<NetworkBool> LocationOccupied => default;

        /// <summary>
        /// Tracks players waiting to be placed at a spawn location. The array is used to
        /// manage the queue for players waiting to occupy a location.
        /// </summary>
        [Networked, Capacity(8)]
        private NetworkArray<PlayerRef> QueuedPlayers => default;

        [Tooltip("The player's camera rig that will be positioned at spawn locations.")] [SerializeField]
        private Transform cameraRig;

        [Tooltip("The object in the scene that avatars will interact with or face.")] [SerializeField]
        private GameObject objectOfInterest;

        public GameObject ObjectOfInterest => objectOfInterest;

        public bool HasSpawned { get; private set; }

        public override void Spawned()
        {
            base.Spawned();
            HasSpawned = true;
            FusionBBEvents.OnSceneLoadDone += OnLoaded;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            FusionBBEvents.OnSceneLoadDone -= OnLoaded;
        }

        private void OnLoaded(NetworkRunner runner)
        {
            InitializeSpawnLocations();
        }

        private void InitializeSpawnLocations()
        {
            var spawnPoints = objectOfInterest.GetComponentsInChildren<SpawnPointMotif>();

            for (var i = 0; i < SpawnLocations.Length; i++)
            {
                if (i < spawnPoints.Length)
                {
                    SpawnLocations.Set(i, spawnPoints[i].transform.position);
                    LocationOccupied.Set(i, false);
                    OccupyingPlayers.Set(i, default);
                }
                else
                {
                    SpawnLocations.Set(i, Vector3.zero);
                }
            }
        }

        /// <summary>
        /// This method is called by <see cref="AvatarSpawnerHandlerMotif"/> class, whenever a new player joins
        /// the experience. After the player's avatar entity is spawned, we want to place the player in a spawn
        /// queue before they are requesting a spawn location. This approach is used to prevent players from
        /// joining at the exact same time, which could lead to our system assigning them the same spawn location.
        /// </summary>
        public IEnumerator EnqueuePlayerForSpawn(PlayerRef player, AvatarBehaviourFusion avatarObject)
        {
            AddPlayerToQueue(player);
            yield return StartCoroutine(ProcessSpawnQueue(player, avatarObject));
        }

        private void AddPlayerToQueue(PlayerRef player)
        {
            for (var i = 0; i < QueuedPlayers.Length; i++)
            {
                if (QueuedPlayers.Get(i) != PlayerRef.None)
                {
                    continue;
                }

                RequestModifyQueueRpc(i, player);
                return;
            }
        }

        private IEnumerator ProcessSpawnQueue(PlayerRef player, NetworkBehaviour avatarObject)
        {
            // The random delay is introduced to prevent multiple clients from attempting to occupy the same spawn location
            // simultaneously. Without this, clients could fetch the same location at the exact same time, causing conflicts.
            // This delay helps stagger their requests slightly, reducing the likelihood of this race condition.
            var randomDelay = Random.Range(0.1f, 2.0f);
            yield return new WaitForSeconds(randomDelay);

            while (IsAnotherPlayerAheadInQueue(player))
            {
                yield return new WaitForEndOfFrame();
                yield return null;
            }

            for (var i = 0; i < SpawnLocations.Length; i++)
            {
                if (LocationOccupied.Get(i))
                {
                    continue;
                }

                RequestOccupyLocationRpc(i, player);
                RequestRemovePlayerFromQueueRpc(player);
                MoveAvatarToLocation(avatarObject, SpawnLocations.Get(i));
                break;
            }
        }

        private bool IsAnotherPlayerAheadInQueue(PlayerRef player)
        {
            foreach (var queuedPlayer in QueuedPlayers)
            {
                if (queuedPlayer != PlayerRef.None && queuedPlayer.PlayerId < player.PlayerId)
                {
                    return true;
                }
            }

            return false;
        }

        private void MoveAvatarToLocation(NetworkBehaviour avatarObject, Vector3 location)
        {
            if (!avatarObject.HasStateAuthority)
            {
                return;
            }

            cameraRig.position = location;
            var directionToLook = objectOfInterest.transform.position - cameraRig.position;
            directionToLook.y = 0;
            cameraRig.rotation = Quaternion.LookRotation(directionToLook);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RequestModifyQueueRpc(int queueIndex, PlayerRef player)
        {
            QueuedPlayers.Set(queueIndex, player);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RequestOccupyLocationRpc(int locationIndex, PlayerRef player)
        {
            if (LocationOccupied.Get(locationIndex))
            {
                return;
            }

            OccupyingPlayers.Set(locationIndex, player);
            LocationOccupied.Set(locationIndex, true);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RequestRemovePlayerFromQueueRpc(PlayerRef player)
        {
            for (var i = 0; i < QueuedPlayers.Length; i++)
            {
                if (QueuedPlayers.Get(i) != player)
                {
                    continue;
                }

                ShiftQueueLeftRpc(i);
                return;
            }
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void ShiftQueueLeftRpc(int startIndex)
        {
            for (var i = startIndex; i < QueuedPlayers.Length - 1; i++)
            {
                QueuedPlayers.Set(i, QueuedPlayers.Get(i + 1));
            }

            QueuedPlayers.Set(QueuedPlayers.Length - 1, PlayerRef.None);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void ReleaseLocationRpc(int locationIndex, PlayerRef player)
        {
            if (OccupyingPlayers.Get(locationIndex) != player)
            {
                return;
            }

            LocationOccupied.Set(locationIndex, false);
            OccupyingPlayers.Set(locationIndex, default);
        }

        private void OnDrawGizmos()
        {
            if (!HasSpawned)
            {
                return;
            }

            for (var i = 0; i < SpawnLocations.Length; i++)
            {
                if (SpawnLocations.Get(i) == Vector3.zero)
                {
                    continue;
                }

                Gizmos.color = LocationOccupied.Get(i) ? Color.green : Color.blue;
                Gizmos.DrawSphere(SpawnLocations.Get(i), 0.1f);
            }
        }
    }
}
#endif
