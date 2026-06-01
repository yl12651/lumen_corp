using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConversationCutsceneController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private ConversationCutsceneAsset cutscene;
    [SerializeField] private bool playOnStart = true;

    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Button advanceButton;
    [SerializeField] private GameObject continueIndicator;
    [SerializeField] private Canvas canvasToHideWhenWaitingForSignal;
    [SerializeField] private ConversationTutorialMiddleware tutorialMiddleware;
    [SerializeField] private SceneAsyncLoader sceneAsyncLoader;

    private int lineIndex;
    private bool waitingForSignal;

    private void Awake()
    {
        if (advanceButton != null)
            advanceButton.onClick.AddListener(TryAdvanceFromClick);
    }

    private void Start()
    {
        if (playOnStart)
            Play(cutscene);
    }

    private void OnDestroy()
    {
        if (advanceButton != null)
            advanceButton.onClick.RemoveListener(TryAdvanceFromClick);
    }

    public void Play(ConversationCutsceneAsset newCutscene)
    {
        cutscene = newCutscene;
        lineIndex = 0;
        waitingForSignal = false;
        SetWaitingCanvasVisible(true);

        if (cutscene == null || cutscene.lines == null || cutscene.lines.Count == 0)
        {
            LoadNextScene();
            return;
        }

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (lineIndex >= cutscene.lines.Count)
        {
            LoadNextScene();
            return;
        }

        ConversationLine line = cutscene.lines[lineIndex];

        if (titleText != null)
            titleText.text = cutscene.title;

        if (speakerNameText != null)
            speakerNameText.text = line.speakerName;

        if (bodyText != null)
            bodyText.text = line.text;

        if (portraitImage != null)
        {
            Sprite portrait = line.portrait != null ? line.portrait : cutscene.defaultPortrait;
            portraitImage.sprite = portrait;
            portraitImage.enabled = portrait != null;
            portraitImage.preserveAspect = true;
        }

        waitingForSignal = line.advanceMode == ConversationAdvanceMode.WaitForSignal;
        SetWaitingCanvasVisible(!waitingForSignal || !line.hideCanvasWhileWaitingForSignal);

        if (continueIndicator != null)
            continueIndicator.SetActive(!waitingForSignal);

        if (tutorialMiddleware != null)
            tutorialMiddleware.HandleLineStarted(line);
    }

    private void TryAdvanceFromClick()
    {
        if (waitingForSignal)
            return;

        AdvanceLine();
    }

    private void AdvanceLine()
    {
        if (cutscene != null && cutscene.lines != null && lineIndex < cutscene.lines.Count && tutorialMiddleware != null)
            tutorialMiddleware.HandleLineEnded(cutscene.lines[lineIndex]);

        lineIndex++;
        ShowCurrentLine();
    }

    public bool CompleteSignal(string signalId)
    {
        if (!waitingForSignal)
            return false;

        ConversationLine line = cutscene.lines[lineIndex];

        if (line.requiredSignalId != signalId)
            return false;

        AdvanceLine();
        return true;
    }

    private void SetWaitingCanvasVisible(bool isVisible)
    {
        if (canvasToHideWhenWaitingForSignal != null)
            canvasToHideWhenWaitingForSignal.enabled = isVisible;
    }

    private void LoadNextScene()
    {
        if (cutscene != null && !string.IsNullOrWhiteSpace(cutscene.nextSceneName))
            LoadSceneAsync(cutscene.nextSceneName);
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
}
