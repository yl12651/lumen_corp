using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum CafeConversationFinishAction
{
    LoadScene,
    HideAndReturnToCafe
}

public class CafeConversationCutsceneController : MonoBehaviour
{
    public event Action ConversationFinished;

    [Header("Root")]
    [SerializeField] private GameObject cutsceneRoot;
    [SerializeField] private GameObject cafeRoot;

    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text speakerLabelText;
    [SerializeField] private TMP_Text conversationText;
    [SerializeField] private Button bubbleButton;
    [SerializeField] private RectTransform bubbleRoot;

    [Header("Characters")]
    [SerializeField] private Image leftCharacterImage;
    [SerializeField] private Image rightCharacterImage;

    [Header("Sprite Mapping")]
    [SerializeField] private List<CharacterSpriteEntry> characterSprites = new List<CharacterSpriteEntry>();

    [Header("End")]
    [SerializeField] private string endBubbleText = "The End";
    [SerializeField] private CafeConversationFinishAction finishAction = CafeConversationFinishAction.LoadScene;
    [SerializeField] private string characterCreationSceneName = "CharacterCreationScene";
    [SerializeField] private SceneAsyncLoader sceneAsyncLoader;

    [Header("Visual Tuning")]
    [SerializeField] private float currentSpeakerAlpha = 1f;
    [SerializeField] private float inactiveSpeakerAlpha = 0.45f;

    private Dictionary<string, Sprite> spriteLookup = new Dictionary<string, Sprite>();
    private Dictionary<string, string> speakerLabelLookup = new Dictionary<string, string>();

    private CafeConversationResponse currentConversation;
    private int bubbleIndex;
    private bool showingEndBubble;
    private bool showingContextBubble;

    private string leftSpeakerKey;
    private string rightSpeakerKey;

    private void Awake()
    {
        BuildSpriteLookup();

        if (cutsceneRoot != null)
            cutsceneRoot.SetActive(false);

        if (bubbleRoot == null && bubbleButton != null)
            bubbleRoot = bubbleButton.transform as RectTransform;

        if (bubbleButton != null)
            bubbleButton.onClick.AddListener(OnBubbleClicked);

        if (leftCharacterImage != null)
            leftCharacterImage.raycastTarget = false;

        if (rightCharacterImage != null)
            rightCharacterImage.raycastTarget = false;
    }

    private void OnDestroy()
    {
        if (bubbleButton != null)
            bubbleButton.onClick.RemoveListener(OnBubbleClicked);
    }

    private void BuildSpriteLookup()
    {
        spriteLookup.Clear();

        foreach (CharacterSpriteEntry entry in characterSprites)
        {
            if (!string.IsNullOrEmpty(entry.id) && entry.sprite != null)
                spriteLookup[entry.id] = entry.sprite;
        }
    }

