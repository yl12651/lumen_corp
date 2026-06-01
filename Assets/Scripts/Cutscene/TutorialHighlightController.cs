using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TutorialHighlightController : MonoBehaviour
{
    [SerializeField] private RectTransform overlayRoot;
    [SerializeField] private RectTransform dimPanelRoot;
    [SerializeField] private CanvasGroup dimCanvasGroup;
    [SerializeField] private RectTransform highlightFrame;
    [SerializeField] private CanvasGroup highlightCanvasGroup;
    [SerializeField] private Image highlightImage;
    [SerializeField] private Vector2 padding = new Vector2(24f, 24f);
    [SerializeField] private Color dimColor = new Color(0f, 0f, 0f, 0.72f);
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private float pulseScale = 1.02f;
    [SerializeField] private float pulseDuration = 0.45f;

    private Sequence pulseSequence;
    private string activeTargetId;
    private RectTransform topDimPanel;
    private RectTransform bottomDimPanel;
    private RectTransform leftDimPanel;
    private RectTransform rightDimPanel;

    public string ActiveTargetId => activeTargetId;

    private void Awake()
    {
        if (overlayRoot == null)
            overlayRoot = transform as RectTransform;

        if (highlightCanvasGroup == null && highlightFrame != null)
            highlightCanvasGroup = highlightFrame.GetComponent<CanvasGroup>();

        if (highlightImage == null && highlightFrame != null)
            highlightImage = highlightFrame.GetComponent<Image>();

        EnsureDimPanels();
        HideAllHighlights(true);
    }

    private void OnDestroy()
    {
        KillTweens();
    }

    public void ShowHighlight(string targetId, RectTransform target, float duration)
    {
        if (target == null || overlayRoot == null)
            return;

        KillTweens();
        activeTargetId = targetId;

        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(overlayRoot, target);
        Rect paddedTargetRect = GetPaddedTargetRect(bounds);
        LayoutDimPanels(paddedTargetRect);

        if (highlightFrame != null)
        {
            highlightFrame.anchorMin = new Vector2(0.5f, 0.5f);
            highlightFrame.anchorMax = new Vector2(0.5f, 0.5f);
            highlightFrame.pivot = new Vector2(0.5f, 0.5f);
            highlightFrame.anchoredPosition = paddedTargetRect.center;
            highlightFrame.sizeDelta = paddedTargetRect.size;
            highlightFrame.localScale = Vector3.one;
            highlightFrame.gameObject.SetActive(true);
            highlightFrame.SetAsLastSibling();
        }

        float resolvedFadeDuration = duration > 0f ? duration : fadeDuration;

        if (dimPanelRoot != null)
            dimPanelRoot.gameObject.SetActive(true);

        if (dimCanvasGroup != null)
        {
            dimCanvasGroup.alpha = 0f;
            dimCanvasGroup.DOFade(1f, resolvedFadeDuration);
        }

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

        if (immediate || highlightCanvasGroup == null)
        {
            if (dimCanvasGroup != null)
                dimCanvasGroup.alpha = 0f;

            if (highlightCanvasGroup != null)
                highlightCanvasGroup.alpha = 0f;

            if (dimPanelRoot != null)
                dimPanelRoot.gameObject.SetActive(false);

            if (highlightFrame != null)
                highlightFrame.gameObject.SetActive(false);
            return;
        }

        if (dimCanvasGroup != null)
        {
            dimCanvasGroup
                .DOFade(0f, fadeDuration)
                .OnComplete(() => dimPanelRoot.gameObject.SetActive(false));
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

        pulseSequence = DOTween.Sequence();
        pulseSequence
            .Append(highlightFrame.DOScale(pulseScale, pulseDuration));

        pulseSequence.Append(highlightFrame.DOScale(1f, pulseDuration));

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

        if (dimCanvasGroup != null)
            dimCanvasGroup.DOKill();

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

    private void EnsureDimPanels()
    {
        if (overlayRoot == null)
            return;

        if (dimPanelRoot == null)
        {
            GameObject root = new GameObject("Generated Dim Panels", typeof(RectTransform), typeof(CanvasGroup));
            dimPanelRoot = root.GetComponent<RectTransform>();
            dimPanelRoot.SetParent(overlayRoot, false);
            dimPanelRoot.anchorMin = Vector2.zero;
            dimPanelRoot.anchorMax = Vector2.one;
            dimPanelRoot.offsetMin = Vector2.zero;
            dimPanelRoot.offsetMax = Vector2.zero;
        }

        if (dimCanvasGroup == null)
            dimCanvasGroup = dimPanelRoot.GetComponent<CanvasGroup>();

        topDimPanel = EnsureDimPanel("Top Dim Panel", topDimPanel);
        bottomDimPanel = EnsureDimPanel("Bottom Dim Panel", bottomDimPanel);
        leftDimPanel = EnsureDimPanel("Left Dim Panel", leftDimPanel);
        rightDimPanel = EnsureDimPanel("Right Dim Panel", rightDimPanel);

        if (highlightFrame != null)
            highlightFrame.SetAsLastSibling();
    }

    private RectTransform EnsureDimPanel(string panelName, RectTransform currentPanel)
    {
        if (currentPanel != null)
            return currentPanel;

        GameObject panel = new GameObject(panelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.SetParent(dimPanelRoot, false);

        Image image = panel.GetComponent<Image>();
        image.color = dimColor;
        image.raycastTarget = false;

        return panelRect;
    }

    private Rect GetPaddedTargetRect(Bounds bounds)
    {
        Rect rootRect = overlayRoot.rect;
        Vector2 targetMin = new Vector2(bounds.min.x - padding.x * 0.5f, bounds.min.y - padding.y * 0.5f);
        Vector2 targetMax = new Vector2(bounds.max.x + padding.x * 0.5f, bounds.max.y + padding.y * 0.5f);

        targetMin.x = Mathf.Clamp(targetMin.x, rootRect.xMin, rootRect.xMax);
        targetMin.y = Mathf.Clamp(targetMin.y, rootRect.yMin, rootRect.yMax);
        targetMax.x = Mathf.Clamp(targetMax.x, rootRect.xMin, rootRect.xMax);
        targetMax.y = Mathf.Clamp(targetMax.y, rootRect.yMin, rootRect.yMax);

        return Rect.MinMaxRect(targetMin.x, targetMin.y, targetMax.x, targetMax.y);
    }

    private void LayoutDimPanels(Rect targetRect)
    {
        EnsureDimPanels();

        Rect rootRect = overlayRoot.rect;

        SetPanelRect(topDimPanel, rootRect.xMin, targetRect.yMax, rootRect.xMax, rootRect.yMax);
        SetPanelRect(bottomDimPanel, rootRect.xMin, rootRect.yMin, rootRect.xMax, targetRect.yMin);
        SetPanelRect(leftDimPanel, rootRect.xMin, targetRect.yMin, targetRect.xMin, targetRect.yMax);
        SetPanelRect(rightDimPanel, targetRect.xMax, targetRect.yMin, rootRect.xMax, targetRect.yMax);
    }

    private void SetPanelRect(RectTransform panel, float xMin, float yMin, float xMax, float yMax)
    {
        if (panel == null)
            return;

        float width = Mathf.Max(0f, xMax - xMin);
        float height = Mathf.Max(0f, yMax - yMin);
        panel.gameObject.SetActive(width > 0f && height > 0f);

        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = new Vector2((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f);
        panel.sizeDelta = new Vector2(width, height);
    }
}
