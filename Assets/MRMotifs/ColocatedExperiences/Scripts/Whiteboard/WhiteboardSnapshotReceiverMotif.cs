// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using System.Collections.Generic;
using Meta.XR.Samples;
using UnityEngine;

namespace MRMotifs.ColocatedExperiences.Whiteboard
{

    [MetaCodeSample("MRMotifs-ColocatedExperiences")]
    public class WhiteboardSnapshotReceiverMotif : MonoBehaviour
    {
        public static WhiteboardSnapshotReceiverMotif Instance { get; private set; }
        private readonly Dictionary<int, byte[]> m_receivedChunks = new();
        private int m_totalChunks = -1;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void ReceiveChunk(byte[] chunk, int chunkIndex, int totalChunks)
        {
            m_totalChunks = totalChunks;
            m_receivedChunks[chunkIndex] = chunk;

            if (m_receivedChunks.Count == m_totalChunks)
            {
                AssembleTexture();
            }
        }

        private void AssembleTexture()
        {
            var fullData = new List<byte>();
            for (var i = 0; i < m_totalChunks; i++)
            {
                fullData.AddRange(m_receivedChunks[i]);
            }

            m_receivedChunks.Clear();
            m_totalChunks = -1;
            ApplySnapshot(fullData.ToArray());
        }

        private void ApplySnapshot(byte[] snapshotData)
        {
            var newTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            newTex.LoadImage(snapshotData);
            newTex.Apply();

            if (WhiteboardManagerMotif.Instance == null || WhiteboardManagerMotif.Instance.whiteboardRenderer == null)
            {
                return;
            }

            WhiteboardManagerMotif.Instance.whiteboardRenderer.material.mainTexture = newTex;
            WhiteboardManagerMotif.Instance.whiteboardTexture = newTex;
        }
    }
}
#endif
