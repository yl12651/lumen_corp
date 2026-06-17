using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

public class CafeRetentionSelectionController : MonoBehaviour
{
    [SerializeField] private GameObject canvasRoot;
    [SerializeField] private CafeUIDropInAnimator dropInAnimator;
    [SerializeField] private CafeSimulationSubmit simulationSubmitter;
    [SerializeField] private CafeConversationSetController conversationSetController;
    [SerializeField] private ConversationCutsceneController cutsceneController;
    [SerializeField] private string submitSignalId = "retention_selection_submitted";

    [Header("Backend")]
    [SerializeField] private string retentionReviewBackendUrl = "http://localhost:3000/api/retention-review";
    [SerializeField] private GameObject loadingCanvas;

    [Header("Generated Review Cutscene")]
    [SerializeField] private Sprite generatedReviewDefaultPortrait;

    [Tooltip("Preferred path for duplicated SchedulePanel entries. Each selectable wraps an AssignmentDropPanel.")]
    [SerializeField] private List<CafeRetentionAssignmentPanelSelectable> assignmentPanelSelectables =
        new List<CafeRetentionAssignmentPanelSelectable>();
    [Tooltip("Legacy/manual entry path. Used only when Assignment Panel Selectables is empty.")]
    [SerializeField] private List<CafeRetentionSelectionEntry> entries = new List<CafeRetentionSelectionEntry>();
    [SerializeField] private Button submitButton;
    [SerializeField] private TMP_Text warningText;
    [SerializeField] private bool requireAtLeastOneSelection = true;
    [SerializeField] private bool hideCanvasAfterSignalAcknowledged = true;
    [SerializeField] private UnityEvent selectionSubmitted = new UnityEvent();

    private readonly List<CafeAssignedSubjectSelection> assignedSubjects = new List<CafeAssignedSubjectSelection>();
    private readonly HashSet<string> selectedSpeakerKeys = new HashSet<string>();
    private bool hasSentSubmitSignal;
    private bool isSubmittingReview;

    public IReadOnlyList<CafeAssignedSubjectSelection> AssignedSubjects => assignedSubjects;

    private void Awake()
    {
        if (simulationSubmitter == null)
            simulationSubmitter = FindFirstObjectByType<CafeSimulationSubmit>();

        if (conversationSetController == null)
            conversationSetController = FindFirstObjectByType<CafeConversationSetController>();

        if (cutsceneController == null)
            cutsceneController = FindFirstObjectByType<ConversationCutsceneController>();

        if (dropInAnimator == null && canvasRoot != null)
            dropInAnimator = canvasRoot.GetComponentInChildren<CafeUIDropInAnimator>(true);

        if (submitButton != null)
            submitButton.onClick.AddListener(SubmitSelection);

        HideWarning();
        SetLoadingVisible(false);
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (submitButton != null)
            submitButton.onClick.RemoveListener(SubmitSelection);
    }

    public void Open()
    {
        hasSentSubmitSignal = false;
        selectedSpeakerKeys.Clear();
        RefreshFromAssignments();
        SetVisible(true);

        if (dropInAnimator != null)
            dropInAnimator.Play();

        if (conversationSetController != null)
            conversationSetController.SetPairInteractionLocked(true);

        Debug.Log(
            "[CafeRetentionSelectionController] Opened selection canvas. " +
            $"assignedSubjects={assignedSubjects.Count}, selectedSubjects={selectedSpeakerKeys.Count}",
            this
        );
    }

    public IEnumerator OpenRoutine()
    {
        Open();
        yield break;
    }

    public void Close()
    {
        SetVisible(false);

        if (conversationSetController != null)
            conversationSetController.SetPairInteractionLocked(false);

        Debug.Log("[CafeRetentionSelectionController] Closed selection canvas and unlocked pair interaction.", this);
    }

    public List<CafeRetentionSelectionPayload> BuildSelectionPayload()
    {
        List<CafeRetentionSelectionPayload> payload = new List<CafeRetentionSelectionPayload>();

        foreach (CafeAssignedSubjectSelection subject in assignedSubjects)
        {
            if (subject == null || subject.subject == null)
                continue;

            payload.Add(new CafeRetentionSelectionPayload
            {
                pairKey = subject.pairKey,
                positionName = subject.positionName,
                slot = subject.slot,
                speakerKey = subject.speakerKey,
                subject = subject.subject,
                shouldRemain = selectedSpeakerKeys.Contains(subject.speakerKey)
            });
        }

        return payload;
    }

