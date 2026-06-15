using UnityEngine;

public class CafeSchedulePanelController : MonoBehaviour
{
    [SerializeField] private GameObject schedulePanelRoot;
    [SerializeField] private bool hideOnAwake = true;
    [SerializeField] private BagUIController bagUIController;

    private void Awake()
    {
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
    }
}
