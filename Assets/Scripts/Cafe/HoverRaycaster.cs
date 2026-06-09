using UnityEngine;

public class HoverRaycaster : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private LayerMask interactableMask;
    [SerializeField] private HoverUIManager hoverUI;

    private CafeInteractable current;
    private bool hoverEnabled = true;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void Update()
    {
        if (!hoverEnabled)
            return;

        Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, interactableMask))
        {
            CafeInteractable interactable = hit.collider.GetComponentInParent<CafeInteractable>();

            if (interactable != current)
            {
                ClearCurrent();

                current = interactable;

                if (current != null)
                {
                    current.SetHighlighted(true);

                    if (hoverUI != null)
                        hoverUI.Show(current.panelData);
                }
            }
        }
        else
        {
            ClearCurrent();
        }
    }

    private void ClearCurrent()
    {
        if (current != null)
        {
            current.SetHighlighted(false);
            current = null;
        }

        if (hoverUI != null)
            hoverUI.Hide();
    }

    public void SetHoverEnabled(bool enabled)
    {
        hoverEnabled = enabled;

        if (!hoverEnabled)
            ClearCurrent();
    }
}
