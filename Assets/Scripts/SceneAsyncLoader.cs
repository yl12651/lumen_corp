using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneAsyncLoader : MonoBehaviour
{
    [Header("Loading UI")]
    [SerializeField] private GameObject loadingUIRoot;
    [SerializeField, Min(0f)] private float minimumLoadingSeconds = 0.75f;

    public void LoadScene(string sceneName)
    {
        if (SceneAsyncLoadRunner.IsLoading || string.IsNullOrWhiteSpace(sceneName))
            return;

        SceneAsyncLoadRunner.LoadScene(sceneName, loadingUIRoot, minimumLoadingSeconds);
    }
}

internal sealed class SceneAsyncLoadRunner : MonoBehaviour
{
    private static bool isLoading;

    private GameObject loadingUIInstance;

    public static bool IsLoading => isLoading;

    public static void LoadScene(string sceneName, GameObject loadingUIRoot = null, float minimumLoadingSeconds = 0f)
    {
        if (isLoading || string.IsNullOrWhiteSpace(sceneName))
            return;

        GameObject runnerObject = new GameObject(nameof(SceneAsyncLoadRunner));
        DontDestroyOnLoad(runnerObject);

        SceneAsyncLoadRunner runner = runnerObject.AddComponent<SceneAsyncLoadRunner>();
        runner.StartCoroutine(runner.LoadSceneRoutine(sceneName, loadingUIRoot, Mathf.Max(0f, minimumLoadingSeconds)));
    }

    private IEnumerator LoadSceneRoutine(string sceneName, GameObject loadingUIRoot, float minimumLoadingSeconds)
    {
        isLoading = true;
        ShowLoadingUI(loadingUIRoot);
        float loadingStartTime = Time.unscaledTime;

        // Give the loading canvas one frame to render before scene loading starts.
        yield return null;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);

        if (loadOperation == null)
        {
            HideLoadingUI();
            isLoading = false;
            Destroy(gameObject);
            yield break;
        }

        loadOperation.allowSceneActivation = false;

        while (loadOperation.progress < 0.9f)
            yield return null;

        while (Time.unscaledTime - loadingStartTime < minimumLoadingSeconds)
            yield return null;

        loadOperation.allowSceneActivation = true;

        while (!loadOperation.isDone)
            yield return null;

        HideLoadingUI();
        isLoading = false;
        Destroy(gameObject);
    }

    private void ShowLoadingUI(GameObject loadingUIRoot)
    {
        Debug.Log("Showing loading UI");
        if (loadingUIRoot != null)
        {
            loadingUIInstance = Instantiate(loadingUIRoot);
            loadingUIInstance.name = loadingUIRoot.name;
        }

        if (loadingUIInstance == null)
            return;

        DontDestroyOnLoad(loadingUIInstance);
        loadingUIInstance.SetActive(true);

        foreach (Animator animator in loadingUIInstance.GetComponentsInChildren<Animator>(true))
        {
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.enabled = true;
        }
    }

    private void HideLoadingUI()
    {
        if (loadingUIInstance != null)
            Destroy(loadingUIInstance);
    }
}
