using UnityEngine;

public class CafeSchedulePanelController : MonoBehaviour
{
    [SerializeField] private GameObject schedulePanelRoot;
    [SerializeField] private bool hideOnAwake = true;
    [SerializeField] private BagUIController bagUIController;
    [SerializeField] private CafeScheduleIntroAnimator introAnimator;
    [SerializeField] private ConversationCutsceneController cutsceneController;
    [SerializeField] private string scheduleOpenedSignalId = "cafe_schedule_opened";
    [SerializeField] private string scheduleClosedSignalId = "cafe_schedule_closed";

    private bool hasOpenedSchedule;
    private bool hasSentScheduleOpenedSignal;
    private bool hasSentScheduleClosedSignal;

    private void Awake()
    {
        if (introAnimator == null && schedulePanelRoot != null)
            introAnimator = schedulePanelRoot.GetComponentInChildren<CafeScheduleIntroAnimator>(true);

        if (cutsceneController == null)
            cutsceneController = FindFirstObjectByType<ConversationCutsceneController>();

        if (hideOnAwake)
            SetVisible(false);
    }

    public void Open()
    {
        SetVisible(true);
    }

    public void Close()
    {
        SetVisible(false);
    }

    public void Toggle()
    {
        SetVisible(schedulePanelRoot == null || !schedulePanelRoot.activeSelf);
    }

    private void SetVisible(bool visible)
    {
        bool wasVisible = schedulePanelRoot != null && schedulePanelRoot.activeSelf;

        if (schedulePanelRoot != null)
            schedulePanelRoot.SetActive(visible);

        if (visible)
        {
            hasOpenedSchedule = true;

            TutorialSignalUtility.SendTutorialSignalOnce(
                cutsceneController,
                scheduleOpenedSignalId,
                ref hasSentScheduleOpenedSignal
            );
        }

        if (visible && introAnimator != null)
            introAnimator.HandleScheduleOpened();

        if (visible && bagUIController != null)
            bagUIController.RefreshAll();

        if (!visible && wasVisible && hasOpenedSchedule)
        {
            TutorialSignalUtility.SendTutorialSignalOnce(
                cutsceneController,
                scheduleClosedSignalId,
                ref hasSentScheduleClosedSignal
            );
        }
    }
}
