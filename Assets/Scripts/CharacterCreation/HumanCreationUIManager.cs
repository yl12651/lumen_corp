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
        Debug.Log("Curiosity Extract: " + curiosityRow.SelectedLevel);
        Debug.Log("Discipline Compound: " + disciplineRow.SelectedLevel);
        Debug.Log("Drive Serum: " + driveRow.SelectedLevel);
        Debug.Log("Empathy Medium: " + empathyRow.SelectedLevel);
        Debug.Log("Instability Reagent: " + instabilityRow.SelectedLevel);
        Debug.Log("Submission Catalyst: " + submissionRow.SelectedLevel);
    }
}