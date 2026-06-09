using UnityEngine;
using UnityEngine.UI;

public class CafeSceneReloadButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private SceneAsyncLoader sceneAsyncLoader;
    [SerializeField] private string cafeSceneName = "CafeScene";

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(ReloadCafeScene);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(ReloadCafeScene);
    }

    public void ReloadCafeScene()
    {
        if (sceneAsyncLoader == null)
            sceneAsyncLoader = FindFirstObjectByType<SceneAsyncLoader>();

        if (sceneAsyncLoader == null)
        {
            Debug.LogError("SceneAsyncLoader is required before reloading " + cafeSceneName + ".", this);
            return;
        }

        sceneAsyncLoader.LoadScene(cafeSceneName);
    }
}
