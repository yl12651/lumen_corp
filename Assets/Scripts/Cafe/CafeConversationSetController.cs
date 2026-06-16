using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CafeConversationSetController : MonoBehaviour
{
    [SerializeField] private CafeConversationCutsceneController cutsceneController;

    [Header("Ready State")]
    [Tooltip("Assign CafePanels, Cafe Bag, the simulation start button, and any other UI roots that should disappear once conversations are ready.")]
    [SerializeField] private List<GameObject> uiRootsToHideWhenReady = new List<GameObject>();
    [Tooltip("Assign the back/reload button and any other UI roots that should appear once conversations are ready.")]
    [SerializeField] private List<GameObject> uiRootsToShowWhenReady = new List<GameObject>();
    [SerializeField] private List<HoverRaycaster> cafeHoverRaycastersToDisable = new List<HoverRaycaster>();
    [SerializeField] private List<CafeInteractable> cafeInteractablesToClear = new List<CafeInteractable>();

    [Header("Pair Attention")]
    [SerializeField] private List<CafeConversationPairAttentionAnimator> pairAttentionAnimators =
        new List<CafeConversationPairAttentionAnimator>();

    [Header("Read Tutorial Signals")]
    [SerializeField] private ConversationCutsceneController tutorialCutsceneController;
    [SerializeField] private List<CafePairReadTutorialSignal> pairReadSignals =
        new List<CafePairReadTutorialSignal>();
    [SerializeField] private bool retryPendingReadSignalsUntilAcknowledged = true;
    [SerializeField] private UnityEvent allPreparedPairReadSignalsAcknowledged = new UnityEvent();

    private readonly Dictionary<string, CafeConversationResponse> conversationsByPairKey =
        new Dictionary<string, CafeConversationResponse>();

    private readonly HashSet<string> readPairKeys = new HashSet<string>();
    private string activePairKey;
    private bool hasLoggedAllPreparedReadSignalsAcknowledged;
    private bool pairInteractionLocked;

    public bool HasPreparedConversations => conversationsByPairKey.Count > 0;
    public bool AreAllPreparedPairReadSignalsAcknowledged => AreAllPreparedReadSignalsAcknowledged();
    public bool IsPairInteractionLocked => pairInteractionLocked;

    private void Awake()
    {
        if (cutsceneController == null)
            cutsceneController = FindFirstObjectByType<CafeConversationCutsceneController>();

        if (tutorialCutsceneController == null)
            tutorialCutsceneController = FindFirstObjectByType<ConversationCutsceneController>();

        if (cutsceneController != null)
        {
            cutsceneController.SetFinishAction(CafeConversationFinishAction.HideAndReturnToCafe);
            cutsceneController.ConversationFinished += OnConversationFinished;
        }

        SetReadyOnlyUiVisible(false);
    }

    private void OnDestroy()
    {
        if (cutsceneController != null)
            cutsceneController.ConversationFinished -= OnConversationFinished;
    }

    private void Update()
    {
        if (retryPendingReadSignalsUntilAcknowledged)
            TrySendPendingReadSignals();
    }

    public void PrepareFromBackendJson(string backendJson)
    {
        conversationsByPairKey.Clear();
        readPairKeys.Clear();
        activePairKey = "";
        hasLoggedAllPreparedReadSignalsAcknowledged = false;
        ResetPairReadSignals();
        StopAllAttentionLoops();

        string conversationJson = ExtractConversationJson(backendJson);
        conversationJson = CleanJsonText(conversationJson);

        CafeConversationSetResponse conversationSet =
            JsonUtility.FromJson<CafeConversationSetResponse>(conversationJson);

        if (conversationSet == null)
        {
            Debug.LogWarning("[CafeConversationSetController] No conversation set was generated.", this);
            return;
        }

        if (!string.IsNullOrEmpty(conversationSet.error))
        {
            Debug.LogWarning("[CafeConversationSetController] Conversation set error: " + conversationSet.error, this);
            return;
        }

        if (conversationSet.conversations == null || conversationSet.conversations.Count == 0)
        {
            Debug.LogWarning("[CafeConversationSetController] Conversation set contains no conversations.", this);
            return;
        }

        for (int i = 0; i < conversationSet.conversations.Count; i++)
        {
            CafeConversationResponse conversation = conversationSet.conversations[i];

            if (conversation == null)
                continue;

            string pairKey = GetConversationPairKey(conversation, i);
            conversationsByPairKey[pairKey] = conversation;
        }

        Debug.Log("[CafeConversationSetController] Prepared conversations: " + conversationsByPairKey.Count);
        ApplyConversationSelectionReadyState();
    }

    public void PlayPair(string pairKey)
    {
        if (pairInteractionLocked)
            return;

        if (string.IsNullOrEmpty(pairKey))
        {
            Debug.LogWarning("[CafeConversationSetController] Cannot play a conversation without a pair key.", this);
            return;
        }

        if (cutsceneController == null)
        {
            Debug.LogError("[CafeConversationSetController] Cutscene controller is not assigned.", this);
            return;
        }

        if (!conversationsByPairKey.TryGetValue(pairKey, out CafeConversationResponse conversation))
        {
            Debug.LogWarning("[CafeConversationSetController] No prepared conversation for pair key: " + pairKey, this);
            return;
        }

        activePairKey = pairKey;
        cutsceneController.PlayConversation(conversation);
    }

    public bool HasConversationForPair(string pairKey)
    {
        return !string.IsNullOrEmpty(pairKey) && conversationsByPairKey.ContainsKey(pairKey);
    }

    public void SetPairInteractionLocked(bool locked)
    {
        pairInteractionLocked = locked;
    }

    private void ApplyConversationSelectionReadyState()
    {
        foreach (GameObject uiRoot in uiRootsToHideWhenReady)
        {
            if (uiRoot != null)
                uiRoot.SetActive(false);
        }

        SetReadyOnlyUiVisible(true);

        foreach (HoverRaycaster hoverRaycaster in cafeHoverRaycastersToDisable)
        {
            if (hoverRaycaster != null)
            {
                hoverRaycaster.SetHoverEnabled(false);
                hoverRaycaster.enabled = false;
            }
        }

        foreach (CafeInteractable interactable in cafeInteractablesToClear)
        {
            if (interactable != null)
                interactable.SetHighlighted(false);
        }

        StartUnreadAttentionLoops();
    }

    private void OnConversationFinished()
    {
        if (string.IsNullOrEmpty(activePairKey))
            return;

        readPairKeys.Add(activePairKey);
        StopAttentionLoop(activePairKey);
        MarkPairReadSignalPending(activePairKey);
        TrySendPendingReadSignals();
        activePairKey = "";
    }

    private void StartUnreadAttentionLoops()
    {
        foreach (CafeConversationPairAttentionAnimator animator in pairAttentionAnimators)
        {
            if (animator == null)
                continue;

            if (!HasConversationForPair(animator.PairKey))
                continue;

            if (readPairKeys.Contains(animator.PairKey))
                continue;

            animator.StartAttentionLoop();
        }
    }

    private void StopAttentionLoop(string pairKey)
    {
        foreach (CafeConversationPairAttentionAnimator animator in pairAttentionAnimators)
        {
            if (animator != null && animator.MatchesPairKey(pairKey))
                animator.StopAttentionLoop(true);
        }
    }

    private void StopAllAttentionLoops()
    {
        foreach (CafeConversationPairAttentionAnimator animator in pairAttentionAnimators)
        {
            if (animator != null)
                animator.StopAttentionLoop(true);
        }
    }

    private void ResetPairReadSignals()
    {
        foreach (CafePairReadTutorialSignal signal in pairReadSignals)
        {
            if (signal != null)
                signal.ResetRuntimeState();
        }
    }

    private void MarkPairReadSignalPending(string pairKey)
    {
        CafePairReadTutorialSignal signal = FindReadSignal(pairKey);

        if (signal == null)
        {
            Debug.LogWarning("[CafeConversationSetController] No read tutorial signal is configured for pair key: " + pairKey, this);
            return;
        }

        signal.MarkPending();
    }

    private void TrySendPendingReadSignals()
    {
        if (tutorialCutsceneController == null)
            return;

        bool acknowledgedAnySignal = false;

        foreach (CafePairReadTutorialSignal signal in pairReadSignals)
        {
            if (signal != null && signal.TrySend(tutorialCutsceneController))
                acknowledgedAnySignal = true;
        }

        if (acknowledgedAnySignal)
            CheckAllPreparedReadSignalsAcknowledged();
    }

    private void CheckAllPreparedReadSignalsAcknowledged()
    {
        if (hasLoggedAllPreparedReadSignalsAcknowledged)
            return;

        if (!AreAllPreparedReadSignalsAcknowledged())
            return;

        hasLoggedAllPreparedReadSignalsAcknowledged = true;
        Debug.Log("[CafeConversationSetController] All prepared pair read tutorial signals acknowledged.", this);
        allPreparedPairReadSignalsAcknowledged.Invoke();
    }

    private bool AreAllPreparedReadSignalsAcknowledged()
    {
        if (conversationsByPairKey.Count == 0)
            return false;

        foreach (string pairKey in conversationsByPairKey.Keys)
        {
            CafePairReadTutorialSignal signal = FindReadSignal(pairKey);

            if (signal == null || !signal.HasSentSignal)
                return false;
        }

        return true;
    }

    private CafePairReadTutorialSignal FindReadSignal(string pairKey)
    {
        if (string.IsNullOrEmpty(pairKey))
            return null;

        foreach (CafePairReadTutorialSignal signal in pairReadSignals)
        {
            if (signal != null && signal.MatchesPairKey(pairKey))
                return signal;
        }

        return null;
    }

    private void SetReadyOnlyUiVisible(bool visible)
    {
        foreach (GameObject uiRoot in uiRootsToShowWhenReady)
        {
            if (uiRoot != null)
                uiRoot.SetActive(visible);
        }
    }

    private string GetConversationPairKey(CafeConversationResponse conversation, int index)
    {
        if (!string.IsNullOrEmpty(conversation.pairKey))
            return conversation.pairKey;

        if (!string.IsNullOrEmpty(conversation.position))
            return conversation.position;

        return "pair-" + (index + 1);
    }

    private string ExtractConversationJson(string backendJson)
    {
        CafeBackendResponse backendResponse =
            JsonUtility.FromJson<CafeBackendResponse>(backendJson);

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

    [Serializable]
    private class CafeBackendResponse
    {
        public string text;
    }
}

[Serializable]
public class CafePairReadTutorialSignal
{
    [SerializeField] private string pairKey;
    [SerializeField] private string signalId;

    private bool isPending;
    private bool hasSentSignal;

    public bool HasSentSignal => hasSentSignal;

    public bool MatchesPairKey(string candidatePairKey)
    {
        return !string.IsNullOrEmpty(pairKey) && pairKey == candidatePairKey;
    }

    public void ResetRuntimeState()
    {
        isPending = false;
        hasSentSignal = false;
    }

    public void MarkPending()
    {
        if (!hasSentSignal)
            isPending = true;
    }

    public bool TrySend(ConversationCutsceneController cutsceneController)
    {
        if (!isPending || hasSentSignal)
            return false;

        bool acknowledged = TutorialSignalUtility.SendTutorialSignalOnce(
            cutsceneController,
            signalId,
            ref hasSentSignal
        );

        if (acknowledged)
            isPending = false;

        return acknowledged;
    }
}
