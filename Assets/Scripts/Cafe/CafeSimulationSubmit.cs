using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class CafeSimulationSubmit : MonoBehaviour
{
    [Header("Assignments")]
    [SerializeField] private List<AssignmentDropPanel> assignmentPanels = new List<AssignmentDropPanel>();

    [Header("Validation UI")]
    [SerializeField] private TMP_Text warningText;
    [SerializeField] private string missingAssignmentMessage = "Please assign a subject to every position before continuing.";

    [Header("Backend")]
    [SerializeField] private string backendUrl = "http://localhost:3000/api/simulate";

    [SerializeField] private CafeConversationCutsceneController cutsceneController;
    
    private Coroutine warningCoroutine;
    
    private void Awake()
    {
        HideWarning();
    }

    public void SubmitCafeSimulation()
    {
        if (!AllPanelsAssigned())
        {
            ShowWarning(missingAssignmentMessage);
            return;
        }

        HideWarning();

        CafeSimulationRequest requestData = BuildRequestData();
        StartCoroutine(SendAssignmentsToBackend(requestData));
    }

    private bool AllPanelsAssigned()
    {
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
            assignments = new List<AssignmentPayload>()
        };

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
            Debug.LogError("[CafeSimulationSubmit] Request failed: " + request.error);
            Debug.LogError(request.downloadHandler.text);
            yield break;
        }

        Debug.Log("[CafeSimulationSubmit] GPT Result:");
        Debug.Log(request.downloadHandler.text);
        
        if (cutsceneController != null)
        {
            cutsceneController.PlayFromBackendJson(request.downloadHandler.text);
        }
    }

    [Serializable]
    private class CafeSimulationRequest
    {
        public List<AssignmentPayload> assignments;
    }

    [Serializable]
    private class AssignmentPayload
    {
        public string panelName;
        public CharacterDefinition subject;
    }
}