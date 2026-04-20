using UnityEngine;

public class CafeInteractable : MonoBehaviour
{
    public InteractableId interactableId;
    public HoverPanelData panelData;

    [Header("Highlight")]
    public Renderer[] targetRenderers;

    private MaterialPropertyBlock mpb;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<Renderer>();

        mpb = new MaterialPropertyBlock();
    }

    public void SetHighlighted(bool highlighted)
    {
        foreach (var r in targetRenderers)
        {
            r.GetPropertyBlock(mpb);

            if (highlighted)
            {
                mpb.SetColor(EmissionColor, Color.white * 1.5f);
            }
            else
            {
                mpb.SetColor(EmissionColor, Color.black);
            }

            r.SetPropertyBlock(mpb);
        }
    }
}