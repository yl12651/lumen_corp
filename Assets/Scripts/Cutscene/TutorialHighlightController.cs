using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TutorialHighlightController : MonoBehaviour
{
    [SerializeField] private RectTransform overlayRoot;
    [SerializeField] private RectTransform highlightFrame;
    [SerializeField] private CanvasGroup highlightCanvasGroup;
    [SerializeField] private Image highlightImage;
    [SerializeField] private Vector2 padding = new Vector2(24f, 24f);
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private float pulseScale = 1.06f;
    [SerializeField] private float pulseDuration = 0.45f;

    private Sequence pulseSequence;
    private string activeTargetId;

    public string ActiveTargetId => activeTargetId;

    private void Awake()
    {
        if (overlayRoot == null)
            overlayRoot = transform as RectTransform;

        if (highlightCanvasGroup == null && highlightFrame != null)
            highlightCanvasGroup = highlightFrame.GetComponent<CanvasGroup>();

        if (highlightImage == null && highlightFrame != null)
            highlightImage = highlightFrame.GetComponent<Image>();

        HideAllHighlights(true);
    }

    private void OnDestroy()
    {
        KillTweens();
    }

    public void ShowHighlight(string targetId, RectTransform target, float duration)
    {
        if (target == null || highlightFrame == null || overlayRoot == null)
            return;

        KillTweens();
        activeTargetId = targetId;

        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(overlayRoot, target);
        highlightFrame.anchorMin = new Vector2(0.5f, 0.5f);
        highlightFrame.anchorMax = new Vector2(0.5f, 0.5f);
        highlightFrame.pivot = new Vector2(0.5f, 0.5f);
        highlightFrame.anchoredPosition = bounds.center;
        highlightFrame.sizeDelta = new Vector2(bounds.size.x + padding.x, bounds.size.y + padding.y);
        highlightFrame.localScale = Vector3.one;
        highlightFrame.gameObject.SetActive(true);

        float resolvedFadeDuration = duration > 0f ? duration : fadeDuration;

        if (highlightCanvasGroup != null)
        {
            highlightCanvasGroup.alpha = 0f;
            highlightCanvasGroup.DOFade(1f, resolvedFadeDuration);
        }

        StartPulse();
    }

    public void HideHighlight(string targetId)
    {
        if (activeTargetId == targetId)
            HideAllHighlights(false);
    }

    public void HideAllHighlights(bool immediate = false)
    {
        activeTargetId = "";
        KillTweens();

        if (highlightFrame == null)
            return;

        if (immediate || highlightCanvasGroup == null)
        {
            if (highlightCanvasGroup != null)
                highlightCanvasGroup.alpha = 0f;

            highlightFrame.gameObject.SetActive(false);
            return;
        }

        highlightCanvasGroup
            .DOFade(0f, fadeDuration)
            .OnComplete(() => highlightFrame.gameObject.SetActive(false));
    }

    private void StartPulse()
    {
        KillPulseSequence();

        if (highlightFrame == null)
            return;

        Color baseColor = highlightImage != null ? highlightImage.color : Color.white;
        Color dimColor = baseColor;
        dimColor.a *= 0.55f;

        pulseSequence = DOTween.Sequence();
        pulseSequence
            .Append(highlightFrame.DOScale(pulseScale, pulseDuration));

        if (highlightImage != null)
            pulseSequence.Join(highlightImage.DOColor(dimColor, pulseDuration));

        pulseSequence.Append(highlightFrame.DOScale(1f, pulseDuration));

        if (highlightImage != null)
            pulseSequence.Join(highlightImage.DOColor(baseColor, pulseDuration));

        pulseSequence
            .SetLoops(-1)
            .SetEase(Ease.InOutSine);
    }

    private void KillTweens()
    {
        KillPulseSequence();

        if (highlightFrame != null)
        {
            highlightFrame.DOKill();
            highlightFrame.localScale = Vector3.one;
        }

        if (highlightCanvasGroup != null)
            highlightCanvasGroup.DOKill();

        if (highlightImage != null)
            highlightImage.DOKill();
    }

    private void KillPulseSequence()
    {
        if (pulseSequence != null)
        {
            pulseSequence.Kill();
            pulseSequence = null;
        }
    }
}
