using UnityEngine;

public class CafeSchedulePanelController : MonoBehaviour
{
    [SerializeField] private GameObject schedulePanelRoot;
    [SerializeField] private bool hideOnAwake = true;
    [SerializeField] private BagUIController bagUIController;
    [SerializeField] private CafeScheduleFolderAnimator folderAnimator;

    private void Awake()
    {
        if (folderAnimator == null && schedulePanelRoot != null)
            folderAnimator = schedulePanelRoot.GetComponentInChildren<CafeScheduleFolderAnimator>(true);

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
        if (schedulePanelRoot != null)
            schedulePanelRoot.SetActive(visible);

        if (visible && bagUIController != null)
            bagUIController.RefreshAll();

        if (visible && folderAnimator != null)
            folderAnimator.PlayOpenAnimation();
    }
}
