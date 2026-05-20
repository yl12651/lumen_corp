using UnityEngine;
using UnityEngine.UI;

public class CafeSimulationButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private CafeSimulationSubmit submitter;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(OnButtonClicked);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        if (submitter != null)
            submitter.SubmitCafeSimulation();
    }
}