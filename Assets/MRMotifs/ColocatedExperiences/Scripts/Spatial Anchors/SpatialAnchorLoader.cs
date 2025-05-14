// Copyright (c) Meta Platforms, Inc. and affiliates.

using TMPro;
using System;
using UnityEngine;
using System.Collections.Generic;
using Meta.XR.Samples;

namespace MRMotifs.ColocatedExperiences.SpatialAnchors
{
    [MetaCodeSample("MRMotifs-ColocatedExperiences")]
    public class SpatialAnchorLoader : MonoBehaviour
    {
        [SerializeField] private OVRSpatialAnchor anchorPrefab;

        private void Update()
        {
            if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
            {
                LoadAnchors();
            }
        }

        private void LoadAnchors()
        {
            var uuids = SpatialAnchorStorage.LoadAllUuidsFromPlayerPrefs();
            LoadAnchorsByUuid(uuids);
        }

        private async void LoadAnchorsByUuid(List<Guid> uuids)
        {
            if (uuids == null || uuids.Count == 0)
            {
                return;
            }

            var unboundAnchors = new List<OVRSpatialAnchor.UnboundAnchor>();
            var loadResult = await OVRSpatialAnchor.LoadUnboundAnchorsAsync(uuids, unboundAnchors);

            if (!loadResult.Success)
            {
                return;
            }

            foreach (var unboundAnchor in unboundAnchors)
            {
                if (await unboundAnchor.LocalizeAsync())
                {
                    BindAnchor(unboundAnchor);
                }
            }
        }

        private void BindAnchor(OVRSpatialAnchor.UnboundAnchor unboundAnchor)
        {
            if (!unboundAnchor.TryGetPose(out var pose))
            {
                return;
            }

            var anchorObject = Instantiate(anchorPrefab.gameObject, pose.position, pose.rotation);
            var spatialAnchor = anchorObject.GetComponent<OVRSpatialAnchor>();

            if (spatialAnchor)
            {
                unboundAnchor.BindTo(spatialAnchor);
                InitializeAnchorUI(spatialAnchor, "Loaded and Bound");
            }
            else
            {
                Destroy(anchorObject);
            }
        }

        private void InitializeAnchorUI(OVRSpatialAnchor anchor, string status)
        {
            var canvas = anchor.GetComponentInChildren<Canvas>();
            if (!canvas)
            {
                return;
            }

            var textComponents = canvas.GetComponentsInChildren<TextMeshProUGUI>();
            if (textComponents.Length < 2)
            {
                return;
            }

            textComponents[0].text = $"UUID: {anchor.Uuid}";
            textComponents[1].text = status;
        }
    }
}
