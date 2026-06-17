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

    private readonly Dictionary<string, List<TutorialTarget>> targetLookup = new Dictionary<string, List<TutorialTarget>>();

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

        bool clearedHighlightsForLine = false;

        foreach (ConversationLineAction action in actions)
            RunAction(action, ref clearedHighlightsForLine);
    }

    private void RunAction(ConversationLineAction action, ref bool clearedHighlightsForLine)
    {
        if (action == null)
            return;

        switch (action.actionType)
        {
            case ConversationLineActionType.ShowHighlight:
                ShowHighlight(action, ref clearedHighlightsForLine);
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

    private void ShowHighlight(ConversationLineAction action, ref bool clearedHighlightsForLine)
    {
        if (highlightController == null)
            return;

        if (action.clearPreviousHighlights && !clearedHighlightsForLine)
        {
            highlightController.HideAllHighlights(true);
            clearedHighlightsForLine = true;
        }

        if (!targetLookup.TryGetValue(action.targetId, out List<TutorialTarget> targets) || targets.Count == 0)
        {
            Debug.LogWarning("Tutorial target was not found: " + action.targetId, this);
            return;
        }

        bool highlightedAnyTarget = false;
        foreach (TutorialTarget target in targets)
        {
            if (target == null || target.RectTransform == null)
                continue;

            highlightController.ShowHighlight(action.targetId, target.RectTransform, action.duration);
            highlightedAnyTarget = true;
        }

        if (!highlightedAnyTarget)
            Debug.LogWarning("Tutorial target has no RectTransform: " + action.targetId, this);
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

        if (!targetLookup.TryGetValue(target.TargetId, out List<TutorialTarget> targets))
        {
            targets = new List<TutorialTarget>();
            targetLookup[target.TargetId] = targets;
        }

        if (!targets.Contains(target))
            targets.Add(target);
    }
}

[Serializable]
public class TutorialCoroutineEntry
{
    public string routineId;
    public MonoBehaviour runner;
    public string methodName;
}
