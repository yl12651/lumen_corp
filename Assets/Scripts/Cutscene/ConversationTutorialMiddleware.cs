using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ConversationTutorialMiddleware : MonoBehaviour
{
    [SerializeField] private TutorialHighlightController highlightController;
    [SerializeField] private List<TutorialTarget> registeredTargets = new List<TutorialTarget>();
    [SerializeField] private List<TutorialCoroutineEntry> coroutineEntries = new List<TutorialCoroutineEntry>();
    [SerializeField] private UnityEvent<string> actionSignalSent = new UnityEvent<string>();

    private readonly Dictionary<string, TutorialTarget> targetLookup = new Dictionary<string, TutorialTarget>();

    private void Awake()
    {
        RebuildTargetLookup();
    }

    public void RebuildTargetLookup()
    {
        targetLookup.Clear();

        TutorialTarget[] sceneTargets = FindObjectsByType<TutorialTarget>(FindObjectsSortMode.None);
        foreach (TutorialTarget target in sceneTargets)
            RegisterTarget(target);

        foreach (TutorialTarget target in registeredTargets)
            RegisterTarget(target);
    }

    public void HandleLineStarted(ConversationLine line)
    {
        if (line == null)
            return;

        RunActions(line.startActions);
    }

    public void HandleLineEnded(ConversationLine line)
    {
        if (line == null)
            return;

        RunActions(line.endActions);
    }

    private void RunActions(List<ConversationLineAction> actions)
    {
        if (actions == null)
            return;

        foreach (ConversationLineAction action in actions)
            RunAction(action);
    }

    private void RunAction(ConversationLineAction action)
    {
        if (action == null)
            return;

        switch (action.actionType)
        {
            case ConversationLineActionType.ShowHighlight:
                ShowHighlight(action);
                break;

            case ConversationLineActionType.HideHighlight:
                HideHighlight(action);
                break;

            case ConversationLineActionType.HideAllHighlights:
                HideAllHighlights();
                break;

            case ConversationLineActionType.SendSignal:
                SendSignal(action);
                break;

            case ConversationLineActionType.StartNamedCoroutine:
                StartNamedCoroutine(action);
                break;
        }
    }

    private void ShowHighlight(ConversationLineAction action)
    {
        if (highlightController == null)
            return;

        if (action.clearPreviousHighlights)
            highlightController.HideAllHighlights(true);

        if (!targetLookup.TryGetValue(action.targetId, out TutorialTarget target) || target.RectTransform == null)
        {
            Debug.LogWarning("Tutorial target was not found: " + action.targetId, this);
            return;
        }

        highlightController.ShowHighlight(action.targetId, target.RectTransform, action.duration);
    }

    private void HideHighlight(ConversationLineAction action)
    {
        if (highlightController != null)
            highlightController.HideHighlight(action.targetId);
    }

    private void HideAllHighlights()
    {
        if (highlightController != null)
            highlightController.HideAllHighlights(false);
    }

    private void SendSignal(ConversationLineAction action)
    {
        if (!string.IsNullOrEmpty(action.signalId))
            actionSignalSent.Invoke(action.signalId);
    }

    private void StartNamedCoroutine(ConversationLineAction action)
    {
        if (string.IsNullOrEmpty(action.signalId))
            return;

        foreach (TutorialCoroutineEntry entry in coroutineEntries)
        {
            if (entry == null || entry.routineId != action.signalId || entry.runner == null || string.IsNullOrEmpty(entry.methodName))
                continue;

            entry.runner.StartCoroutine(entry.methodName);
            return;
        }

        Debug.LogWarning("Tutorial coroutine entry was not found: " + action.signalId, this);
    }

    private void RegisterTarget(TutorialTarget target)
    {
        if (target == null || string.IsNullOrWhiteSpace(target.TargetId))
            return;

        targetLookup[target.TargetId] = target;
    }
}

[Serializable]
public class TutorialCoroutineEntry
{
    public string routineId;
    public MonoBehaviour runner;
    public string methodName;
}
