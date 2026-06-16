using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(AssignmentDropPanel))]
public class CafeRetentionAssignmentPanelSelectable : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private AssignmentDropPanel assignmentPanel;
    [SerializeField] private UnityEngine.UI.Outline selectedOutline;

    private CafeAssignedSubjectSelection assignedSubject;
    private CafeRetentionSelectionController controller;

    public AssignmentDropPanel AssignmentPanel => assignmentPanel;
    public CafeAssignedSubjectSelection AssignedSubject => assignedSubject;
    public bool IsSelected { get; private set; }

    private void Awake()
    {
        ResolveReferences();
        SetSelected(false);
    }

    private void ResolveReferences()
    {
        if (assignmentPanel == null)
            assignmentPanel = GetComponent<AssignmentDropPanel>();

        if (selectedOutline == null)
            selectedOutline = GetComponent<UnityEngine.UI.Outline>();

        if (selectedOutline == null)
            selectedOutline = gameObject.AddComponent<UnityEngine.UI.Outline>();
    }

    public void Setup(
        CafeAssignedSubjectSelection subjectSelection,
        CafeRetentionSelectionController selectionController
    )
    {
        ResolveReferences();

        assignedSubject = subjectSelection;
        controller = selectionController;

        if (assignmentPanel != null && subjectSelection != null)
        {
            assignmentPanel.SetAssigned(
                subjectSelection.inventoryIndex,
                subjectSelection.subject,
                subjectSelection.sprite
            );
        }

        SetSelected(false);
    }

    public void Clear()
    {
        ResolveReferences();

        assignedSubject = null;
        SetSelected(false);

        if (assignmentPanel != null)
            assignmentPanel.ClearAssignedWithoutNotify();
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;

        if (selectedOutline != null)
            selectedOutline.enabled = selected;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (assignedSubject == null)
            return;

        if (controller != null)
            controller.ToggleAssignmentPanelSelection(this);
    }
}
