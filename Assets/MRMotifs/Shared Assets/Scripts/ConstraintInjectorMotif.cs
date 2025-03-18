// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;

namespace MRMotifs.SharedAssets
{
    /// <summary>
    /// Injects rotation constraints into the <see cref="GrabFreeTransformer"/> component at runtime.
    /// Used to limit the rotation of the chess board and movie screen in the samples scenes of this MR Motif.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedAssets")]
    public class ConstraintInjectorMotif : MonoBehaviour
    {
        [Tooltip("Optional rotation constraints to be injected into the GrabFreeTransformer component.")]
        [SerializeField]
        private TransformerUtils.RotationConstraints rotationConstraints;

        private GrabFreeTransformer m_grabFreeTransformer;

        private void Update()
        {
            if (m_grabFreeTransformer)
            {
                return;
            }

            m_grabFreeTransformer = gameObject.GetComponent<GrabFreeTransformer>();

            if (!m_grabFreeTransformer)
            {
                return;
            }

            m_grabFreeTransformer.InjectOptionalRotationConstraints(rotationConstraints);
            enabled = false;
        }
    }
}
