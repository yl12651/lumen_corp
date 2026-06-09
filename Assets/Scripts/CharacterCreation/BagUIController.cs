using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class BagCatalogEntry
{
    public string id;
    public string displayName;
    [TextArea] public string lockedHint;
}

[System.Serializable]
public class AssignmentWorldObjectBinding
{
    public AssignmentDropPanel panel;
    public GameObject targetObject;
    public SpriteRenderer spriteRenderer;
}

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
    [SerializeField] private GameObject inspectPanel;
    [SerializeField] private Image inspectImage;
    [SerializeField] private TMP_Text inspectTypeText;
    [SerializeField] private TMP_Text inspectCountText;
    [SerializeField] private TMP_Text inspectDescriptionText;
    [SerializeField] private GameObject removeButton;

    [Header("Catalog")]
    [SerializeField] private Sprite lockedCatalogSprite;
    [SerializeField] private List<BagCatalogEntry> catalogCharacters = new List<BagCatalogEntry>
    {
        new BagCatalogEntry { id = "Cor", displayName = "Subject-C" },
        new BagCatalogEntry { id = "Emo", displayName = "Subject-E" },
        new BagCatalogEntry { id = "Inw", displayName = "Subject-I" },
        new BagCatalogEntry { id = "Log", displayName = "Subject-L" },
        new BagCatalogEntry { id = "Rep", displayName = "Subject-R" },
        new BagCatalogEntry { id = "Soc", displayName = "Subject-S" }
    };

    [Header("Sprite Mapping")]
    [SerializeField] private List<CharacterSpriteEntry> characterSprites = new List<CharacterSpriteEntry>();

    [Header("Assignment World Sprites")]
    [SerializeField] private List<CharacterSpriteEntry> assignmentWorldSprites = new List<CharacterSpriteEntry>();
    [SerializeField] private List<AssignmentWorldObjectBinding> assignmentWorldObjects = new List<AssignmentWorldObjectBinding>();

    [Header("Drag Preview")]
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private Image dragPreviewImage;

    [Header("Tutorial Signals")]
    [SerializeField] private ConversationCutsceneController cutsceneController;
    [SerializeField] private string openBagSignalId = "bag_triggered";

    private Dictionary<string, Sprite> spriteLookup = new Dictionary<string, Sprite>();
    private Dictionary<string, Sprite> assignmentWorldSpriteLookup = new Dictionary<string, Sprite>();

    // Key = inventory index in GameSession.
    // Value = the assignment panel currently holding that character.
    private Dictionary<int, AssignmentDropPanel> assignedLookup = new Dictionary<int, AssignmentDropPanel>();

    private int currentDisplayIndex = -1;
    private bool hasSentOpenBagSignal;

    private void Awake()
    {
        BuildSpriteLookup();
        BuildAssignmentWorldSpriteLookup();

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

        if (inspectPanel != null)
            inspectPanel.SetActive(false);

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

    private void BuildAssignmentWorldSpriteLookup()
    {
        assignmentWorldSpriteLookup.Clear();

        foreach (CharacterSpriteEntry entry in assignmentWorldSprites)
        {
            if (!string.IsNullOrEmpty(entry.id) && entry.sprite != null)
            {
                assignmentWorldSpriteLookup[entry.id] = entry.sprite;
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
        TutorialSignalUtility.SendTutorialSignalOnce(cutsceneController, openBagSignalId, ref hasSentOpenBagSignal);
    }

    public void CloseBag()
    {
        if (inspectPanel.activeSelf)
        {
            inspectPanel.SetActive(false);
            return;
        }
        
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
        RefreshAssignmentWorldObjects();
    }

    public void RefreshBag()
    {
        Dictionary<string, int> ownedCounts = BuildOwnedCountLookup();

        for (int i = 0; i < slotUIs.Count; i++)
        {
            BagCatalogEntry catalogEntry = GetCatalogEntryAt(i);

            if (catalogEntry == null || string.IsNullOrEmpty(catalogEntry.id))
            {
                slotUIs[i].ShowEmpty();
                continue;
            }

            int count = ownedCounts.TryGetValue(catalogEntry.id, out int ownedCount) ? ownedCount : 0;
            bool isOwned = count > 0;
            Sprite sprite = isOwned ? GetSpriteForId(catalogEntry.id) : lockedCatalogSprite;
            string displayName = isOwned ? GetDisplayName(catalogEntry) : "???";

            slotUIs[i].ShowCatalogEntry(sprite, displayName, count);
        }
    }

    private Dictionary<string, int> BuildOwnedCountLookup()
    {
        Dictionary<string, int> ownedCounts = new Dictionary<string, int>();

        if (GameSession.Instance == null)
            return ownedCounts;

        int bagCount = GameSession.Instance.GetBagCount();

        for (int i = 0; i < bagCount; i++)
        {
            CharacterDefinition subject = GameSession.Instance.GetSubjectAt(i);

            if (subject == null || string.IsNullOrEmpty(subject.id))
                continue;

            if (ownedCounts.ContainsKey(subject.id))
                ownedCounts[subject.id]++;
            else
                ownedCounts[subject.id] = 1;
        }

        return ownedCounts;
    }

    private BagCatalogEntry GetCatalogEntryAt(int catalogIndex)
    {
        if (catalogIndex < 0)
            return null;

        if (catalogIndex < catalogCharacters.Count)
            return catalogCharacters[catalogIndex];

        if (catalogCharacters.Count > 0 || catalogIndex >= characterSprites.Count)
            return null;

        CharacterSpriteEntry spriteEntry = characterSprites[catalogIndex];

        return new BagCatalogEntry
        {
            id = spriteEntry.id,
            displayName = spriteEntry.id
        };
    }

    private string GetDisplayName(BagCatalogEntry catalogEntry)
    {
        CharacterDefinition subject = GetFirstOwnedSubjectById(catalogEntry.id);

        if (subject != null && !string.IsNullOrEmpty(subject.type))
            return subject.type;

        if (!string.IsNullOrEmpty(catalogEntry.displayName))
            return catalogEntry.displayName;

        return catalogEntry.id;
    }

    private CharacterDefinition GetFirstOwnedSubjectById(string id)
    {
        if (GameSession.Instance == null || string.IsNullOrEmpty(id))
            return null;

        int bagCount = GameSession.Instance.GetBagCount();

        for (int i = 0; i < bagCount; i++)
        {
            CharacterDefinition subject = GameSession.Instance.GetSubjectAt(i);

            if (subject != null && subject.id == id)
                return subject;
        }

        return null;
    }

    private int GetOwnedCount(string id)
    {
        if (GameSession.Instance == null || string.IsNullOrEmpty(id))
            return 0;

        int count = 0;
        int bagCount = GameSession.Instance.GetBagCount();

        for (int i = 0; i < bagCount; i++)
        {
            CharacterDefinition subject = GameSession.Instance.GetSubjectAt(i);

            if (subject != null && subject.id == id)
                count++;
        }

        return count;
    }

    private Sprite GetSpriteForId(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        if (spriteLookup.TryGetValue(id, out Sprite sprite))
            return sprite;

        return null;
    }

    private void ShowCatalogInspect(int catalogIndex)
    {
        BagCatalogEntry catalogEntry = GetCatalogEntryAt(catalogIndex);

        if (catalogEntry == null || string.IsNullOrEmpty(catalogEntry.id))
        {
            ClearInspect();
            return;
        }

        CharacterDefinition subject = GetFirstOwnedSubjectById(catalogEntry.id);
        bool isOwned = subject != null;
        int count = GetOwnedCount(catalogEntry.id);

        if (inspectTypeText != null)
            inspectTypeText.text = isOwned ? GetDisplayName(catalogEntry) : "???";

        if (inspectCountText != null)
            inspectCountText.text = $"Labor: {count}";

        if (inspectDescriptionText != null)
        {
            inspectDescriptionText.text = isOwned
                ? subject.description
                : catalogEntry.lockedHint;
        }

        if (inspectImage != null)
        {
            Sprite sprite = isOwned ? GetSpriteForId(catalogEntry.id) : lockedCatalogSprite;
            inspectImage.sprite = sprite;
            inspectImage.enabled = sprite != null;
            inspectImage.preserveAspect = true;
        }

        currentDisplayIndex = -1;

        if (inspectPanel != null)
            inspectPanel.SetActive(true);

        if (removeButton != null)
            removeButton.SetActive(false);
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

    private void RefreshAssignmentWorldObjects()
    {
        foreach (AssignmentWorldObjectBinding binding in assignmentWorldObjects)
        {
            if (binding == null)
                continue;

            SpriteRenderer renderer = GetAssignmentWorldRenderer(binding);
            bool hasAssignment = binding.panel != null && binding.panel.HasAssignedCharacter;
            Sprite sprite = hasAssignment ? GetAssignmentWorldSpriteForSubject(binding.panel.AssignedSubject) : null;

            if (renderer != null)
                renderer.sprite = sprite;

            GameObject targetObject = binding.targetObject != null
                ? binding.targetObject
                : renderer != null ? renderer.gameObject : null;

            if (targetObject != null)
                targetObject.SetActive(hasAssignment);
        }
    }

    public void OnSlotClicked(int slotIndex, SlotViewType viewType)
    {
        Debug.Log("OnSlotClicked triggered: " + slotIndex);

        if (viewType == SlotViewType.Bag)
        {
            ShowCatalogInspect(slotIndex);
        }
        else if (viewType == SlotViewType.Cafe)
        {
            if (GameSession.Instance == null)
                return;

            CharacterDefinition subject = GameSession.Instance.GetSubjectAt(slotIndex);

            if (subject == null)
                return;

            Debug.Log("Cafe slot clicked: " + subject.type);
        }
    }

    private void ShowInspect(int slotIndex, CharacterDefinition subject)
    {
        if (inspectTypeText != null)
            inspectTypeText.text = subject.type;

        if (inspectCountText != null)
            inspectCountText.text = "";

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

        if (inspectPanel != null)
            inspectPanel.SetActive(true);
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
        RefreshAssignmentWorldObjects();
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
        RefreshAssignmentWorldObjects();
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
        RefreshAssignmentWorldObjects();
    }

    private Sprite GetSpriteForSubject(CharacterDefinition subject)
    {
        if (subject == null)
            return null;

        if (spriteLookup.TryGetValue(subject.id, out Sprite sprite))
            return sprite;

        return null;
    }

    private Sprite GetAssignmentWorldSpriteForSubject(CharacterDefinition subject)
    {
        if (subject == null)
            return null;

        if (!string.IsNullOrEmpty(subject.id) && assignmentWorldSpriteLookup.TryGetValue(subject.id, out Sprite sprite))
            return sprite;

        if (!string.IsNullOrEmpty(subject.type) && assignmentWorldSpriteLookup.TryGetValue(subject.type, out sprite))
            return sprite;

        return null;
    }

    private SpriteRenderer GetAssignmentWorldRenderer(AssignmentWorldObjectBinding binding)
    {
        if (binding.spriteRenderer != null)
            return binding.spriteRenderer;

        if (binding.targetObject == null)
            return null;

        return binding.targetObject.GetComponentInChildren<SpriteRenderer>(true);
    }

    private void ClearInspect()
    {
        if (inspectTypeText != null)
            inspectTypeText.text = "";

        if (inspectCountText != null)
            inspectCountText.text = "";

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

        if (inspectPanel != null)
            inspectPanel.SetActive(false);
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
