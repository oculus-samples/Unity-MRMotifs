// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace MRMotifs.ColocatedExperiences.Editor
{
    using UnityEditor;


    [MetaCodeSample("MRMotifs-ColocatedExperiences")]
    public class ClearAndPrintPlayerPrefsEditor : Editor
    {
        [MenuItem("MR Motifs/PlayerPrefs/Clear PlayerPrefs")]
        public static void ClearPlayerPrefs()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            var numUuids = PlayerPrefs.GetInt("numUuids", 0);
            Debug.Log($"Cleared all {numUuids} PlayerPrefs.");
        }

        [MenuItem("MR Motifs/PlayerPrefs/Print PlayerPrefs")]
        public static void PrintPlayerPrefs()
        {
            if (PlayerPrefs.HasKey("numUuids"))
            {
                var numUuids = PlayerPrefs.GetInt("numUuids", 0);
                Debug.Log($"Number of saved UUIDs: {numUuids}");

                for (var i = 0; i < numUuids; i++)
                {
                    var uuidKey = $"uuid{i}";
                    if (!PlayerPrefs.HasKey(uuidKey))
                    {
                        continue;
                    }

                    var uuidValue = PlayerPrefs.GetString(uuidKey);
                    Debug.Log($"{uuidKey}: {uuidValue}");
                }
            }
            else
            {
                Debug.Log("No PlayerPrefs found.");
            }
        }
    }
}
