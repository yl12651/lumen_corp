using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BagUIController : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject bagPanel;
    [SerializeField] private GameObject creationPanel;

    [Header("Slots")]
    [SerializeField] private List<BagSlotUI> slotUIs = new List<BagSlotUI>();

    [Header("Inspect Panel")]
    [SerializeField] private Image inspectImage;
    [SerializeField] private TMP_Text inspectTypeText;
    [SerializeField] private TMP_Text inspectDescriptionText;
    [SerializeField] private GameObject removeButton;

    [Header("Sprite Mapping")]
    [SerializeField] private List<CharacterSpriteEntry> characterSprites = new List<CharacterSpriteEntry>();

    private Dictionary<string, Sprite> spriteLookup = new Dictionary<string, Sprite>();
    private int currentDisplayIndex;

    private void Awake()
    {
        BuildSpriteLookup();

        for (int i = 0; i < slotUIs.Count; i++)
        {
            slotUIs[i].Setup(this, i);
        }

        if (bagPanel != null)
            bagPanel.SetActive(false);
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

        RefreshBag();
        ClearInspect();
    }

    public void CloseBag()
    {
        if (bagPanel != null)
            bagPanel.SetActive(false);
        if (creationPanel != null)
            creationPanel.SetActive(true);
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

    public void OnSlotClicked(int slotIndex)
    {
        if (GameSession.Instance == null)
            return;

        CharacterDefinition subject = GameSession.Instance.GetSubjectAt(slotIndex);
        if (subject == null)
        {
            ClearInspect();
            return;
        }

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
        removeButton.SetActive(true);
    }

    public void OnRemoveSlotClicked()
    {
        if (GameSession.Instance == null)
            return;
        if (GameSession.Instance.RemoveSubject(currentDisplayIndex))
        {
            ClearInspect();
            RefreshBag();
        }
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
    }
}