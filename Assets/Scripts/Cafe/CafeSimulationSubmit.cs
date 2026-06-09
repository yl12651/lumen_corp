using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class CafeCoworkerPair
{
    public string pairKey;
    public string positionName;
    public AssignmentDropPanel firstPanel;
    public AssignmentDropPanel secondPanel;
}

public class CafeSimulationSubmit : MonoBehaviour
{
    [Header("Assignments")]
    [SerializeField] private List<AssignmentDropPanel> assignmentPanels = new List<AssignmentDropPanel>();

    [Header("Coworker Pairs")]
    [SerializeField] private List<CafeCoworkerPair> coworkerPairs = new List<CafeCoworkerPair>();

    [Header("Validation UI")]
    [SerializeField] private TMP_Text warningText;
    [SerializeField] private string missingAssignmentMessage = "Please assign a subject to every position before continuing.";

    [Header("Backend")]
    [SerializeField] private string backendUrl = "http://localhost:3000/api/simulate";

    [Header("Loading")]
    [SerializeField] private GameObject loadingCanvas;

    [SerializeField] private CafeConversationSetController conversationSetController;
    [SerializeField] private CafeConversationCutsceneController cutsceneController;
    
    private Coroutine warningCoroutine;
    
    private void Awake()
    {
        HideWarning();
        SetLoadingVisible(false);
    }

    public void SubmitCafeSimulation()
    {
        if (!AllPanelsAssigned())
        {
            ShowWarning(missingAssignmentMessage);
            return;
        }

        HideWarning();
        SetLoadingVisible(true);

        CafeSimulationRequest requestData = BuildRequestData();
        StartCoroutine(SendAssignmentsToBackend(requestData));
    }

    private bool AllPanelsAssigned()
    {
        if (coworkerPairs.Count > 0)
        {
            foreach (CafeCoworkerPair pair in coworkerPairs)
            {
                if (pair == null)
                    continue;

                if (pair.firstPanel == null || pair.secondPanel == null)
                    return false;

                if (!pair.firstPanel.HasAssignedCharacter || !pair.secondPanel.HasAssignedCharacter)
                    return false;
            }

            return true;
        }

        foreach (AssignmentDropPanel panel in assignmentPanels)
        {
            if (panel == null)
                continue;

            if (!panel.HasAssignedCharacter)
                return false;
        }

        return true;
    }

    private void ShowWarning(string message)
    {
        if (warningText == null)
            return;

        warningText.text = message;
        warningText.gameObject.SetActive(true);

        Color color = warningText.color;
        color.a = 1f;
        warningText.color = color;

        if (warningCoroutine != null)
            StopCoroutine(warningCoroutine);

        warningCoroutine = StartCoroutine(HideWarningAfterDelay());
    }

    private IEnumerator HideWarningAfterDelay()
    {
        yield return new WaitForSeconds(2f);

        HideWarningImmediate();
        warningCoroutine = null;
    }

    private void HideWarning()
    {
        if (warningCoroutine != null)
        {
            StopCoroutine(warningCoroutine);
            warningCoroutine = null;
        }

        HideWarningImmediate();
    }

    private void HideWarningImmediate()
    {
        if (warningText == null)
            return;

        warningText.text = "";
        warningText.gameObject.SetActive(false);
    }

    private CafeSimulationRequest BuildRequestData()
    {
        CafeSimulationRequest request = new CafeSimulationRequest
        {
            pairs = new List<CafePairPayload>(),
            assignments = new List<AssignmentPayload>()
        };

        foreach (CafeCoworkerPair pair in coworkerPairs)
        {
            if (pair == null || pair.firstPanel == null || pair.secondPanel == null)
                continue;

            CafePairPayload pairPayload = new CafePairPayload
            {
                pairKey = GetPairKey(pair, request.pairs.Count),
                position = pair.positionName,
                subjects = new List<CafePairSubjectPayload>()
            };

            pairPayload.subjects.Add(BuildPairSubjectPayload(pairPayload.pairKey, pairPayload.position, "a", pair.firstPanel.AssignedSubject));
            pairPayload.subjects.Add(BuildPairSubjectPayload(pairPayload.pairKey, pairPayload.position, "b", pair.secondPanel.AssignedSubject));

            request.pairs.Add(pairPayload);
        }

        foreach (AssignmentDropPanel panel in assignmentPanels)
        {
            if (panel == null)
                continue;

            AssignmentPayload assignment = new AssignmentPayload
            {
                panelName = panel.PanelName,
                subject = panel.AssignedSubject
            };

            request.assignments.Add(assignment);
        }

        return request;
    }

    private string GetPairKey(CafeCoworkerPair pair, int index)
    {
        if (pair != null && !string.IsNullOrEmpty(pair.pairKey))
            return pair.pairKey;

        if (pair != null && !string.IsNullOrEmpty(pair.positionName))
            return pair.positionName;

        return "pair-" + (index + 1);
    }

    private CafePairSubjectPayload BuildPairSubjectPayload(
        string pairKey,
        string position,
        string slot,
        CharacterDefinition subject
    )
    {
        return new CafePairSubjectPayload
        {
            speakerKey = pairKey + ":" + slot,
            position = position,
            subject = subject
        };
    }

    private IEnumerator SendAssignmentsToBackend(CafeSimulationRequest requestData)
    {
        string jsonBody = JsonUtility.ToJson(requestData, true);

        Debug.Log("[CafeSimulationSubmit] Sending JSON:");
        Debug.Log(jsonBody);

        using UnityWebRequest request = new UnityWebRequest(backendUrl, "POST");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            SetLoadingVisible(false);
            Debug.LogError("[CafeSimulationSubmit] Request failed: " + request.error);
            Debug.LogError(request.downloadHandler.text);
            yield break;
        }

        Debug.Log("[CafeSimulationSubmit] GPT Result:");
        Debug.Log(request.downloadHandler.text);
        
        if (conversationSetController != null)
        {
            conversationSetController.PrepareFromBackendJson(request.downloadHandler.text);
            SetLoadingVisible(false);
        }
        else if (cutsceneController != null)
        {
            cutsceneController.PlayFromBackendJson(request.downloadHandler.text);
            SetLoadingVisible(false);
        }
        else
        {
            SetLoadingVisible(false);
        }
    }

    private void SetLoadingVisible(bool visible)
    {
        if (loadingCanvas != null)
            loadingCanvas.SetActive(visible);
    }

    [Serializable]
    private class CafeSimulationRequest
    {
        public List<CafePairPayload> pairs;
        public List<AssignmentPayload> assignments;
    }

    [Serializable]
    private class CafePairPayload
    {
        public string pairKey;
        public string position;
        public List<CafePairSubjectPayload> subjects;
    }

    [Serializable]
    private class CafePairSubjectPayload
    {
        public string speakerKey;
        public string position;
        public CharacterDefinition subject;
    }

    [Serializable]
    private class AssignmentPayload
    {
        public string panelName;
        public CharacterDefinition subject;
    }
}
