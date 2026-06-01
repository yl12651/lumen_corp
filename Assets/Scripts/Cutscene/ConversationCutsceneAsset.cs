using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ConversationCutscene",
    menuName = "Lumen Corp/Cutscene/Conversation Cutscene"
)]
public class ConversationCutsceneAsset : ScriptableObject
{
    public string title;
    public string nextSceneName;
    public Sprite defaultPortrait;
    public List<ConversationLine> lines = new List<ConversationLine>();
}

[Serializable]
public class ConversationLine
{
    public string speakerName;
    public Sprite portrait;

    [TextArea(2, 6)]
    public string text;

    public ConversationAdvanceMode advanceMode = ConversationAdvanceMode.Click;

    [Tooltip("Only used when Advance Mode is WaitForSignal.")]
    public string requiredSignalId;

    [Tooltip("Only used when Advance Mode is WaitForSignal. If enabled, hides the assigned cutscene canvas until the required signal is completed.")]
    public bool hideCanvasWhileWaitingForSignal;

    public List<ConversationLineAction> startActions = new List<ConversationLineAction>();
    public List<ConversationLineAction> endActions = new List<ConversationLineAction>();
}

[Serializable]
public class ConversationLineAction
{
    public ConversationLineActionType actionType = ConversationLineActionType.None;

    [Tooltip("Used by highlight actions to find a TutorialTarget in the current scene.")]
    public string targetId;

    [Tooltip("Used by SendSignal and StartNamedCoroutine actions.")]
    public string signalId;

    [Tooltip("Optional timing value used by actions that need a duration.")]
    public float duration = 0.25f;

    public bool clearPreviousHighlights = true;
}

public enum ConversationAdvanceMode
{
    Click,
    WaitForSignal
}

public enum ConversationLineActionType
{
    None,
    ShowHighlight,
    HideHighlight,
    HideAllHighlights,
    SendSignal,
    StartNamedCoroutine
}
