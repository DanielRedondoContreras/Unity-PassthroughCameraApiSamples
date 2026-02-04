// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

/// <summary>
/// Lightweight exporter that logs frame metadata to the Unity Console.
/// Use this to validate that CameraCapture is delivering stereo frames and
/// that pose/timestamp data are reasonable before adding heavier exporters.
/// </summary>
public class DebugDataExporter : MonoBehaviour, CameraCapture.IDataExporter
{
    /// <summary>
    /// Receives captured frame data and logs key fields for debugging.
    /// </summary>
    /// <param name="frameData">Frame metadata emitted by CameraCapture.</param>
    public void ExportFrame(CameraCapture.CameraFrameData frameData)
    {
        var leftValid = frameData.LeftTexture != null;
        var rightValid = frameData.RightTexture != null;

        Debug.Log(
            $"[DebugDataExporter] Timestamp: {frameData.FrameTimestampSeconds:F6}s | " +
            $"LeftTex: {leftValid} | RightTex: {rightValid} | " +
            $"LeftPose: pos {FormatVector(frameData.LeftPose.position)} rot {FormatQuaternion(frameData.LeftPose.rotation)} | " +
            $"RightPose: pos {FormatVector(frameData.RightPose.position)} rot {FormatQuaternion(frameData.RightPose.rotation)}");
    }

    private static string FormatVector(Vector3 vector)
    {
        return $"({vector.x:F4}, {vector.y:F4}, {vector.z:F4})";
    }

    private static string FormatQuaternion(Quaternion quaternion)
    {
        return $"({quaternion.x:F4}, {quaternion.y:F4}, {quaternion.z:F4}, {quaternion.w:F4})";
    }
}
