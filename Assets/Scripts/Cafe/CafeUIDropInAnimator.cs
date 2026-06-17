using DG.Tweening;
using UnityEngine;

public class CafeUIDropInAnimator : MonoBehaviour
{
    [SerializeField] private RectTransform animatedTarget;
    [SerializeField] private Vector2 fallStartOffset = new Vector2(0f, 260f);
    [SerializeField] private float fallDuration = 0.35f;
    [SerializeField] private Ease fallEase = Ease.OutCubic;
    [SerializeField] private bool restoreHomePositionOnDisable = true;

    private Vector2 homeAnchoredPosition;
    private Tween activeTween;

    private void Awake()
    {
        if (animatedTarget == null)
            animatedTarget = GetComponent<RectTransform>();

        if (animatedTarget != null)
            homeAnchoredPosition = animatedTarget.anchoredPosition;
    }

    private void OnDisable()
    {
        KillActiveTween();

        if (restoreHomePositionOnDisable && animatedTarget != null)
            animatedTarget.anchoredPosition = homeAnchoredPosition;
    }

    private void OnDestroy()
    {
        KillActiveTween();
    }

    public void Play()
    {
        if (animatedTarget == null)
            return;

        KillActiveTween();

        animatedTarget.anchoredPosition = homeAnchoredPosition + fallStartOffset;
        activeTween = animatedTarget
            .DOAnchorPos(homeAnchoredPosition, fallDuration)
            .SetEase(fallEase);
    }

    private void KillActiveTween()
    {
        if (activeTween != null && activeTween.IsActive())
            activeTween.Kill(false);

        activeTween = null;
    }
}
