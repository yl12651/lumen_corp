using TMPro;
using UnityEngine;

public class HoverUIManager : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    public void Show(HoverPanelData data)
    {
        panelRoot.SetActive(true);

        titleText.text = data.title ?? "";
        descriptionText.text = data.description ?? "";
    }

    public void Hide()
    {
        panelRoot.SetActive(false);
    }
}