    public void SubmitSelection()
    {
        Debug.Log(
            "[CafeRetentionSelectionController] Submit clicked. " +
            $"assignedSubjects={assignedSubjects.Count}, selectedSubjects={selectedSpeakerKeys.Count}, signalId={submitSignalId}",
            this
        );

        if (requireAtLeastOneSelection && selectedSpeakerKeys.Count == 0)
        {
            ShowWarning("Select at least one subject before submitting.");
            Debug.LogWarning("[CafeRetentionSelectionController] Submit blocked: no subjects selected.", this);
            return;
        }

        List<CafeRetentionSelectionPayload> payload = BuildSelectionPayload();
        Debug.Log(
            "[CafeRetentionSelectionController] Built retention payload. " +
            $"payloadItems={payload.Count}, keptItems={CountKeptPayloadItems(payload)}",
            this
        );

        if (isSubmittingReview)
        {
            Debug.Log("[CafeRetentionSelectionController] Submit ignored: retention review request is already running.", this);
            return;
        }

        HideWarning();
        StartCoroutine(SendRetentionReviewAndPlay(payload));
    }

    public void LogCurrentSelectionPayload()
    {
        List<CafeRetentionSelectionPayload> payload = BuildSelectionPayload();
        Debug.Log(
            "[CafeRetentionSelectionController] Current selection payload:\n" +
            JsonUtility.ToJson(new CafeRetentionSelectionPayloadDebugWrapper { selections = payload }, true),
            this
        );
    }

    public void ToggleAssignmentPanelSelection(CafeRetentionAssignmentPanelSelectable selectable)
    {
        if (selectable == null || selectable.AssignedSubject == null || string.IsNullOrEmpty(selectable.AssignedSubject.speakerKey))
            return;

        string speakerKey = selectable.AssignedSubject.speakerKey;
        bool selected = !selectedSpeakerKeys.Contains(speakerKey);

        if (selected)
            selectedSpeakerKeys.Add(speakerKey);
        else
            selectedSpeakerKeys.Remove(speakerKey);

        selectable.SetSelected(selected);
        HideWarning();
        Debug.Log(
            "[CafeRetentionSelectionController] Assignment panel selection changed. " +
            $"speakerKey={speakerKey}, selected={selected}, selectedSubjects={selectedSpeakerKeys.Count}",
            this
        );
    }

    private void RefreshFromAssignments()
    {
        assignedSubjects.Clear();

        if (simulationSubmitter != null)
            assignedSubjects.AddRange(simulationSubmitter.BuildAssignedSubjectSelections());

        if (assignmentPanelSelectables.Count > 0)
        {
            RefreshAssignmentPanelSelectables();
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            CafeRetentionSelectionEntry entry = entries[i];

            if (entry == null)
                continue;

            bool hasSubject = i < assignedSubjects.Count;
            entry.gameObject.SetActive(hasSubject);

            if (hasSubject)
                entry.Setup(assignedSubjects[i], OnEntryClicked);
        }
    }

    private void OnEntryClicked(CafeRetentionSelectionEntry entry)
    {
        if (entry == null || entry.AssignedSubject == null || string.IsNullOrEmpty(entry.AssignedSubject.speakerKey))
            return;

        string speakerKey = entry.AssignedSubject.speakerKey;
        bool selected = !selectedSpeakerKeys.Contains(speakerKey);

        if (selected)
            selectedSpeakerKeys.Add(speakerKey);
        else
            selectedSpeakerKeys.Remove(speakerKey);

        entry.SetSelected(selected);
        HideWarning();
        Debug.Log(
            "[CafeRetentionSelectionController] Entry selection changed. " +
            $"speakerKey={speakerKey}, selected={selected}, selectedSubjects={selectedSpeakerKeys.Count}",
            this
        );
    }

