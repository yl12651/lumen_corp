using UnityEngine;
using TMPro;

public class SceneTransitionController : MonoBehaviour
{
    [SerializeField] private SceneAsyncLoader sceneAsyncLoader;
    [SerializeField] private int minimumSubjectsForCafe = 6;
    
    [Header("Tutorial Signals")]
    [SerializeField] private ConversationCutsceneController cutsceneController;
    [SerializeField] private string notEnoughSignalId = "not_enough_triggered";
    
    private bool hasSentNotEnoughSignal;

    public void LoadCafeScene()
    {
        if (!HasEnoughSubjectsForCafe())
        {
            TutorialSignalUtility.SendTutorialSignalOnce(cutsceneController, notEnoughSignalId, ref hasSentNotEnoughSignal);
            return;
        }

        LoadSceneAsync("CafeScene");
    }

    private bool HasEnoughSubjectsForCafe()
    {
        if (GameSession.Instance == null)
            return false;

        return GameSession.Instance.GetBagCount() >= minimumSubjectsForCafe;
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
