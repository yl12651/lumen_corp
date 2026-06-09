using UnityEngine;

public class CafeCharacterSpriteHoverHighlight : MonoBehaviour
{
    [SerializeField] private Outline outline;

    private void Awake()
    {
        if (outline == null)
            outline = GetComponent<Outline>();

        if (outline == null)
            outline = GetComponentInChildren<Outline>();

        SetHighlighted(false);
    }

    private void OnMouseEnter()
    {
        SetHighlighted(true);
    }

    private void OnMouseExit()
    {
        SetHighlighted(false);
    }

    public void SetHighlighted(bool highlighted)
    {
        if (outline != null)
            outline.enabled = highlighted;
    }
}
