using UnityEngine;

public class CafeInteractable : MonoBehaviour
{
    public InteractableId interactableId;
    public HoverPanelData panelData;

    [Header("3D Outline Highlight")]
    [SerializeField] private Outline objectOutline;

    [Header("Linked UI Hover Effects")]
    [SerializeField] private UnityEngine.UI.Outline[] linkedUIHoverEffects;

    private void Awake()
    {
        if (objectOutline == null)
            objectOutline = GetComponent<Outline>();

        if (objectOutline == null)
            objectOutline = GetComponentInChildren<Outline>();

        if (objectOutline != null)
            objectOutline.enabled = false;

        SetLinkedUIHoverEffects(false);
    }

    public void SetHighlighted(bool highlighted)
    {
        if (objectOutline != null)
            objectOutline.enabled = highlighted;

        SetLinkedUIHoverEffects(highlighted);
    }

    private void SetLinkedUIHoverEffects(bool active)
    {
        foreach (UnityEngine.UI.Outline outline in linkedUIHoverEffects)
        {
            if (outline != null)
                outline.enabled = active;
        }
    }
}