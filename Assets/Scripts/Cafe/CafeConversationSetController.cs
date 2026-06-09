using System;
using System.Collections.Generic;
using UnityEngine;

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

    private readonly Dictionary<string, CafeConversationResponse> conversationsByPairKey =
        new Dictionary<string, CafeConversationResponse>();

    public bool HasPreparedConversations => conversationsByPairKey.Count > 0;

    private void Awake()
    {
        if (cutsceneController == null)
            cutsceneController = FindFirstObjectByType<CafeConversationCutsceneController>();

        if (cutsceneController != null)
            cutsceneController.SetFinishAction(CafeConversationFinishAction.HideAndReturnToCafe);

        SetReadyOnlyUiVisible(false);
    }

    public void PrepareFromBackendJson(string backendJson)
    {
        conversationsByPairKey.Clear();

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

        cutsceneController.PlayConversation(conversation);
    }

    public bool HasConversationForPair(string pairKey)
    {
        return !string.IsNullOrEmpty(pairKey) && conversationsByPairKey.ContainsKey(pairKey);
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
