// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Reflection;
using Meta.XR;
using UnityEngine;

/// <summary>
/// Captures per-frame stereo camera data from Passthrough Camera API and forwards it
/// to a data exporter component for offline processing.
/// </summary>
/// <remarks>
/// This component is scene-agnostic and can be reused across experimental setups. Assign
/// the left and right PassthroughCameraAccess components, plus a DataExporter implementation,
/// via the inspector.
/// </remarks>
public class CameraCapture : MonoBehaviour
{
    /// <summary>
    /// Data container for a single stereo frame capture.
    /// </summary>
    public readonly struct CameraFrameData
    {
        public CameraFrameData(
            Texture leftTexture,
            Texture rightTexture,
            Pose leftPose,
            Pose rightPose,
            object leftIntrinsics,
            object rightIntrinsics,
            double frameTimestampSeconds,
            double? leftTimestampSeconds,
            double? rightTimestampSeconds)
        {
            LeftTexture = leftTexture;
            RightTexture = rightTexture;
            LeftPose = leftPose;
            RightPose = rightPose;
            LeftIntrinsics = leftIntrinsics;
            RightIntrinsics = rightIntrinsics;
            FrameTimestampSeconds = frameTimestampSeconds;
            LeftTimestampSeconds = leftTimestampSeconds;
            RightTimestampSeconds = rightTimestampSeconds;
        }

        /// <summary>Left eye camera texture.</summary>
        public Texture LeftTexture { get; }

        /// <summary>Right eye camera texture.</summary>
        public Texture RightTexture { get; }

        /// <summary>Left camera pose (can be treated as extrinsics).</summary>
        public Pose LeftPose { get; }

        /// <summary>Right camera pose (can be treated as extrinsics).</summary>
        public Pose RightPose { get; }

        /// <summary>Left camera intrinsics object, if exposed by the API.</summary>
        public object LeftIntrinsics { get; }

        /// <summary>Right camera intrinsics object, if exposed by the API.</summary>
        public object RightIntrinsics { get; }

        /// <summary>Unified frame timestamp in seconds.</summary>
        public double FrameTimestampSeconds { get; }

        /// <summary>Left camera timestamp in seconds, if available.</summary>
        public double? LeftTimestampSeconds { get; }

        /// <summary>Right camera timestamp in seconds, if available.</summary>
        public double? RightTimestampSeconds { get; }
    }

    /// <summary>
    /// Implement this interface in a component to handle captured frame data (e.g., serialization,
    /// streaming, or saving to disk). This is intentionally minimal for future extension.
    /// </summary>
    public interface IDataExporter
    {
        void ExportFrame(CameraFrameData frameData);
    }

    [Header("Camera Access")]
    [SerializeField] private PassthroughCameraAccess m_leftCameraAccess;
    [SerializeField] private PassthroughCameraAccess m_rightCameraAccess;

    [Header("Capture Timing")]
    [Tooltip("Capture interval in seconds. Set to 0 to capture every Update.")]
    [SerializeField] private float m_captureIntervalSeconds = 0f;

    [Header("Data Export")]
    [SerializeField] private MonoBehaviour m_dataExporter;

    private double m_nextCaptureTime;
    private IDataExporter m_cachedExporter;

    private static readonly string[] s_intrinsicsMethodNames =
    {
        "GetCameraIntrinsics",
        "TryGetCameraIntrinsics",
        "GetIntrinsics",
        "TryGetIntrinsics"
    };

    private static readonly string[] s_timestampMethodNames =
    {
        "GetTimestampNs",
        "GetTimestamp",
        "GetFrameTimestamp",
        "GetFrameTimestampNs"
    };

    private void Awake()
    {
        if (m_leftCameraAccess == null || m_rightCameraAccess == null)
        {
            Debug.LogError("CameraCapture requires both left and right PassthroughCameraAccess references.");
            enabled = false;
            return;
        }

        m_cachedExporter = m_dataExporter as IDataExporter;
        if (m_dataExporter != null && m_cachedExporter == null)
        {
            Debug.LogError("CameraCapture DataExporter does not implement IDataExporter.");
            enabled = false;
        }
    }

    private void Update()
    {
        if (!IsReadyToCapture())
        {
            return;
        }

        var now = Time.timeAsDouble;
        if (m_captureIntervalSeconds > 0f && now < m_nextCaptureTime)
        {
            return;
        }

        CaptureFrame(now);
        m_nextCaptureTime = now + m_captureIntervalSeconds;
    }

    private bool IsReadyToCapture()
    {
        if (!m_leftCameraAccess.IsPlaying || !m_rightCameraAccess.IsPlaying)
        {
            return false;
        }

        if (m_cachedExporter == null)
        {
            return false;
        }

        return true;
    }

    private void CaptureFrame(double fallbackTimestampSeconds)
    {
        var leftTexture = m_leftCameraAccess.GetTexture();
        var rightTexture = m_rightCameraAccess.GetTexture();

        var leftPose = m_leftCameraAccess.GetCameraPose();
        var rightPose = m_rightCameraAccess.GetCameraPose();

        var leftIntrinsics = TryGetMetadata(m_leftCameraAccess, s_intrinsicsMethodNames, out _);
        var rightIntrinsics = TryGetMetadata(m_rightCameraAccess, s_intrinsicsMethodNames, out _);

        var leftTimestamp = TryGetTimestamp(m_leftCameraAccess);
        var rightTimestamp = TryGetTimestamp(m_rightCameraAccess);

        var frameTimestampSeconds = leftTimestamp ?? rightTimestamp ?? fallbackTimestampSeconds;

        var frameData = new CameraFrameData(
            leftTexture,
            rightTexture,
            leftPose,
            rightPose,
            leftIntrinsics,
            rightIntrinsics,
            frameTimestampSeconds,
            leftTimestamp,
            rightTimestamp);

        m_cachedExporter.ExportFrame(frameData);
    }

    private static object TryGetMetadata(
        PassthroughCameraAccess cameraAccess,
        string[] methodNames,
        out bool resolved)
    {
        resolved = false;

        foreach (var methodName in methodNames)
        {
            var method = cameraAccess.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
            {
                continue;
            }

            if (method.ReturnType != typeof(void) && method.GetParameters().Length == 0)
            {
                resolved = true;
                return method.Invoke(cameraAccess, null);
            }

            if (method.ReturnType == typeof(bool))
            {
                var parameters = method.GetParameters();
                if (parameters.Length == 1 && parameters[0].IsOut)
                {
                    var args = new object[] { null };
                    var success = (bool)method.Invoke(cameraAccess, args);
                    if (success)
                    {
                        resolved = true;
                        return args[0];
                    }
                }
            }
        }

        return null;
    }

    private static double? TryGetTimestamp(PassthroughCameraAccess cameraAccess)
    {
        var result = TryGetMetadata(cameraAccess, s_timestampMethodNames, out var resolved);
        if (!resolved || result == null)
        {
            return null;
        }

        if (result is double timestampSeconds)
        {
            return timestampSeconds;
        }

        if (result is float timestampFloat)
        {
            return timestampFloat;
        }

        if (result is long timestampNanoseconds)
        {
            return timestampNanoseconds / 1_000_000_000.0;
        }

        if (result is int timestampNanosecondsInt)
        {
            return timestampNanosecondsInt / 1_000_000_000.0;
        }

        if (double.TryParse(result.ToString(), out var parsedTimestamp))
        {
            return parsedTimestamp;
        }

        return null;
    }
}
