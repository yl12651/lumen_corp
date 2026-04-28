using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BagSlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI References")]
    [SerializeField] private Image portraitImage;

    [Header("Optional")]
    [SerializeField] private TMP_Text typeText;

    private BagUIController controller;
    private int slotIndex;
    private SlotViewType viewType;
    private bool canDrag;

    private CharacterDefinition currentSubject;
    private Sprite currentSprite;

    public void Setup(BagUIController bagUIController, int index, SlotViewType slotViewType, bool draggable)
    {
        controller = bagUIController;
        slotIndex = index;
        viewType = slotViewType;
        canDrag = draggable;
    }

    public void ShowSubject(CharacterDefinition subject, Sprite sprite)
    {
        currentSubject = subject;
        currentSprite = sprite;

        if (portraitImage != null)
        {
            portraitImage.sprite = sprite;
            portraitImage.enabled = sprite != null;
            portraitImage.preserveAspect = true;

            Color color = portraitImage.color;
            color.a = sprite != null ? 1f : 0f;
            portraitImage.color = color;
        }

        if (typeText != null)
            typeText.text = subject != null ? subject.type : "";
    }

    public void ShowEmpty()
    {
        currentSubject = null;
        currentSprite = null;

        if (portraitImage != null)
        {
            portraitImage.sprite = null;
            portraitImage.enabled = true;

            Color color = portraitImage.color;
            color.a = 0f;
            portraitImage.color = color;
        }

        if (typeText != null)
            typeText.text = "";
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (controller == null)
            return;

        controller.OnSlotClicked(slotIndex, viewType);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!canDrag)
            return;

        if (controller == null)
            return;

        if (currentSubject == null)
            return;

        if (viewType == SlotViewType.Cafe && controller.IsInventoryIndexAssigned(slotIndex))
            return;

        CharacterDragState.BeginDrag(slotIndex, currentSubject);

        controller.BeginDragPreview(currentSprite);
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