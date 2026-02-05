// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a non-interactive, world-space UI overlay with recording guidance and status.
/// </summary>
public class RecordingUIOverlay : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private Vector3 m_anchorOffset = new(0f, 0f, 1.5f);
    [SerializeField] private float m_canvasScale = 0.002f;
    [SerializeField] private Vector2 m_infoOffset = new(0f, 120f);
    [SerializeField] private Vector2 m_instructionsOffset = new(0f, -40f);
    [SerializeField] private Vector2 m_recordingOffset = new(0f, 260f);

    [Header("Behavior")]
    [SerializeField] private float m_confirmationDurationSeconds = 2.5f;
    [SerializeField] private RecordingController m_recordingController;
    [SerializeField] private Transform m_trackingAnchor;

    private Canvas m_canvas;
    private Text m_infoText;
    private Text m_instructionsText;
    private Text m_recordingText;
    private Text m_confirmationText;

    private bool m_wasRecording;
    private Coroutine m_confirmationRoutine;

    private void Awake()
    {
        if (m_recordingController == null)
        {
            m_recordingController = FindFirstObjectByType<RecordingController>();
        }

        ResolveTrackingAnchor();
        BuildCanvas();
        UpdateState(force: true);
    }

    private void Update()
    {
        UpdateCanvasPose();
        UpdateState(force: false);
    }

    private void UpdateState(bool force)
    {
        var isRecording = m_recordingController != null && m_recordingController.IsRecording;
        if (!force && isRecording == m_wasRecording)
        {
            return;
        }

        if (isRecording)
        {
            ShowRecordingState();
        }
        else if (m_wasRecording)
        {
            ShowConfirmationState();
        }
        else
        {
            ShowIdleState();
        }

        m_wasRecording = isRecording;
    }

    private void ShowIdleState()
    {
        SetActive(m_infoText, true);
        SetActive(m_instructionsText, true);
        SetActive(m_recordingText, false);
        SetActive(m_confirmationText, false);
    }

    private void ShowRecordingState()
    {
        if (m_confirmationRoutine != null)
        {
            StopCoroutine(m_confirmationRoutine);
            m_confirmationRoutine = null;
        }

        SetActive(m_infoText, false);
        SetActive(m_instructionsText, false);
        SetActive(m_recordingText, true);
        SetActive(m_confirmationText, false);
    }

    private void ShowConfirmationState()
    {
        if (m_confirmationRoutine != null)
        {
            StopCoroutine(m_confirmationRoutine);
        }

        SetActive(m_infoText, true);
        SetActive(m_instructionsText, false);
        SetActive(m_recordingText, false);
        SetActive(m_confirmationText, true);
        m_confirmationRoutine = StartCoroutine(HideConfirmationAfterDelay());
    }

    private IEnumerator HideConfirmationAfterDelay()
    {
        yield return new WaitForSeconds(m_confirmationDurationSeconds);
        m_confirmationRoutine = null;
        ShowIdleState();
    }

    private void BuildCanvas()
    {
        var canvasObject = new GameObject("RecordingUIOverlayCanvas");
        canvasObject.transform.SetParent(transform, false);
        canvasObject.transform.localScale = Vector3.one * m_canvasScale;

        m_canvas = canvasObject.AddComponent<Canvas>();
        m_canvas.renderMode = RenderMode.WorldSpace;
        canvasObject.AddComponent<CanvasScaler>();

        m_infoText = CreateText(
            canvasObject.transform,
            "InfoText",
            "TFG_41\nDaniel_Redondo\nCapture&Export_PassthroughCamera",
            new Vector2(900f, 220f),
            m_infoOffset,
            46,
            TextAnchor.MiddleCenter);

        m_instructionsText = CreateText(
            canvasObject.transform,
            "InstructionsText",
            "Iniciar grabación: A / X\nDetener grabación: B / Y",
            new Vector2(900f, 160f),
            m_instructionsOffset,
            40,
            TextAnchor.MiddleCenter);

        m_recordingText = CreateText(
            canvasObject.transform,
            "RecordingText",
            "Grabando...",
            new Vector2(420f, 80f),
            m_recordingOffset,
            38,
            TextAnchor.MiddleCenter);

        m_confirmationText = CreateText(
            canvasObject.transform,
            "ConfirmationText",
            "Su grabación se ha guardado correctamente",
            new Vector2(900f, 140f),
            new Vector2(0f, -40f),
            36,
            TextAnchor.MiddleCenter);
    }

    private static Text CreateText(
        Transform parent,
        string name,
        string text,
        Vector2 size,
        Vector2 anchoredPosition,
        int fontSize,
        TextAnchor alignment)
    {
        var textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        var textComponent = textObject.AddComponent<Text>();
        textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.alignment = alignment;
        textComponent.color = Color.white;
        textComponent.raycastTarget = false;

        var rectTransform = textComponent.rectTransform;
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = anchoredPosition;

        return textComponent;
    }

    private static void SetActive(Text text, bool active)
    {
        if (text != null)
        {
            text.gameObject.SetActive(active);
        }
    }

    private void ResolveTrackingAnchor()
    {
        if (m_trackingAnchor != null)
        {
            return;
        }

        if (Camera.main != null)
        {
            m_trackingAnchor = Camera.main.transform;
        }
    }

    private void UpdateCanvasPose()
    {
        if (m_canvas == null || m_trackingAnchor == null)
        {
            return;
        }

        var canvasTransform = m_canvas.transform;
        canvasTransform.position = m_trackingAnchor.TransformPoint(m_anchorOffset);
        canvasTransform.rotation = m_trackingAnchor.rotation;
    }
}
