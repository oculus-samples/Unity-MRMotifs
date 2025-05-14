// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace MRMotifs.ColocatedExperiences.Whiteboard
{
    public enum InteractionMode
    {
        None,
        DrawingPointer,
        DrawingRaycast,
        PanelManipulation
    }

    [MetaCodeSample("MRMotifs-ColocatedExperiences")]
    public class InteractionStateManagerMotif : MonoBehaviour
    {
        public static InteractionStateManagerMotif Instance { get; private set; }

        public InteractionMode CurrentMode { get; set; } = InteractionMode.None;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public bool CanDrawWithPointer() => CurrentMode == InteractionMode.None;
        public bool CanDrawWithRaycast() => CurrentMode == InteractionMode.None;
        public bool CanManipulatePanel() => CurrentMode == InteractionMode.None;

        public void SetMode(InteractionMode mode)
        {
            CurrentMode = mode;
        }

        public void ResetMode(InteractionMode mode)
        {
            if (CurrentMode == mode)
            {
                CurrentMode = InteractionMode.None;
            }
        }
    }
}
