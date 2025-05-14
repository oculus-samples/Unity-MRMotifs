// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using Fusion;
using System;
using UnityEngine;
using Meta.XR.Samples;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MRMotifs.ColocatedExperiences.Colocation
{
    [MetaCodeSample("MRMotifs-ColocatedExperiences")]
    public class SharedSpatialAnchorManager : NetworkBehaviour
    {
        private ColocationManager m_colocationManager;
        private Guid m_sharedAnchorGroupId;

        public override void Spawned()
        {
            base.Spawned();
            m_colocationManager = FindAnyObjectByType<ColocationManager>();
            PrepareColocation();
        }

        private void PrepareColocation()
        {
            if (Object.HasStateAuthority)
            {
                Debug.Log("Motif: Starting advertisement...");
                AdvertiseColocationSession();
            }
            else
            {
                Debug.Log("Motif: Starting discovery...");
                DiscoverNearbySession();
            }
        }

        private async void AdvertiseColocationSession()
        {
            // Optional advertisement data, e.g. the name of the session or a message to other clients (max. size: 1024 bytes).
            // We can also leave it empty with "null".
            // var colocationSessionData = Encoding.UTF8.GetBytes("SharedSpatialAnchorSession");
            var startAdvertisementResult = await OVRColocationSession.StartAdvertisementAsync(null);

            if (startAdvertisementResult.Success)
            {
                m_sharedAnchorGroupId = startAdvertisementResult.Value;
                Debug.Log($"Motif: Advertisement started successfully. UUID: {m_sharedAnchorGroupId}");
                CreateAndShareAlignmentAnchor();
            }
            else
            {
                Debug.LogError($"Motif: Advertisement failed with status: {startAdvertisementResult.Status}");
            }
        }

        private async void DiscoverNearbySession()
        {
            OVRColocationSession.ColocationSessionDiscovered += OnColocationSessionDiscovered;

            var discoveryResult = await OVRColocationSession.StartDiscoveryAsync();
            if (!discoveryResult.Success)
            {
                Debug.LogError($"Motif: Discovery failed with status: {discoveryResult.Status}");
                return;
            }

            Debug.Log("Motif: Discovery started successfully.");
        }

        private void OnColocationSessionDiscovered(OVRColocationSession.Data session)
        {
            OVRColocationSession.ColocationSessionDiscovered -= OnColocationSessionDiscovered;

            m_sharedAnchorGroupId = session.AdvertisementUuid;
            Debug.Log($"Motif: Discovered session with UUID: {m_sharedAnchorGroupId}");
            LoadAndAlignToAnchor(m_sharedAnchorGroupId);
        }

        private async void CreateAndShareAlignmentAnchor()
        {
            Debug.Log("Motif: Creating alignment anchor...");
            var anchor = await CreateAnchor(Vector3.zero, Quaternion.identity);

            if (anchor == null)
            {
                Debug.LogError("Motif: Failed to create alignment anchor.");
                return;
            }

            if (!anchor.Localized)
            {
                Debug.LogError("Motif: Anchor is not localized. Cannot proceed with sharing.");
                return;
            }

            var saveResult = await anchor.SaveAnchorAsync();
            if (!saveResult.Success)
            {
                Debug.LogError($"Motif: Failed to save alignment anchor. Error: {saveResult}");
                return;
            }

            Debug.Log($"Motif: Alignment anchor saved successfully. UUID: {anchor.Uuid}");
            Debug.Log("Motif: Attempting to share alignment anchor...");
            var shareResult = await OVRSpatialAnchor.ShareAsync(new List<OVRSpatialAnchor> { anchor }, m_sharedAnchorGroupId);

            if (!shareResult.Success)
            {
                Debug.LogError($"Motif: Failed to share alignment anchor. Error: {shareResult}");
                return;
            }

            Debug.Log($"Motif: Alignment anchor shared successfully. Group UUID: {m_sharedAnchorGroupId}");
        }

        private async Task<OVRSpatialAnchor> CreateAnchor(Vector3 position, Quaternion rotation)
        {
            var anchorGameObject = new GameObject("Motif: Alignment Anchor")
            {
                transform = { position = position, rotation = rotation }
            };

            var spatialAnchor = anchorGameObject.AddComponent<OVRSpatialAnchor>();
            while (!spatialAnchor.Created)
            {
                await Task.Yield();
            }

            Debug.Log($"Motif: Anchor created successfully. UUID: {spatialAnchor.Uuid}");
            return spatialAnchor;
        }

        private async void LoadAndAlignToAnchor(Guid groupUuid)
        {
            Debug.Log($"Motif: Loading anchors for Group UUID: {groupUuid}...");
            var unboundAnchors = new List<OVRSpatialAnchor.UnboundAnchor>();
            var loadResult = await OVRSpatialAnchor.LoadUnboundSharedAnchorsAsync(groupUuid, unboundAnchors);

            if (!loadResult.Success || unboundAnchors.Count == 0)
            {
                Debug.LogError($"Motif: Failed to load anchors. Success: {loadResult.Success}, Count: {unboundAnchors.Count}");
                return;
            }

            foreach (var unboundAnchor in unboundAnchors)
            {
                if (await unboundAnchor.LocalizeAsync())
                {
                    Debug.Log($"Motif: Anchor localized successfully. UUID: {unboundAnchor.Uuid}");

                    var anchorGameObject = new GameObject($"Anchor_{unboundAnchor.Uuid}");
                    var spatialAnchor = anchorGameObject.AddComponent<OVRSpatialAnchor>();
                    unboundAnchor.BindTo(spatialAnchor);

                    m_colocationManager.AlignUserToAnchor(spatialAnchor);
                    return;
                }

                Debug.LogWarning($"Motif: Failed to localize anchor: {unboundAnchor.Uuid}");
            }
        }
    }
}
#endif
