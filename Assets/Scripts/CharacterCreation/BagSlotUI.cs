using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BagSlotUI : MonoBehaviour
{
    [SerializeField] private Button slotButton;
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text typeText;

    private int slotIndex;
    private BagUIController owner;
    private bool hasSubject;

    public void Setup(BagUIController controller, int index)
    {
        owner = controller;
        slotIndex = index;

        if (slotButton != null)
        {
            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(OnClicked);
        }
    }

    public void ShowEmpty()
    {
        hasSubject = false;

        if (portraitImage != null)
        {
            portraitImage.sprite = null;
            portraitImage.enabled = false;
        }

        if (typeText != null)
        {
            typeText.text = "";
        }

        if (slotButton != null)
        {
            slotButton.interactable = false;
        }
    }

    public void ShowSubject(CharacterDefinition subject, Sprite sprite)
    {
        hasSubject = true;

        if (portraitImage != null)
        {
            portraitImage.sprite = sprite;
            portraitImage.enabled = sprite != null;
            portraitImage.preserveAspect = true;
        }

        if (typeText != null)
        {
            typeText.text = subject.type;
        }

        if (slotButton != null)
        {
            slotButton.interactable = true;
        }
    }

    private void OnClicked()
    {
        if (!hasSubject)
            return;

        if (owner != null)
            owner.OnSlotClicked(slotIndex);
    }
}