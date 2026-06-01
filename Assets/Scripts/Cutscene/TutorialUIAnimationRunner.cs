using System.Collections;
using DG.Tweening;
using UnityEngine;

public class TutorialUIAnimationRunner : MonoBehaviour
{
    [SerializeField] private RectTransform curiositySlider;

    public IEnumerator PulseCuriositySlider()
    {
        if (curiositySlider == null)
            yield break;

        curiositySlider.DOKill();
        curiositySlider.localScale = Vector3.one;

        yield return curiositySlider
            .DOScale(1.08f, 0.25f)
            .SetLoops(4, LoopType.Yoyo)
            .WaitForCompletion();

        curiositySlider.localScale = Vector3.one;
    }
}