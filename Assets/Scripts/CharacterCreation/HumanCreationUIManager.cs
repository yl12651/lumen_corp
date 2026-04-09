using UnityEngine;

public class HumanCreationUIManager : MonoBehaviour
{
    [SerializeField] private TraitRowUI curiosityRow;
    [SerializeField] private TraitRowUI disciplineRow;
    [SerializeField] private TraitRowUI driveRow;
    [SerializeField] private TraitRowUI empathyRow;
    [SerializeField] private TraitRowUI instabilityRow;
    [SerializeField] private TraitRowUI submissionRow;

    public void PrintCurrentBuild()
    {
        Debug.Log("Current Subject Build:");
        Debug.Log("Curiosity Extract: " + curiosityRow.SelectedValue);
        Debug.Log("Discipline Compound: " + disciplineRow.SelectedValue);
        Debug.Log("Drive Serum: " + driveRow.SelectedValue);
        Debug.Log("Empathy Medium: " + empathyRow.SelectedValue);
        Debug.Log("Instability Reagent: " + instabilityRow.SelectedValue);
        Debug.Log("Submission Catalyst: " + submissionRow.SelectedValue);
    }
}