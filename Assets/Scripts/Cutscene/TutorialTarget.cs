using UnityEngine;

public class TutorialTarget : MonoBehaviour
{
    [SerializeField] private string targetId;

    public string TargetId => targetId;
    public RectTransform RectTransform => transform as RectTransform;
}
