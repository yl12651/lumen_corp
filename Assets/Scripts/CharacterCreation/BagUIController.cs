using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BagUIController : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject bagPanel;
    [SerializeField] private GameObject creationPanel;

    [Header("Cafe Inventory Grid")]
    [SerializeField] private GameObject cafeSlotGrid;
    [SerializeField] private List<BagSlotUI> cafeSlotUIs = new List<BagSlotUI>();

    [Header("Bag Panel Slots")]
    [SerializeField] private List<BagSlotUI> slotUIs = new List<BagSlotUI>();

    [Header("Assignment Panels")]
    [SerializeField] private List<AssignmentDropPanel> assignmentPanels = new List<AssignmentDropPanel>();

    [Header("Inspect Panel")]
    [SerializeField] private Image inspectImage;
    [SerializeField] private TMP_Text inspectTypeText;
    [SerializeField] private TMP_Text inspectDescriptionText;
    [SerializeField] private GameObject removeButton;

    [Header("Sprite Mapping")]
    [SerializeField] private List<CharacterSpriteEntry> characterSprites = new List<CharacterSpriteEntry>();

    [Header("Drag Preview")]
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private Image dragPreviewImage;

    private Dictionary<string, Sprite> spriteLookup = new Dictionary<string, Sprite>();

    // Key = inventory index in GameSession.
    // Value = the assignment panel currently holding that character.
    private Dictionary<int, AssignmentDropPanel> assignedLookup = new Dictionary<int, AssignmentDropPanel>();

    private int currentDisplayIndex = -1;

    private void Awake()
    {
        BuildSpriteLookup();

        for (int i = 0; i < slotUIs.Count; i++)
        {
            slotUIs[i].Setup(this, i, SlotViewType.Bag, false);
        }

        for (int i = 0; i < cafeSlotUIs.Count; i++)
        {
            cafeSlotUIs[i].Setup(this, i, SlotViewType.Cafe, true);
        }

        foreach (AssignmentDropPanel panel in assignmentPanels)
        {
            if (panel != null)
                panel.Setup(this);
        }

        if (bagPanel != null)
            bagPanel.SetActive(false);

        if (cafeSlotGrid != null)
            cafeSlotGrid.SetActive(true);

        if (dragPreviewImage != null)
            dragPreviewImage.gameObject.SetActive(false);

        if (removeButton != null)
            removeButton.SetActive(false);

        RefreshAll();
    }

    private void BuildSpriteLookup()
    {
        spriteLookup.Clear();

        foreach (CharacterSpriteEntry entry in characterSprites)
        {
            if (!string.IsNullOrEmpty(entry.id) && entry.sprite != null)
            {
                spriteLookup[entry.id] = entry.sprite;
            }
        }
    }

    public void OpenBag()
    {
        if (bagPanel != null)
            bagPanel.SetActive(true);

        if (creationPanel != null)
            creationPanel.SetActive(false);

        if (cafeSlotGrid != null)
            cafeSlotGrid.SetActive(false);

        RefreshBag();
        ClearInspect();
    }

    public void CloseBag()
    {
        if (bagPanel != null)
            bagPanel.SetActive(false);

        if (creationPanel != null)
            creationPanel.SetActive(true);

        if (cafeSlotGrid != null)
            cafeSlotGrid.SetActive(true);

        RefreshCafeSlots();
    }

    public void RefreshAll()
    {
        RefreshBag();
        RefreshCafeSlots();
        RefreshAssignmentPanels();
    }

    public void RefreshBag()
    {
        if (GameSession.Instance == null)
            return;

        int bagCount = GameSession.Instance.GetBagCount();

        for (int i = 0; i < slotUIs.Count; i++)
        {
            if (i < bagCount)
            {
                CharacterDefinition subject = GameSession.Instance.GetSubjectAt(i);
                Sprite sprite = GetSpriteForSubject(subject);
                slotUIs[i].ShowSubject(subject, sprite);
            }
            else
            {
                slotUIs[i].ShowEmpty();
            }
        }
    }

    public void RefreshCafeSlots()
    {
        if (GameSession.Instance == null)
            return;

        int bagCount = GameSession.Instance.GetBagCount();

        for (int i = 0; i < cafeSlotUIs.Count; i++)
        {
            if (i < bagCount)
            {
                if (IsInventoryIndexAssigned(i))
                {
                    // The character still exists in GameSession,
                    // but it disappears from the available cafe slots.
                    cafeSlotUIs[i].ShowEmpty();
                }
                else
                {
                    CharacterDefinition subject = GameSession.Instance.GetSubjectAt(i);
                    Sprite sprite = GetSpriteForSubject(subject);
                    cafeSlotUIs[i].ShowSubject(subject, sprite);
                }
            }
            else
            {
                cafeSlotUIs[i].ShowEmpty();
            }
        }
    }

    private void RefreshAssignmentPanels()
    {
        foreach (AssignmentDropPanel panel in assignmentPanels)
        {
            if (panel != null)
                panel.RefreshDisplay();
        }
    }

    public void OnSlotClicked(int slotIndex, SlotViewType viewType)
    {
        Debug.Log("OnSlotClicked triggered: " + slotIndex);
        if (GameSession.Instance == null)
            return;

        CharacterDefinition subject = GameSession.Instance.GetSubjectAt(slotIndex);

        if (subject == null)
        {
            if (viewType == SlotViewType.Bag)
                ClearInspect();

            return;
        }

        if (viewType == SlotViewType.Bag)
        {
            ShowInspect(slotIndex, subject);
        }
        else if (viewType == SlotViewType.Cafe)
        {
            Debug.Log("Cafe slot clicked: " + subject.type);
        }
    }

    private void ShowInspect(int slotIndex, CharacterDefinition subject)
    {
        if (inspectTypeText != null)
            inspectTypeText.text = subject.type;

        if (inspectDescriptionText != null)
            inspectDescriptionText.text = subject.description;

        if (inspectImage != null)
        {
            Sprite sprite = GetSpriteForSubject(subject);
            inspectImage.sprite = sprite;
            inspectImage.enabled = sprite != null;
            inspectImage.preserveAspect = true;
        }

        currentDisplayIndex = slotIndex;

        if (removeButton != null)
            removeButton.SetActive(true);
    }

    public void OnRemoveSlotClicked()
    {
        if (GameSession.Instance == null)
            return;

        if (currentDisplayIndex < 0)
            return;

        if (GameSession.Instance.RemoveSubject(currentDisplayIndex))
        {
            RebuildAssignmentsAfterInventoryRemove(currentDisplayIndex);

            ClearInspect();
            RefreshAll();
        }
    }

    private void RebuildAssignmentsAfterInventoryRemove(int removedIndex)
    {
        Dictionary<int, AssignmentDropPanel> rebuilt = new Dictionary<int, AssignmentDropPanel>();

        foreach (KeyValuePair<int, AssignmentDropPanel> pair in assignedLookup)
        {
            int oldIndex = pair.Key;
            AssignmentDropPanel panel = pair.Value;

            if (panel == null)
                continue;

            if (oldIndex == removedIndex)
            {
                panel.ClearAssignedWithoutNotify();
                continue;
            }

            int newIndex = oldIndex > removedIndex ? oldIndex - 1 : oldIndex;
            rebuilt[newIndex] = panel;
            panel.SetAssignedInventoryIndexWithoutNotify(newIndex);
        }

        assignedLookup = rebuilt;
    }

    public bool IsInventoryIndexAssigned(int inventoryIndex)
    {
        return assignedLookup.ContainsKey(inventoryIndex);
    }

    public CharacterDefinition GetSubjectAtInventoryIndex(int inventoryIndex)
    {
        if (GameSession.Instance == null)
            return null;

        return GameSession.Instance.GetSubjectAt(inventoryIndex);
    }

    public Sprite GetSpriteAtInventoryIndex(int inventoryIndex)
    {
        CharacterDefinition subject = GetSubjectAtInventoryIndex(inventoryIndex);
        return GetSpriteForSubject(subject);
    }

    public void AssignInventoryIndexToPanel(int inventoryIndex, AssignmentDropPanel targetPanel)
    {
        if (GameSession.Instance == null)
            return;

        if (targetPanel == null)
            return;

        CharacterDefinition subject = GameSession.Instance.GetSubjectAt(inventoryIndex);

        if (subject == null)
            return;

        // If this target panel already has someone, return that character first.
        int existingIndexInTarget = targetPanel.AssignedInventoryIndex;
        if (existingIndexInTarget >= 0)
        {
            ReturnInventoryIndexFromAssignment(existingIndexInTarget);
        }

        // If this character is already assigned to another panel, clear that old panel.
        if (assignedLookup.TryGetValue(inventoryIndex, out AssignmentDropPanel oldPanel))
        {
            if (oldPanel != null)
                oldPanel.ClearAssignedWithoutNotify();

            assignedLookup.Remove(inventoryIndex);
        }

        assignedLookup[inventoryIndex] = targetPanel;

        Sprite sprite = GetSpriteForSubject(subject);
        targetPanel.SetAssigned(inventoryIndex, subject, sprite);

        RefreshCafeSlots();
    }

    public void ReturnInventoryIndexFromAssignment(int inventoryIndex)
    {
        if (assignedLookup.TryGetValue(inventoryIndex, out AssignmentDropPanel panel))
        {
            if (panel != null)
                panel.ClearAssignedWithoutNotify();

            assignedLookup.Remove(inventoryIndex);
        }

        RefreshCafeSlots();
    }

    private Sprite GetSpriteForSubject(CharacterDefinition subject)
    {
        if (subject == null)
            return null;

        if (spriteLookup.TryGetValue(subject.id, out Sprite sprite))
            return sprite;

        return null;
    }

    private void ClearInspect()
    {
        if (inspectTypeText != null)
            inspectTypeText.text = "";

        if (inspectDescriptionText != null)
            inspectDescriptionText.text = "";

        if (inspectImage != null)
        {
            inspectImage.sprite = null;
            inspectImage.enabled = false;
        }

        currentDisplayIndex = -1;

        if (removeButton != null)
            removeButton.SetActive(false);
    }

    public void BeginDragPreview(Sprite sprite)
    {
        if (dragPreviewImage == null)
            return;

        dragPreviewImage.sprite = sprite;
        dragPreviewImage.enabled = true;
        dragPreviewImage.preserveAspect = true;
        dragPreviewImage.raycastTarget = false;

        Color color = dragPreviewImage.color;
        color.a = sprite != null ? 1f : 0f; // 1f = alpha 255
        dragPreviewImage.color = color;

        dragPreviewImage.gameObject.SetActive(sprite != null);
    }

    public void UpdateDragPreview(Vector2 screenPosition)
    {
        if (dragPreviewImage == null || rootCanvas == null)
            return;

        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        RectTransform previewRect = dragPreviewImage.rectTransform;

        Camera canvasCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : rootCanvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            canvasCamera,
            out Vector2 localPoint
        );

        previewRect.anchoredPosition = localPoint;
    }

    public void EndDragPreview()
    {
        if (dragPreviewImage == null)
            return;

        Color color = dragPreviewImage.color;
        color.a = 0f;
        dragPreviewImage.color = color;

        dragPreviewImage.sprite = null;
        dragPreviewImage.gameObject.SetActive(false);
    }
}