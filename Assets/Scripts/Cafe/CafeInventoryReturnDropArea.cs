using UnityEngine;
using UnityEngine.EventSystems;

public class CafeInventoryReturnDropArea : MonoBehaviour, IDropHandler
{
    [SerializeField] private BagUIController bagUIController;

    public void OnDrop(PointerEventData eventData)
    {
        if (bagUIController == null)
            return;

        if (!CharacterDragState.IsDragging)
            return;

        int inventoryIndex = CharacterDragState.InventoryIndex;

        if (inventoryIndex < 0)
            return;

        bagUIController.ReturnInventoryIndexFromAssignment(inventoryIndex);
    }
}