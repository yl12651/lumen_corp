using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TraitRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text traitLabel;
    [SerializeField] private List<Button> levelButtons = new List<Button>();

    [SerializeField] private Color normalColor = new Color(0.25f, 0.25f, 0.25f);
    [SerializeField] private Color selectedColor = new Color(0.9f, 0.8f, 0.2f);

    private int selectedLevel = 0;

    public int SelectedLevel => selectedLevel;

    private void Start()
    {
        for (int i = 0; i < levelButtons.Count; i++)
        {
            int index = i + 1;
            levelButtons[i].onClick.AddListener(() => SelectLevel(index));
        }

        RefreshVisuals();
    }

    public void SetLabel(string newLabel)
    {
        if (traitLabel != null)
        {
            traitLabel.text = newLabel;
        }
    }

    public void SelectLevel(int level)
    {
        selectedLevel = level;
        RefreshVisuals();
    }

    private void RefreshVisuals()
    {
        for (int i = 0; i < levelButtons.Count; i++)
        {
            Image image = levelButtons[i].GetComponent<Image>();
            if (image != null)
            {
                image.color = (i + 1 == selectedLevel) ? selectedColor : normalColor;
            }
        }
    }
}