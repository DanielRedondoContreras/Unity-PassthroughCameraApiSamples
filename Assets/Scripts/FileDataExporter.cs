// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Exports captured stereo frames to disk for offline processing.
///
/// File layout (relative to Application.persistentDataPath):
/// ExportedData/
///   session_yyyyMMdd_HHmmss/
///     frame_000001/
///       left.png
///       right.png
///       metadata.json
/// </summary>
public class FileDataExporter : MonoBehaviour, CameraCapture.IDataExporter
{
    /// <summary>Controls whether incoming frames are exported.</summary>
    public bool EnableExport { get; set; } = false;

    /// <summary>Number of frames successfully written to disk.</summary>
    public int ExportedFrameCount { get; private set; }

    [Tooltip("Maximum number of frames to export per Update.")]
    [SerializeField] private int m_maxExportsPerFrame = 1;

    [Tooltip("Maximum number of queued frames before dropping the oldest.")]
    [SerializeField] private int m_maxQueuedFrames = 300;

    private readonly Queue<QueuedFrame> m_pendingFrames = new();
    private int m_frameIndex;
    private string m_rootDirectory;
    private string m_sessionDirectory;
    private float m_nextQueueWarningTime;

    private Texture2D m_leftReadback;
    private Texture2D m_rightReadback;

    private void Awake()
    {
        m_rootDirectory = Path.Combine(Application.persistentDataPath, "ExportedData");
        Directory.CreateDirectory(m_rootDirectory);
        var sessionName = $"session_{DateTime.Now:yyyyMMdd_HHmmss}";
        m_sessionDirectory = Path.Combine(m_rootDirectory, sessionName);
        Directory.CreateDirectory(m_sessionDirectory);
    }

    /// <summary>
    /// Queue a frame for export. The actual disk write happens incrementally in Update
    /// to avoid long blocking operations on the main thread.
    /// </summary>
    public void ExportFrame(CameraCapture.CameraFrameData frameData)
    {
        if (!EnableExport)
        {
            return;
        }

        if (m_pendingFrames.Count >= m_maxQueuedFrames)
        {
            m_pendingFrames.Dequeue();
            if (Time.unscaledTime >= m_nextQueueWarningTime)
            {
                Debug.LogWarning("FileDataExporter queue is full; dropping oldest frame.");
                m_nextQueueWarningTime = Time.unscaledTime + 1f;
            }
        }

        m_pendingFrames.Enqueue(new QueuedFrame(frameData, m_frameIndex++));
    }

    private void Update()
    {
        var exportsThisFrame = 0;
        while (m_pendingFrames.Count > 0 && exportsThisFrame < m_maxExportsPerFrame)
        {
            var queued = m_pendingFrames.Dequeue();
            ExportQueuedFrame(queued);
            exportsThisFrame++;
        }
    }

    private void ExportQueuedFrame(QueuedFrame queued)
    {
        var frameFolderName = $"frame_{queued.FrameIndex:D6}";
        var frameFolderPath = Path.Combine(m_sessionDirectory, frameFolderName);
        Directory.CreateDirectory(frameFolderPath);

        var leftPath = Path.Combine(frameFolderPath, "left.png");
        var rightPath = Path.Combine(frameFolderPath, "right.png");
        var metadataPath = Path.Combine(frameFolderPath, "metadata.json");

        try
        {
            if (queued.FrameData.LeftTexture != null)
            {
                var leftBytes = EncodeTextureToPng(queued.FrameData.LeftTexture, ref m_leftReadback);
                if (leftBytes != null)
                {
                    File.WriteAllBytes(leftPath, leftBytes);
                }
            }

            if (queued.FrameData.RightTexture != null)
            {
                var rightBytes = EncodeTextureToPng(queued.FrameData.RightTexture, ref m_rightReadback);
                if (rightBytes != null)
                {
                    File.WriteAllBytes(rightPath, rightBytes);
                }
            }

            var metadata = new FrameMetadata(queued);
            var json = JsonUtility.ToJson(metadata, true);
            File.WriteAllText(metadataPath, json);
            ExportedFrameCount++;
        }
        catch (Exception ex)
        {
            Debug.LogError($"FileDataExporter failed to write frame {queued.FrameIndex}: {ex.Message}");
        }
    }

    private static byte[] EncodeTextureToPng(Texture source, ref Texture2D readbackTexture)
    {
        if (source == null)
        {
            return null;
        }

        var width = source.width;
        var height = source.height;

        if (width <= 0 || height <= 0)
        {
            return null;
        }

        if (readbackTexture == null || readbackTexture.width != width || readbackTexture.height != height)
        {
            readbackTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        }

        var renderTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        var previous = RenderTexture.active;
        try
        {
            Graphics.Blit(source, renderTexture);
            RenderTexture.active = renderTexture;
            readbackTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            readbackTexture.Apply();
            return readbackTexture.EncodeToPNG();
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);
        }
    }

    [Serializable]
    private struct SerializablePose
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public SerializablePose(Pose pose)
        {
            Position = pose.position;
            Rotation = pose.rotation;
        }
    }

    [Serializable]
    private struct FrameMetadata
    {
        public int FrameIndex;
        public double TimestampSeconds;
        public SerializablePose LeftPose;
        public SerializablePose RightPose;
        public int LeftTextureWidth;
        public int LeftTextureHeight;
        public int RightTextureWidth;
        public int RightTextureHeight;
        public double? LeftTimestampSeconds;
        public double? RightTimestampSeconds;
        public string LeftIntrinsics;
        public string RightIntrinsics;

        public FrameMetadata(QueuedFrame queued)
        {
            FrameIndex = queued.FrameIndex;
            TimestampSeconds = queued.FrameData.FrameTimestampSeconds;
            LeftPose = new SerializablePose(queued.FrameData.LeftPose);
            RightPose = new SerializablePose(queued.FrameData.RightPose);
            LeftTextureWidth = queued.FrameData.LeftTexture != null ? queued.FrameData.LeftTexture.width : 0;
            LeftTextureHeight = queued.FrameData.LeftTexture != null ? queued.FrameData.LeftTexture.height : 0;
            RightTextureWidth = queued.FrameData.RightTexture != null ? queued.FrameData.RightTexture.width : 0;
            RightTextureHeight = queued.FrameData.RightTexture != null ? queued.FrameData.RightTexture.height : 0;
            LeftTimestampSeconds = queued.FrameData.LeftTimestampSeconds;
            RightTimestampSeconds = queued.FrameData.RightTimestampSeconds;
            LeftIntrinsics = queued.FrameData.LeftIntrinsics?.ToString() ?? string.Empty;
            RightIntrinsics = queued.FrameData.RightIntrinsics?.ToString() ?? string.Empty;
        }
    }

    private readonly struct QueuedFrame
    {
        public QueuedFrame(CameraCapture.CameraFrameData frameData, int frameIndex)
        {
            FrameData = frameData;
            FrameIndex = frameIndex;
        }

        public CameraCapture.CameraFrameData FrameData { get; }
        public int FrameIndex { get; }
    }
}