    private void RefreshAssignmentPanelSelectables()
    {
        Dictionary<string, CafeAssignedSubjectSelection> subjectsBySlotKey =
            new Dictionary<string, CafeAssignedSubjectSelection>();

        foreach (CafeAssignedSubjectSelection subject in assignedSubjects)
        {
            if (subject == null || string.IsNullOrEmpty(subject.assignmentSlotKey))
                continue;

            subjectsBySlotKey[subject.assignmentSlotKey] = subject;
        }

        foreach (CafeRetentionAssignmentPanelSelectable selectable in assignmentPanelSelectables)
        {
            if (selectable == null)
                continue;

            AssignmentDropPanel panel = selectable.AssignmentPanel;
            string slotKey = panel != null ? panel.SlotKey : "";
            CafeAssignedSubjectSelection subject = null;
            bool hasSubject = !string.IsNullOrEmpty(slotKey)
                && subjectsBySlotKey.TryGetValue(slotKey, out subject);

            selectable.gameObject.SetActive(hasSubject);

            if (hasSubject)
                selectable.Setup(subject, this);
            else
                selectable.Clear();
        }

        Debug.Log(
            "[CafeRetentionSelectionController] Refreshed duplicated assignment panels. " +
            $"panelCount={assignmentPanelSelectables.Count}, assignedSubjects={assignedSubjects.Count}",
            this
        );
    }

    private int CountKeptPayloadItems(List<CafeRetentionSelectionPayload> payload)
    {
        int count = 0;

        foreach (CafeRetentionSelectionPayload item in payload)
        {
            if (item != null && item.shouldRemain)
                count++;
        }

        return count;
    }

    private IEnumerator SendRetentionReviewAndPlay(List<CafeRetentionSelectionPayload> payload)
    {
        isSubmittingReview = true;
        SetLoadingVisible(true);
        SetSubmitInteractable(false);

        CafeRetentionReviewRequest requestData = new CafeRetentionReviewRequest
        {
            selections = payload
        };

        string jsonBody = JsonUtility.ToJson(requestData, true);
        Debug.Log("[CafeRetentionSelectionController] Sending retention review JSON:\n" + jsonBody, this);

        using UnityWebRequest request = new UnityWebRequest(retentionReviewBackendUrl, "POST");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        SetLoadingVisible(false);
        isSubmittingReview = false;
        SetSubmitInteractable(true);

        if (request.result != UnityWebRequest.Result.Success)
        {
            ShowWarning("Retention review request failed. Please try again.");
            Debug.LogError("[CafeRetentionSelectionController] Retention review request failed: " + request.error, this);
            Debug.LogError(request.downloadHandler != null ? request.downloadHandler.text : "", this);
            yield break;
        }

        Debug.Log("[CafeRetentionSelectionController] Retention review backend result:\n" + request.downloadHandler.text, this);

        ConversationCutsceneAsset generatedCutscene = BuildGeneratedReviewCutscene(request.downloadHandler.text);

        if (generatedCutscene == null)
        {
            ShowWarning("Retention review response could not be parsed.");
            Debug.LogError("[CafeRetentionSelectionController] Failed to build generated retention review cutscene.", this);
            yield break;
        }

        bool acknowledged = TutorialSignalUtility.SendTutorialSignalOnce(
            cutsceneController,
            submitSignalId,
            ref hasSentSubmitSignal
        );

        if (!acknowledged)
        {
            ShowWarning("Review is ready, but Clothos is not waiting for it yet.");
            Debug.LogWarning(
                "[CafeRetentionSelectionController] Submit signal was not acknowledged after backend response. " +
                "Check that the assigned ConversationCutsceneController is currently waiting for this signal.",
                this
            );
            yield break;
        }

        Debug.Log("[CafeRetentionSelectionController] Submit signal acknowledged. Playing generated retention review.", this);

        if (hideCanvasAfterSignalAcknowledged)
            Close();

        if (cutsceneController != null)
        {
            cutsceneController.SetFinishAction(ConversationCutsceneFinishAction.HideCanvases);
            cutsceneController.Play(generatedCutscene);
        }

        selectionSubmitted.Invoke();
        Debug.Log("[CafeRetentionSelectionController] Selection Submitted UnityEvent invoked after generated review playback started.", this);
    }

