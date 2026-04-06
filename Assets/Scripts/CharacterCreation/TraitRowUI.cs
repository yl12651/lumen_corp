using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TraitRowUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text traitLabel;
    [SerializeField] private List<Button> levelButtons = new List<Button>();

    [Header("Visuals")]
    [SerializeField] private Color normalColor = new Color(0.25f, 0.25f, 0.25f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.95f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color normalTextColor = Color.white;
    [SerializeField] private Color selectedTextColor = Color.black;

    [Header("Default")]
    [SerializeField] [Range(1, 5)] private int defaultLevel = 3;

    private int selectedLevel;

    public int SelectedLevel => selectedLevel;

    private void Awake()
    {
        for (int i = 0; i < levelButtons.Count; i++)
        {
            int capturedLevel = i + 1;
            levelButtons[i].onClick.AddListener(() => SelectLevel(capturedLevel));
        }

        selectedLevel = Mathf.Clamp(defaultLevel, 1, 5);
        RefreshVisuals();
    }

    public void SetLabel(string newLabel)
    {
        if (traitLabel != null)
            traitLabel.text = newLabel;
    }

    public void SelectLevel(int level)
    {
        selectedLevel = Mathf.Clamp(level, 1, 5);
        RefreshVisuals();
    }

    private void RefreshVisuals()
    {
        for (int i = 0; i < levelButtons.Count; i++)
        {
            bool isSelected = (i + 1 == selectedLevel);

            Image buttonImage = levelButtons[i].GetComponent<Image>();
            if (buttonImage != null)
                buttonImage.color = isSelected ? selectedColor : normalColor;

            TMP_Text buttonText = levelButtons[i].GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
                buttonText.color = isSelected ? selectedTextColor : normalTextColor;
        }
    }
}