    public void PlayFromBackendJson(string backendJson)
    {
        string conversationJson = ExtractConversationJson(backendJson);
        conversationJson = CleanJsonText(conversationJson);

        CafeConversationSetResponse conversationSet =
            JsonUtility.FromJson<CafeConversationSetResponse>(conversationJson);

        if (conversationSet != null && !string.IsNullOrEmpty(conversationSet.error))
        {
            PlayConversation(new CafeConversationResponse { error = conversationSet.error });
            return;
        }

        if (conversationSet != null && conversationSet.conversations != null && conversationSet.conversations.Count > 0)
        {
            PlayConversation(conversationSet.conversations[0]);
            return;
        }

        CafeConversationResponse conversation =
            JsonUtility.FromJson<CafeConversationResponse>(conversationJson);

        PlayConversation(conversation);
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

    public void PlayConversation(CafeConversationResponse conversation)
    {
        currentConversation = conversation;
        bubbleIndex = 0;
        showingEndBubble = false;
        showingContextBubble = false;

        if (cafeRoot != null)
        {
            cafeRoot.SetActive(false);
        }
        
        if (cutsceneRoot != null)
        {
            cutsceneRoot.SetActive(true);
            cutsceneRoot.transform.SetAsLastSibling();
        }

        if (currentConversation == null)
        {
            ShowFallbackEnd("No conversation was generated.");
            return;
        }

        if (!string.IsNullOrEmpty(currentConversation.error))
        {
            ShowFallbackEnd(currentConversation.error);
            return;
        }

        if (currentConversation.bubbles == null || currentConversation.bubbles.Count == 0)
        {
            ShowFallbackEnd("No dialogue was generated.");
            return;
        }

        SetupSpeakers();
        ShowContextBubble();
    }

    private void SetupSpeakers()
    {
        speakerLabelLookup.Clear();

        CafeDialogueBubble firstBubble = currentConversation.bubbles[0];
        string firstSpeakerKey = GetBubbleSpeakerKey(firstBubble);

        leftSpeakerKey = firstSpeakerKey;
        rightSpeakerKey = "";

        string leftSpriteId = firstBubble.speakerId;
        string rightSpriteId = "";

        if (currentConversation.selectedPair != null)
        {
            bool hasDuplicateIds = HasDuplicateSelectedSubjectIds();

            foreach (SelectedSubjectInfo subject in currentConversation.selectedPair)
            {
                if (subject == null)
                    continue;

                string subjectSpeakerKey = GetSelectedSubjectSpeakerKey(subject);
                speakerLabelLookup[subjectSpeakerKey] = BuildSpeakerLabel(subject, hasDuplicateIds);

                if (subjectSpeakerKey == leftSpeakerKey)
                {
                    leftSpriteId = subject.id;
                    continue;
                }

                if (string.IsNullOrEmpty(rightSpeakerKey))
                {
                    rightSpeakerKey = subjectSpeakerKey;
                    rightSpriteId = subject.id;
                }
            }
        }

        SetCharacterImage(leftCharacterImage, leftSpriteId);
        SetCharacterImage(rightCharacterImage, rightSpriteId);
    }

    private void SetCharacterImage(Image image, string speakerId)
    {
        if (image == null)
            return;

        if (!string.IsNullOrEmpty(speakerId) && spriteLookup.TryGetValue(speakerId, out Sprite sprite))
        {
            image.sprite = sprite;
            image.enabled = true;
            image.preserveAspect = true;
            SetImageAlpha(image, inactiveSpeakerAlpha);
        }
        else
        {
            image.sprite = null;
            image.enabled = false;
        }
    }

    private void RenderCurrentBubble()
    {
        if (currentConversation == null || currentConversation.bubbles == null)
            return;

        if (bubbleIndex >= currentConversation.bubbles.Count)
        {
            ShowEndBubble();
            return;
        }

        CafeDialogueBubble bubble = currentConversation.bubbles[bubbleIndex];

        if (titleText != null)
            titleText.text = currentConversation.sceneTitle;

        if (speakerLabelText != null)
            speakerLabelText.text = GetSpeakerLabel(bubble) + ": the " + bubble.position;

        if (conversationText != null)
            conversationText.text = bubble.text;

        ApplySpeakerRenderOrder(GetBubbleSpeakerKey(bubble));
    }
    
    private void ShowContextBubble()
    {
        showingContextBubble = true;

        if (titleText != null)
            titleText.text = currentConversation.sceneTitle;

        if (speakerLabelText != null)
            speakerLabelText.text = "";

        if (conversationText != null)
            conversationText.text = currentConversation.context;

        ApplyContextRenderOrder();
    }
    
    private void ApplyContextRenderOrder()
    {
        if (leftCharacterImage != null)
        {
            leftCharacterImage.transform.SetSiblingIndex(1);
            SetImageAlpha(leftCharacterImage, inactiveSpeakerAlpha);
        }

        if (rightCharacterImage != null)
        {
            rightCharacterImage.transform.SetSiblingIndex(2);
            SetImageAlpha(rightCharacterImage, inactiveSpeakerAlpha);
        }

        // Context bubble should be above both speakers.
        if (bubbleRoot != null)
            bubbleRoot.SetSiblingIndex(3);

        if (titleText != null)
            titleText.transform.SetAsLastSibling();
    }

    private void ApplySpeakerRenderOrder(string currentSpeakerId)
    {
        bool leftIsCurrent = currentSpeakerId == leftSpeakerKey;

        Image currentImage = leftIsCurrent ? leftCharacterImage : rightCharacterImage;
        Image otherImage = leftIsCurrent ? rightCharacterImage : leftCharacterImage;

        if (otherImage != null)
        {
            otherImage.transform.SetSiblingIndex(1);
            SetImageAlpha(otherImage, inactiveSpeakerAlpha);
        }

        if (bubbleRoot != null)
            bubbleRoot.SetSiblingIndex(2);

        if (currentImage != null)
        {
            currentImage.transform.SetSiblingIndex(3);
            SetImageAlpha(currentImage, currentSpeakerAlpha);
        }

        if (titleText != null)
            titleText.transform.SetAsLastSibling();
    }

    private void ShowEndBubble()
    {
        showingEndBubble = true;

        if (speakerLabelText != null)
            speakerLabelText.text = "";

        if (conversationText != null)
            conversationText.text = endBubbleText;

        if (leftCharacterImage != null)
        {
            leftCharacterImage.transform.SetSiblingIndex(1);
            SetImageAlpha(leftCharacterImage, inactiveSpeakerAlpha);
        }

        if (rightCharacterImage != null)
        {
            rightCharacterImage.transform.SetSiblingIndex(2);
            SetImageAlpha(rightCharacterImage, inactiveSpeakerAlpha);
        }

        if (bubbleRoot != null)
            bubbleRoot.SetSiblingIndex(3);

        if (titleText != null)
            titleText.transform.SetAsLastSibling();
    }

    private void ShowFallbackEnd(string message)
    {
        showingEndBubble = true;

        if (titleText != null)
            titleText.text = "";

        if (speakerLabelText != null)
            speakerLabelText.text = "";

        if (conversationText != null)
            conversationText.text = message;
    }

    private void OnBubbleClicked()
    {
        if (showingEndBubble)
        {
            FinishConversation();
            return;
        }

        if (showingContextBubble)
        {
            showingContextBubble = false;
            bubbleIndex = 0;
            RenderCurrentBubble();
            return;
        }

        bubbleIndex++;
        RenderCurrentBubble();
    }

    private string GetBubbleSpeakerKey(CafeDialogueBubble bubble)
    {
        if (bubble == null)
            return "";

        if (!string.IsNullOrEmpty(bubble.speakerKey))
            return bubble.speakerKey;

        return bubble.speakerId;
    }

    private string GetSelectedSubjectSpeakerKey(SelectedSubjectInfo subject)
    {
        if (subject == null)
            return "";

        if (!string.IsNullOrEmpty(subject.speakerKey))
            return subject.speakerKey;

        return subject.id;
    }

    private bool HasDuplicateSelectedSubjectIds()
    {
        if (currentConversation == null || currentConversation.selectedPair == null)
            return false;

        HashSet<string> ids = new HashSet<string>();

        foreach (SelectedSubjectInfo subject in currentConversation.selectedPair)
        {
            if (subject == null || string.IsNullOrEmpty(subject.id))
                continue;

            if (!ids.Add(subject.id))
                return true;
        }

        return false;
    }

    private string BuildSpeakerLabel(SelectedSubjectInfo subject, bool includeSpeakerSlot)
    {
        string label = !string.IsNullOrEmpty(subject.id) ? subject.id : GetSelectedSubjectSpeakerKey(subject);

        if (!includeSpeakerSlot)
            return label;

        string speakerKey = GetSelectedSubjectSpeakerKey(subject);
        int separatorIndex = speakerKey.LastIndexOf(':');

        if (separatorIndex < 0 || separatorIndex >= speakerKey.Length - 1)
            return label;

        return label + " " + speakerKey.Substring(separatorIndex + 1).ToUpperInvariant();
    }

    private string GetSpeakerLabel(CafeDialogueBubble bubble)
    {
        string speakerKey = GetBubbleSpeakerKey(bubble);

        if (!string.IsNullOrEmpty(speakerKey) && speakerLabelLookup.TryGetValue(speakerKey, out string label))
            return label;

        return bubble != null ? bubble.speakerId : "";
    }

    public void SetFinishAction(CafeConversationFinishAction action)
    {
        finishAction = action;
    }

    private void FinishConversation()
    {
        if (finishAction == CafeConversationFinishAction.HideAndReturnToCafe)
        {
            HideCutsceneAndReturnToCafe();
            ConversationFinished?.Invoke();
            return;
        }

        LoadSceneAsync(characterCreationSceneName);
    }

    public void HideCutsceneAndReturnToCafe()
    {
        currentConversation = null;
        bubbleIndex = 0;
        showingEndBubble = false;
        showingContextBubble = false;

        if (cutsceneRoot != null)
            cutsceneRoot.SetActive(false);

        if (cafeRoot != null)
            cafeRoot.SetActive(true);
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
            return;

        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    private void LoadSceneAsync(string sceneName)
    {
        if (sceneAsyncLoader == null)
            sceneAsyncLoader = FindFirstObjectByType<SceneAsyncLoader>();

        if (sceneAsyncLoader == null)
        {
            Debug.LogError("SceneAsyncLoader is required in the scene before loading " + sceneName + ".", this);
            return;
        }

        sceneAsyncLoader.LoadScene(sceneName);
    }

    [Serializable]
    private class CafeBackendResponse
    {
        public string text;
    }
}

[Serializable]
public class CafeConversationSetResponse
{
    public List<CafeConversationResponse> conversations;
    public string error;
}

[Serializable]
public class CafeConversationResponse
{
    public string pairKey;
    public string position;
    public List<SelectedSubjectInfo> selectedPair;
    public string sceneTitle;
    public string context;
    public List<CafeDialogueBubble> bubbles;
    public string error;
}

[Serializable]
public class SelectedSubjectInfo
{
    public string speakerKey;
    public string position;
    public string id;
    public string type;
}

[Serializable]
public class CafeDialogueBubble
{
    public string speakerKey;
    public string speakerId;
    public string position;
    public string text;
}
