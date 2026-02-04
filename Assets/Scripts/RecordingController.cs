// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles user-controlled recording by enabling/disabling CameraCapture and
/// presenting a simple on-screen status indicator.
/// </summary>
public class RecordingController : MonoBehaviour
{
    [SerializeField] private FileDataExporter m_fileDataExporter;
    [SerializeField] private Text m_statusText;

    [Tooltip("Set a capture interval (seconds) when recording. 0 = every Update.")]
    [SerializeField] private float m_recordingCaptureIntervalSeconds = 0f;

    [Header("Haptics")]
    [SerializeField] private bool m_enableHaptics = true;
    [SerializeField] private float m_hapticAmplitude = 0.2f;
    [SerializeField] private float m_hapticDurationSeconds = 0.05f;

    /// <summary>Indicates whether recording is active.</summary>
    public bool IsRecording { get; private set; }

    private void Awake()
    {
        if (m_fileDataExporter == null)
        {
            Debug.LogError("RecordingController requires a FileDataExporter reference.");
            enabled = false;
            return;
        }

        EnsureStatusText();
        SetRecording(false);
    }

    private void Update()
    {
        if (IsTogglePressed())
        {
            if (IsRecording)
            {
                StopRecording();
            }
            else
            {
                StartRecording();
            }
        }

        UpdateStatusText();
    }

    /// <summary>Begin dataset recording.</summary>
    public void StartRecording() => SetRecording(true);

    /// <summary>Stop dataset recording.</summary>
    public void StopRecording() => SetRecording(false);

    private void SetRecording(bool enabled)
    {
        IsRecording = enabled;
        m_fileDataExporter.EnableExport = enabled;

        if (m_enableHaptics)
        {
            StartCoroutine(PlayHapticPulse());
        }

        UpdateStatusText();
    }

    private bool IsTogglePressed()
    {
        return OVRInput.GetDown(OVRInput.Button.One)
            || OVRInput.GetDown(OVRInput.Button.Three)
            || OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger);
    }

    private void UpdateStatusText()
    {
        if (m_statusText == null)
        {
            return;
        }

        m_statusText.text = $"Recording: {(IsRecording ? "ON" : "OFF")} | Frame: {m_fileDataExporter.ExportedFrameCount:D6}";
    }

    private void EnsureStatusText()
    {
        if (m_statusText != null)
        {
            return;
        }

        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var canvasObject = new GameObject("RecordingStatusCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            canvasObject.transform.position = new Vector3(0f, 1.5f, 1.5f);
            canvasObject.transform.localScale = Vector3.one * 0.002f;
        }

        var textObject = new GameObject("RecordingStatusText");
        textObject.transform.SetParent(canvas.transform, false);
        m_statusText = textObject.AddComponent<Text>();
        m_statusText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        m_statusText.alignment = TextAnchor.MiddleCenter;
        m_statusText.color = Color.white;
        m_statusText.rectTransform.sizeDelta = new Vector2(800f, 100f);
        m_statusText.rectTransform.anchoredPosition = Vector2.zero;
    }

    private IEnumerator PlayHapticPulse()
    {
        OVRInput.SetControllerVibration(m_hapticAmplitude, m_hapticAmplitude, OVRInput.Controller.Active);
        yield return new WaitForSeconds(m_hapticDurationSeconds);
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.Active);
    }
}
