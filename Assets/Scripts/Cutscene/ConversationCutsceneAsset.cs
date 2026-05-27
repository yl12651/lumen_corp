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
}

public enum ConversationAdvanceMode
{
    Click,
    WaitForSignal
}