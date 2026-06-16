using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CafeRetentionSelectionEntry : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image subjectImage;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private TMP_Text positionText;
    [SerializeField] private UnityEngine.UI.Outline selectedOutline;

    private CafeAssignedSubjectSelection assignedSubject;
    private UnityAction<CafeRetentionSelectionEntry> clicked;

    public CafeAssignedSubjectSelection AssignedSubject => assignedSubject;
    public bool IsSelected { get; private set; }

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (selectedOutline == null)
            selectedOutline = GetComponent<UnityEngine.UI.Outline>();

        if (button != null)
            button.onClick.AddListener(OnClicked);

        SetSelected(false);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClicked);
    }

    public void Setup(CafeAssignedSubjectSelection subjectSelection, UnityAction<CafeRetentionSelectionEntry> onClicked)
    {
        assignedSubject = subjectSelection;
        clicked = onClicked;

        if (subjectImage != null)
        {
            subjectImage.sprite = subjectSelection != null ? subjectSelection.sprite : null;
            subjectImage.enabled = subjectImage.sprite != null;
            subjectImage.preserveAspect = true;
        }

        if (typeText != null)
            typeText.text = subjectSelection != null && subjectSelection.subject != null
                ? subjectSelection.subject.type
                : "";

        if (positionText != null)
            positionText.text = subjectSelection != null ? subjectSelection.positionName : "";

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;

        if (selectedOutline != null)
            selectedOutline.enabled = selected;
    }

    private void OnClicked()
    {
        clicked?.Invoke(this);
    }
}