    private ConversationCutsceneAsset BuildGeneratedReviewCutscene(string backendJson)
    {
        string reviewJson = ExtractBackendText(backendJson);
        reviewJson = CleanJsonText(reviewJson);

        CafeRetentionReviewResponse reviewResponse =
            JsonUtility.FromJson<CafeRetentionReviewResponse>(reviewJson);

        if (reviewResponse == null)
            return null;

        if (!string.IsNullOrEmpty(reviewResponse.error))
        {
            Debug.LogWarning("[CafeRetentionSelectionController] Retention review response error: " + reviewResponse.error, this);
            return null;
        }

        if (reviewResponse.lines == null || reviewResponse.lines.Count == 0)
        {
            Debug.LogWarning("[CafeRetentionSelectionController] Retention review response contains no lines.", this);
            return null;
        }

        ConversationCutsceneAsset cutscene = ScriptableObject.CreateInstance<ConversationCutsceneAsset>();
        cutscene.title = !string.IsNullOrEmpty(reviewResponse.title)
            ? reviewResponse.title
            : "Retention Review";
        cutscene.nextSceneName = "";
        cutscene.defaultPortrait = generatedReviewDefaultPortrait;
        cutscene.lines = new List<ConversationLine>();

        foreach (CafeRetentionReviewLine reviewLine in reviewResponse.lines)
        {
            if (reviewLine == null || string.IsNullOrWhiteSpace(reviewLine.text))
                continue;

            cutscene.lines.Add(new ConversationLine
            {
                speakerName = !string.IsNullOrWhiteSpace(reviewLine.speakerName)
                    ? reviewLine.speakerName
                    : "Clothos",
                text = reviewLine.text,
                live2DTriggerName = !string.IsNullOrWhiteSpace(reviewLine.live2DTriggerName)
                    ? reviewLine.live2DTriggerName
                    : "Speak",
                advanceMode = ConversationAdvanceMode.Click,
                requiredSignalId = "",
                hideCanvasWhileWaitingForSignal = false,
                startActions = new List<ConversationLineAction>(),
                endActions = new List<ConversationLineAction>()
            });
        }

        if (cutscene.lines.Count == 0)
            return null;

        Debug.Log("[CafeRetentionSelectionController] Built generated retention review cutscene. lines=" + cutscene.lines.Count, this);
        return cutscene;
    }

    private string ExtractBackendText(string backendJson)
    {
        CafeBackendTextResponse backendResponse =
            JsonUtility.FromJson<CafeBackendTextResponse>(backendJson);

        if (backendResponse != null && !string.IsNullOrEmpty(backendResponse.text))
            return backendResponse.text;

        return backendJson;
    }

    private string CleanJsonText(string json)
    {
        if (string.IsNullOrEmpty(json))
            return "";

        json = json.Trim();

        if (json.StartsWith("```"))
        {
            int firstNewLine = json.IndexOf('\n');
            int lastFence = json.LastIndexOf("```", StringComparison.Ordinal);

            if (firstNewLine >= 0 && lastFence > firstNewLine)
                json = json.Substring(firstNewLine + 1, lastFence - firstNewLine - 1).Trim();
        }

        return json;
    }

    private void SetLoadingVisible(bool visible)
    {
        if (loadingCanvas != null)
            loadingCanvas.SetActive(visible);
    }

    private void SetSubmitInteractable(bool interactable)
    {
        if (submitButton != null)
            submitButton.interactable = interactable;
    }

    private void SetVisible(bool visible)
    {
        if (canvasRoot != null)
            canvasRoot.SetActive(visible);
        else
            gameObject.SetActive(visible);
    }

    private void ShowWarning(string message)
    {
        if (warningText == null)
            return;

        warningText.text = message;
        warningText.gameObject.SetActive(true);
    }

    private void HideWarning()
    {
        if (warningText == null)
            return;

        warningText.text = "";
        warningText.gameObject.SetActive(false);
    }
}

[Serializable]
public class CafeRetentionSelectionPayload
{
    public string pairKey;
    public string positionName;
    public string slot;
    public string speakerKey;
    public CharacterDefinition subject;
    public bool shouldRemain;
}

[Serializable]
public class CafeRetentionSelectionPayloadDebugWrapper
{
    public List<CafeRetentionSelectionPayload> selections;
}

[Serializable]
public class CafeRetentionReviewRequest
{
    public List<CafeRetentionSelectionPayload> selections;
}

[Serializable]
public class CafeBackendTextResponse
{
    public string text;
}

[Serializable]
public class CafeRetentionReviewResponse
{
    public string title;
    public List<CafeRetentionReviewLine> lines;
    public string error;
}

[Serializable]
public class CafeRetentionReviewLine
{
    public string speakerName;
    public string text;
    public string live2DTriggerName;
}
