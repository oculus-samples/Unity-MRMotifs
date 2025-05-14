// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using UnityEngine;
using System.Collections.Generic;
using Meta.XR.Samples;

namespace MRMotifs.ColocatedExperiences.SpatialAnchors
{
    [MetaCodeSample("MRMotifs-ColocatedExperiences")]
    public static class SpatialAnchorStorage
    {
        private const string NUM_UUIDS_KEY = "numUuids";
        private const string UUID_KEY_PREFIX = "uuid";

        public static void SaveUuidToPlayerPrefs(Guid uuid)
        {
            var count = PlayerPrefs.GetInt(NUM_UUIDS_KEY, 0);
            PlayerPrefs.SetString($"{UUID_KEY_PREFIX}{count}", uuid.ToString());
            PlayerPrefs.SetInt(NUM_UUIDS_KEY, count + 1);
            PlayerPrefs.Save();
            Debug.Log($"Saved UUID {uuid} to PlayerPrefs.");
        }

        public static List<Guid> LoadAllUuidsFromPlayerPrefs()
        {
            var count = PlayerPrefs.GetInt(NUM_UUIDS_KEY, 0);
            var uuids = new List<Guid>();

            for (var i = 0; i < count; i++)
            {
                var uuidStr = PlayerPrefs.GetString($"{UUID_KEY_PREFIX}{i}", string.Empty);
                if (Guid.TryParse(uuidStr, out var uuid))
                {
                    uuids.Add(uuid);
                }
            }

            return uuids;
        }

        public static void ClearAllUuids()
        {
            var count = PlayerPrefs.GetInt(NUM_UUIDS_KEY, 0);
            for (var i = 0; i < count; i++)
            {
                PlayerPrefs.DeleteKey($"{UUID_KEY_PREFIX}{i}");
            }

            PlayerPrefs.DeleteKey(NUM_UUIDS_KEY);
            PlayerPrefs.Save();
            Debug.Log("Cleared all UUIDs from PlayerPrefs.");
        }
    }
}
