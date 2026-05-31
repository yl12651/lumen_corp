using UnityEngine;

public class SceneTransitionController : MonoBehaviour
{
    [SerializeField] private SceneAsyncLoader sceneAsyncLoader;

    public void LoadCafeScene()
    {
        LoadSceneAsync("CafeScene");
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
