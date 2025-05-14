// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using UnityEngine;
using Meta.XR.Samples;

namespace MRMotifs.ColocatedExperiences.Colocation
{
    [MetaCodeSample("MRMotifs-ColocatedExperiences")]
    public class ColocationManager : MonoBehaviour
    {
        [SerializeField] private SharedSpatialAnchorManager sharedSpatialAnchorManager;

        private Transform m_cameraRigTransform;

        private void Start()
        {
            m_cameraRigTransform = FindAnyObjectByType<OVRCameraRig>().transform;
            sharedSpatialAnchorManager.PrepareColocation();
        }

        /// <summary>
        /// Aligns the player's tracking space and camera rig to the specified anchor.
        /// </summary>
        /// <param name="anchor">The spatial anchor to align to.</param>
        public void AlignUserToAnchor(OVRSpatialAnchor anchor)
        {
            if (!anchor || !anchor.Localized)
            {
                Debug.LogError("Motif: Invalid or un-localized anchor. Cannot align.");
                return;
            }

            var anchorTransform = anchor.transform;

            m_cameraRigTransform.position = anchorTransform.InverseTransformPoint(Vector3.zero);
            m_cameraRigTransform.eulerAngles = new Vector3(0, -anchorTransform.eulerAngles.y, 0);

            Debug.Log("Motif: Alignment complete.");
        }
    }
}
#endif
