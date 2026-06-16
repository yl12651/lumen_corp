using UnityEngine;

public class CafeCharacterSpriteHoverHighlight : MonoBehaviour
{
    [SerializeField] private Outline outline;
    [SerializeField] private CafeCharacterPairHoverGroup pairHoverGroup;

    private void Awake()
    {
        if (outline == null)
            outline = GetComponent<Outline>();

        if (outline == null)
            outline = GetComponentInChildren<Outline>();

        if (pairHoverGroup == null)
            pairHoverGroup = GetComponentInParent<CafeCharacterPairHoverGroup>();

        SetHighlighted(false);
    }

    private void OnMouseEnter()
    {
        if (pairHoverGroup != null)
            pairHoverGroup.NotifyHoverEntered(this);
        else
            SetHighlighted(true);
    }

    private void OnMouseExit()
    {
        if (pairHoverGroup != null)
            pairHoverGroup.NotifyHoverExited(this);
        else
            SetHighlighted(false);
    }

    public void SetHighlighted(bool highlighted)
    {
        if (outline != null)
            outline.enabled = highlighted;
    }
}
