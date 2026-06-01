using UnityEngine;
using UnityEngine.UI;

public class TutorialSliderSignalSender : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private ConversationCutsceneController cutsceneController;
    [SerializeField] private string signalId;
    [SerializeField] private bool sendOnlyOnce = true;

    private bool hasSentSignal;
    private bool hasStarted;

    private void Awake()
    {
        if (slider == null)
            slider = GetComponent<Slider>();
    }

    private void Start()
    {
        hasStarted = true;
        AddListener();
    }

    private void OnEnable()
    {
        if (hasStarted)
            AddListener();
    }

    private void OnDisable()
    {
        if (slider != null)
            slider.onValueChanged.RemoveListener(OnSliderValueChanged);
    }

    private void OnSliderValueChanged(float value)
    {
        if (sendOnlyOnce && hasSentSignal)
            return;

        if (cutsceneController == null || string.IsNullOrWhiteSpace(signalId))
            return;

        if (cutsceneController.CompleteSignal(signalId))
            hasSentSignal = true;
    }

    public void ResetSignal()
    {
        hasSentSignal = false;
    }

    private void AddListener()
    {
        if (slider == null)
            return;

        slider.onValueChanged.RemoveListener(OnSliderValueChanged);
        slider.onValueChanged.AddListener(OnSliderValueChanged);
    }
}
