using UnityEngine;

public static class TutorialSignalUtility
{
    public static bool SendTutorialSignalOnce(
        ConversationCutsceneController cutsceneController,
        string signalId,
        ref bool hasSentSignal
    )
    {
        if (hasSentSignal || cutsceneController == null || string.IsNullOrWhiteSpace(signalId))
            return false;

        bool acknowledged = cutsceneController.CompleteSignal(signalId);

        if (!acknowledged)
            return false;

        hasSentSignal = true;
        Debug.Log($"Signal acknowledged: {signalId}");
        return true;
    }
}
