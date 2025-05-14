#if FUSION2
using System;
using System.Linq;
using System.Text;
using Fusion;
using UnityEngine;
using Meta.XR.MRUtilityKit;
using Meta.XR.Samples;

namespace MRMotifs.ColocatedExperiences.SpaceSharing
{
    [MetaCodeSample("MRMotifs-ColocatedExperiences")]
    public class SpaceSharingManager : NetworkBehaviour
    {
        [Networked] private NetworkString<_512> NetworkedRoomUuids { get; set; }
        [Networked] private NetworkString<_256> NetworkedRemoteFloorPose { get; set; }

        private Guid m_sharedAnchorGroupId;

        public override void Spawned()
        {
            PrepareColocation();
        }

        private void PrepareColocation()
        {
            if (Object.HasStateAuthority)
            {
                AdvertiseColocationSession();
            }
            else
            {
                DiscoverNearbySession();
            }
        }

        private async void AdvertiseColocationSession()
        {
            var advertisementData = Encoding.UTF8.GetBytes("MRUKRoomSharing");
            var result = await OVRColocationSession.StartAdvertisementAsync(advertisementData);

            if (!result.Success)
            {
                Debug.LogError($"Motif: [Host] Advertisement failed: {result.Status}");
                return;
            }

            m_sharedAnchorGroupId = result.Value;
            print($"Motif: [Host] Advertisement started. Group UUID: {m_sharedAnchorGroupId}");
            ShareMrukRooms();
        }

        /// <summary>
        /// This method shares all rooms of the host's MRUK instance.
        /// If you would like to only share the current room follow these steps:
        /// 1. var room = MRUK.Instance.GetCurrentRoom()
        /// 2. var result = await room.ShareRoomAsync(m_sharedAnchorGroupId);
        /// </summary>
        private async void ShareMrukRooms()
        {
            var rooms = MRUK.Instance.Rooms;
            if (rooms.Count == 0)
            {
                Debug.LogError("Motif: [Host] No local MRUK rooms found to share.");
                return;
            }

            print($"Motif: [Host] Sharing MRUK rooms: {string.Join(", ", rooms.Select(r => r.Anchor.Uuid))}");
            var result = await MRUK.Instance.ShareRoomsAsync(rooms, m_sharedAnchorGroupId);

            if (!result.Success)
            {
                Debug.LogError($"Motif: [Host] Failed to share MRUK rooms: {result.Status}");
                return;
            }

            print("Motif: [Host] MRUK rooms shared successfully.");
            NetworkedRoomUuids = string.Join(",", rooms.Select(r => r.Anchor.Uuid));
            print($"Motif: [Host] Set NetworkedRoomUuids = {NetworkedRoomUuids}");

            var room0 = rooms[0];
            var pose = room0.FloorAnchor.transform;
            NetworkedRemoteFloorPose =
                $"{pose.position.x},{pose.position.y},{pose.position.z},{pose.rotation.x},{pose.rotation.y},{pose.rotation.z},{pose.rotation.w}";
            print($"Motif: [Host] Set NetworkedRemoteFloorPose = {NetworkedRemoteFloorPose}");
        }

        private async void DiscoverNearbySession()
        {
            OVRColocationSession.ColocationSessionDiscovered += OnColocationSessionDiscovered;
            var result = await OVRColocationSession.StartDiscoveryAsync();

            if (!result.Success)
            {
                Debug.LogError($"Motif: [Client] Discovery failed: {result.Status}");
            }
            else
            {
                print("Motif: [Client] Discovery started successfully.");
            }
        }

        private void OnColocationSessionDiscovered(OVRColocationSession.Data session)
        {
            OVRColocationSession.ColocationSessionDiscovered -= OnColocationSessionDiscovered;
            m_sharedAnchorGroupId = session.AdvertisementUuid;
            print($"Motif: [Client] Discovered session: {m_sharedAnchorGroupId}");
            LoadSharedRoom(m_sharedAnchorGroupId);
        }

        private static Pose ParsePose(string poseString)
        {
            var parts = poseString.Split(',');
            if (parts.Length == 7)
            {
                return new Pose(
                    new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2])),
                    new Quaternion(
                        float.Parse(parts[3]), float.Parse(parts[4]), float.Parse(parts[5]), float.Parse(parts[6]))
                );
            }

            Debug.LogError("Motif: Invalid pose string: " + poseString);
            return default;
        }

        /// <summary>
        /// This method loads the rooms previously shared with the user via MRUK.Instance.ShareRoomsAsync.
        /// If you previously shared only the current room via MRUKRoom.ShareRoomAsync, then you can use
        /// the following method with a different overload, which does not require the all the roomUuids:
        /// LoadSceneFromSharedRooms(null, groupUuid, (currentRoomUuid, remoteFloorWorldPose));
        /// </summary>
        private async void LoadSharedRoom(Guid groupUuid)
        {
            print($"Motif: [Client] Loading shared MRUK room: {groupUuid}");

            var roomUuids = NetworkedRoomUuids.ToString().Split(',').Select(Guid.Parse).ToArray();
            if (roomUuids.Length == 0)
            {
                Debug.LogError("Motif: [Client] No shared MRUK room UUIDs received.");
                return;
            }

            var remotePoseStr = NetworkedRemoteFloorPose.ToString();
            if (string.IsNullOrEmpty(remotePoseStr))
            {
                Debug.LogError("Motif: [Client] No remote floor world pose received.");
                return;
            }

            var remoteFloorWorldPose = ParsePose(remotePoseStr);
            var result = await MRUK.Instance.LoadSceneFromSharedRooms(
                roomUuids, groupUuid, (roomUuids[0], remoteFloorWorldPose));

            if (result == MRUK.LoadDeviceResult.Success)
            {
                print("Motif: [Client] Successfully loaded and aligned to the shared MRUK room.");
            }
            else
            {
                Debug.LogError($"Motif: [Client] Failed to load shared MRUK room: {result}");
            }
        }
    }
}
#endif
