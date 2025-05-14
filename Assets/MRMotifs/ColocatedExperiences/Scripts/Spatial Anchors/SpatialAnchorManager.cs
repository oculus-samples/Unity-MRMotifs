// Copyright (c) Meta Platforms, Inc. and affiliates.

using TMPro;
using UnityEngine;
using System.Threading.Tasks;
using Meta.XR.Samples;

namespace MRMotifs.ColocatedExperiences.SpatialAnchors
{
    [RequireComponent(typeof(SpatialAnchorLoader))]
    [MetaCodeSample("MRMotifs-ColocatedExperiences")]
    public class SpatialAnchorManager : MonoBehaviour
    {
        [SerializeField] private OVRSpatialAnchor anchorPrefab;

        private OVRSpatialAnchor m_lastCreatedAnchor;

        private void Update()
        {
            HandleUserInput();
        }

        private void HandleUserInput()
        {
            if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
            {
                var position = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
                var rotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch);
                CreateSpatialAnchor(position, rotation);
            }

            if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
            {
                SaveLastCreatedAnchor();
            }

            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
            {
                EraseLastAnchorAsync();
            }
        }

        private async void CreateSpatialAnchor(Vector3 position, Quaternion rotation)
        {
            var anchorObject = Instantiate(anchorPrefab.gameObject, position, rotation);
            m_lastCreatedAnchor = anchorObject.GetComponent<OVRSpatialAnchor>();

            if (!m_lastCreatedAnchor)
            {
                Destroy(anchorObject);
                return;
            }

            while (!m_lastCreatedAnchor.Created)
            {
                await Task.Yield();
            }

            UpdateAnchorInfo(m_lastCreatedAnchor, "Created");
        }

        private async void SaveLastCreatedAnchor()
        {
            if (!m_lastCreatedAnchor)
            {
                return;
            }

            while (!m_lastCreatedAnchor.Created)
            {
                await Task.Yield();
            }

            var result = await m_lastCreatedAnchor.SaveAnchorAsync();
            if (result.Success)
            {
                SpatialAnchorStorage.SaveUuidToPlayerPrefs(m_lastCreatedAnchor.Uuid);
                UpdateAnchorInfo(m_lastCreatedAnchor, "Saved");
            }
            else
            {
                UpdateAnchorInfo(m_lastCreatedAnchor, "Saving failed");
            }
        }

        private async void EraseLastAnchorAsync()
        {
            if (!m_lastCreatedAnchor)
            {
                return;
            }

            var result = await m_lastCreatedAnchor.EraseAnchorAsync();
            if (!result.Success)
            {
                return;
            }

            Destroy(m_lastCreatedAnchor.gameObject);
            m_lastCreatedAnchor = null;
        }

        private void UpdateAnchorInfo(OVRSpatialAnchor anchor, string statusText)
        {
            var canvas = anchor.gameObject.GetComponentInChildren<Canvas>();
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
            textComponents[1].text = statusText;
        }
    }
}
