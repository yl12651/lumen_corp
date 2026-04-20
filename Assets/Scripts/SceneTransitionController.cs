using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionController : MonoBehaviour
{
    public void LoadCafeScene()
    {
        SceneManager.LoadScene("CafeScene");
    }
}