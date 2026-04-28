using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AssignmentDropPanel : MonoBehaviour, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Assigned UI")]
    [SerializeField] private Image assignedImage;

    private BagUIController controller;

    public int AssignedInventoryIndex { get; private set; } = -1;

    private CharacterDefinition assignedSubject;
    private Sprite assignedSprite;

    public void Setup(BagUIController bagUIController)
    {
        controller = bagUIController;
        RefreshDisplay();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (controller == null)
            return;

        if (!CharacterDragState.IsDragging)
            return;

        int inventoryIndex = CharacterDragState.InventoryIndex;

        if (inventoryIndex < 0)
            return;

        controller.AssignInventoryIndexToPanel(inventoryIndex, this);
    }

    public void SetAssigned(int inventoryIndex, CharacterDefinition subject, Sprite sprite)
    {
        AssignedInventoryIndex = inventoryIndex;
        assignedSubject = subject;
        assignedSprite = sprite;

        RefreshDisplay();
    }

    public void SetAssignedInventoryIndexWithoutNotify(int inventoryIndex)
    {
        AssignedInventoryIndex = inventoryIndex;

        if (controller != null)
        {
            assignedSubject = controller.GetSubjectAtInventoryIndex(inventoryIndex);
            assignedSprite = controller.GetSpriteAtInventoryIndex(inventoryIndex);
        }

        RefreshDisplay();
    }

    public void ClearAssignedWithoutNotify()
    {
        AssignedInventoryIndex = -1;
        assignedSubject = null;
        assignedSprite = null;

        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        if (assignedImage != null)
        {
            assignedImage.sprite = assignedSprite;
            assignedImage.enabled = true;
            assignedImage.preserveAspect = true;

            Color color = assignedImage.color;
            color.a = assignedSprite != null ? 1f : 0f;
            assignedImage.color = color;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (controller == null)
            return;

        if (AssignedInventoryIndex < 0)
            return;

        if (assignedSubject == null)
            return;

        CharacterDragState.BeginDrag(AssignedInventoryIndex, assignedSubject);

        controller.BeginDragPreview(assignedSprite);
        controller.UpdateDragPreview(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!CharacterDragState.IsDragging)
            return;

        if (controller != null)
            controller.UpdateDragPreview(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!CharacterDragState.IsDragging)
            return;

        if (controller != null)
            controller.EndDragPreview();

        CharacterDragState.EndDrag();
    }
}