// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using Fusion;
using Meta.XR.Samples;
using UnityEngine;

namespace MRMotifs.ColocatedExperiences.Whiteboard
{

    /// <summary>
    /// The WhiteboardManagerMotif maintains an authoritative whiteboard texture.
    /// Clients send immediate drawing commands via RPC_DrawLine for real‑time feedback.
    /// When a new client joins, they can request a snapshot of the current texture.
    /// The state authority sends the snapshot as a series of small RPC chunks.
    /// </summary>
    [MetaCodeSample("MRMotifs-ColocatedExperiences")]
    public class WhiteboardManagerMotif : NetworkBehaviour
    {
        [Header("Whiteboard")]
        [SerializeField] private Color whiteBoardColor = Color.white;
        [SerializeField] public Renderer whiteboardRenderer;

        [Header("Whiteboard Texture Settings")]
        [SerializeField] private int baseTextureWidth = 2048;
        [SerializeField] private int baseTextureHeight = 2048;

        [Tooltip("Number of bytes in each data chunk sent via RPC to new joiners.")]
        [SerializeField] private int maxChunkSize = 480;

        [HideInInspector] public Texture2D whiteboardTexture;
        private int m_textureWidth, m_textureHeight;
        private bool m_hasSpawned;

        public static WhiteboardManagerMotif Instance { get; private set; }

        public override void Spawned()
        {
            Instance = this;

            var boardScale = transform.localScale;
            m_textureWidth = Mathf.RoundToInt(baseTextureWidth * boardScale.x);
            m_textureHeight = Mathf.RoundToInt(baseTextureHeight * boardScale.y);

            whiteboardTexture = new Texture2D(m_textureWidth, m_textureHeight, TextureFormat.RGBA32, false);
            var pixels = new Color[m_textureWidth * m_textureHeight];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = whiteBoardColor;
            }

            whiteboardTexture.SetPixels(pixels);
            whiteboardTexture.Apply();

            if (whiteboardRenderer != null)
            {
                whiteboardRenderer.material.mainTexture = whiteboardTexture;
            }

            m_hasSpawned = true;

            if (!Object.HasStateAuthority)
            {
                RPC_RequestSnapshot();
            }
        }

        private void Update()
        {
            if (!m_hasSpawned)
            {
                return;
            }

            whiteboardTexture.Apply();
        }

        /// <summary>
        /// Converts a world position to UV coordinates (0–1) relative to the whiteboard.
        /// </summary>
        public Vector2 WorldToUV(Vector3 worldPos)
        {
            var localPos = transform.InverseTransformPoint(worldPos);
            return new Vector2(localPos.x + 0.5f, localPos.y + 0.5f);
        }

        /// <summary>
        /// Draws a circular brush stamp at the given UV coordinate.
        /// </summary>
        private void DrawBrushStamp(Vector2 uv, Color color, int brushRadius)
        {
            var pixelX = (int)(uv.x * m_textureWidth);
            var pixelY = (int)(uv.y * m_textureHeight);

            for (var y = pixelY - brushRadius; y <= pixelY + brushRadius; y++)
            {
                if (y < 0 || y >= m_textureHeight)
                {
                    continue;
                }

                for (var x = pixelX - brushRadius; x <= pixelX + brushRadius; x++)
                {
                    if (x < 0 || x >= m_textureWidth)
                    {
                        continue;
                    }

                    if (Vector2.Distance(new Vector2(x, y), new Vector2(pixelX, pixelY)) <= brushRadius)
                    {
                        whiteboardTexture.SetPixel(x, y, color);
                    }
                }
            }
        }

        /// <summary>
        /// Draws a line between two UV coordinates by stamping the brush along the path.
        /// </summary>
        private void DrawLine(Vector2 startUV, Vector2 endUV, Color color, int brushRadius)
        {
            var distance = Vector2.Distance(startUV, endUV);
            var steps = Mathf.Max(1, Mathf.CeilToInt(distance * m_textureWidth));

            for (var i = 0; i <= steps; i++)
            {
                var lerpedUV = Vector2.Lerp(startUV, endUV, i / (float)steps);
                DrawBrushStamp(lerpedUV, color, brushRadius);
            }
        }

        /// <summary>
        /// RPC called on all clients for immediate drawing feedback.
        /// </summary>
        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RPC_DrawLine(Vector2 startUV, Vector2 endUV, Color color, int brushRadius)
        {
            DrawLine(startUV, endUV, color, brushRadius);
        }

        /// <summary>
        /// RPC called by a client to request a snapshot of the whiteboard.
        /// This RPC is sent to the state authority.
        /// </summary>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestSnapshot()
        {
            if (!Object.HasStateAuthority)
            {
                return;
            }

            var snapshotData = whiteboardTexture.EncodeToPNG();
            SendSnapshotChunks(snapshotData);
        }

        /// <summary>
        /// Splits the snapshot data into chunks and sends each chunk via RPC.
        /// </summary>
        private void SendSnapshotChunks(byte[] snapshotData)
        {
            var totalChunks = Mathf.CeilToInt(snapshotData.Length / (float)maxChunkSize);
            for (var i = 0; i < totalChunks; i++)
            {
                var chunkSize = Mathf.Min(maxChunkSize, snapshotData.Length - i * maxChunkSize);
                var chunk = new byte[chunkSize];
                System.Array.Copy(snapshotData, i * maxChunkSize, chunk, 0, chunkSize);
                RPC_SendSnapshotChunk(chunk, i, totalChunks);
            }
        }

        /// <summary>
        /// RPC called on the requesting client to deliver a snapshot chunk.
        /// </summary>
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_SendSnapshotChunk(byte[] chunkData, int chunkIndex, int totalChunks)
        {
            WhiteboardSnapshotReceiverMotif.Instance.ReceiveChunk(chunkData, chunkIndex, totalChunks);
        }
    }
}
#endif
