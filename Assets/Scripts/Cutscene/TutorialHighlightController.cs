using DG.Tweening;
using System;
using System.Collections.Generic;
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

    private string activeTargetId;
    private readonly List<ActiveHighlight> activeHighlights = new List<ActiveHighlight>();
    private readonly List<ActiveHighlight> highlightPool = new List<ActiveHighlight>();
    private readonly List<RectTransform> dimPanelPool = new List<RectTransform>();

    public string ActiveTargetId => activeTargetId;

    private void Awake()
    {
        if (overlayRoot == null)
            overlayRoot = transform as RectTransform;

        if (highlightCanvasGroup == null && highlightFrame != null)
            highlightCanvasGroup = highlightFrame.GetComponent<CanvasGroup>();

        if (highlightImage == null && highlightFrame != null)
            highlightImage = highlightFrame.GetComponent<Image>();

        if (highlightFrame != null)
            highlightPool.Add(CreateActiveHighlight(highlightFrame));

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

        activeTargetId = targetId;

        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(overlayRoot, target);
        Rect paddedTargetRect = GetPaddedTargetRect(bounds);
        ActiveHighlight highlight = GetAvailableHighlightFrame();

        if (highlight.Frame != null)
        {
            highlight.TargetId = targetId;
            highlight.TargetRect = paddedTargetRect;
            highlight.Frame.anchorMin = new Vector2(0.5f, 0.5f);
            highlight.Frame.anchorMax = new Vector2(0.5f, 0.5f);
            highlight.Frame.pivot = new Vector2(0.5f, 0.5f);
            highlight.Frame.anchoredPosition = paddedTargetRect.center;
            highlight.Frame.sizeDelta = paddedTargetRect.size;
            highlight.Frame.localScale = Vector3.one;
            highlight.Frame.gameObject.SetActive(true);
            highlight.Frame.SetAsLastSibling();
        }

        activeHighlights.Add(highlight);
        LayoutDimPanels(GetActiveHighlightRects());

        float resolvedFadeDuration = duration > 0f ? duration : fadeDuration;

        if (dimPanelRoot != null)
            dimPanelRoot.gameObject.SetActive(true);

        if (dimCanvasGroup != null)
        {
            dimCanvasGroup.alpha = 0f;
            dimCanvasGroup.DOFade(1f, resolvedFadeDuration);
        }

        if (highlight.CanvasGroup != null)
        {
            highlight.CanvasGroup.alpha = 0f;
            highlight.CanvasGroup.DOFade(1f, resolvedFadeDuration);
        }

        StartPulse(highlight);
    }

    public void HideHighlight(string targetId)
    {
        bool removedAnyHighlight = false;

        for (int i = activeHighlights.Count - 1; i >= 0; i--)
        {
            ActiveHighlight highlight = activeHighlights[i];
            if (highlight.TargetId != targetId)
                continue;

            HideActiveHighlight(highlight, false);
            activeHighlights.RemoveAt(i);
            removedAnyHighlight = true;
        }

        if (!removedAnyHighlight)
            return;

        activeTargetId = activeHighlights.Count > 0 ? activeHighlights[activeHighlights.Count - 1].TargetId : "";

        if (activeHighlights.Count == 0)
            HideAllHighlights(false);
        else
            LayoutDimPanels(GetActiveHighlightRects());
    }

    public void HideAllHighlights(bool immediate = false)
    {
        activeTargetId = "";
        KillTweens();

        foreach (ActiveHighlight highlight in highlightPool)
            HideActiveHighlight(highlight, immediate);

        activeHighlights.Clear();

        if (immediate)
        {
            if (dimCanvasGroup != null)
                dimCanvasGroup.alpha = 0f;

            if (dimPanelRoot != null)
                dimPanelRoot.gameObject.SetActive(false);
            return;
        }

        if (dimCanvasGroup != null)
        {
            dimCanvasGroup
                .DOFade(0f, fadeDuration)
                .OnComplete(() => dimPanelRoot.gameObject.SetActive(false));
        }
    }

    private void StartPulse(ActiveHighlight highlight)
    {
        if (highlight == null || highlight.Frame == null)
            return;

        if (highlight.PulseSequence != null)
        {
            highlight.PulseSequence.Kill();
            highlight.PulseSequence = null;
        }

        highlight.Frame.DOKill();
        highlight.Frame.localScale = Vector3.one;

        highlight.PulseSequence = DOTween.Sequence();
        highlight.PulseSequence
            .Append(highlight.Frame.DOScale(pulseScale, pulseDuration));

        highlight.PulseSequence.Append(highlight.Frame.DOScale(1f, pulseDuration));

        highlight.PulseSequence
            .SetLoops(-1)
            .SetEase(Ease.InOutSine);
    }

    private void KillTweens()
    {
        foreach (ActiveHighlight highlight in highlightPool)
            highlight.KillTweens();

        if (dimCanvasGroup != null)
            dimCanvasGroup.DOKill();
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

        if (highlightFrame != null)
            highlightFrame.SetAsLastSibling();
    }

    private ActiveHighlight GetAvailableHighlightFrame()
    {
        foreach (ActiveHighlight highlight in highlightPool)
        {
            if (!activeHighlights.Contains(highlight))
                return highlight;
        }

        RectTransform sourceFrame = highlightFrame != null
            ? highlightFrame
            : activeHighlights.Count > 0 ? activeHighlights[0].Frame : null;

        if (sourceFrame == null)
            return new ActiveHighlight();

        RectTransform clonedFrame = Instantiate(sourceFrame, sourceFrame.parent);
        clonedFrame.name = sourceFrame.name + " (Generated)";
        ActiveHighlight clonedHighlight = CreateActiveHighlight(clonedFrame);
        highlightPool.Add(clonedHighlight);
        return clonedHighlight;
    }

    private ActiveHighlight CreateActiveHighlight(RectTransform frame)
    {
        CanvasGroup canvasGroup = frame.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = frame.gameObject.AddComponent<CanvasGroup>();

        return new ActiveHighlight
        {
            Frame = frame,
            CanvasGroup = canvasGroup,
            Image = frame.GetComponent<Image>(),
        };
    }

    private void HideActiveHighlight(ActiveHighlight highlight, bool immediate)
    {
        if (highlight == null || highlight.Frame == null)
            return;

        highlight.KillTweens();
        highlight.TargetId = "";
        highlight.Frame.localScale = Vector3.one;

        if (immediate || highlight.CanvasGroup == null)
        {
            if (highlight.CanvasGroup != null)
                highlight.CanvasGroup.alpha = 0f;

            highlight.Frame.gameObject.SetActive(false);
            return;
        }

        highlight.CanvasGroup
            .DOFade(0f, fadeDuration)
            .OnComplete(() => highlight.Frame.gameObject.SetActive(false));
    }

    private List<Rect> GetActiveHighlightRects()
    {
        List<Rect> targetRects = new List<Rect>();

        foreach (ActiveHighlight highlight in activeHighlights)
            targetRects.Add(highlight.TargetRect);

        return targetRects;
    }

    private RectTransform EnsureDimPanel(int index)
    {
        while (dimPanelPool.Count <= index)
        {
            GameObject panel = new GameObject("Generated Dim Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.SetParent(dimPanelRoot, false);

            Image image = panel.GetComponent<Image>();
            image.color = dimColor;
            image.raycastTarget = false;

            dimPanelPool.Add(panelRect);
        }

        return dimPanelPool[index];
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

    private void LayoutDimPanels(List<Rect> clearRects)
    {
        EnsureDimPanels();

        if (overlayRoot == null)
            return;

        Rect rootRect = overlayRoot.rect;
        List<float> xEdges = new List<float> { rootRect.xMin, rootRect.xMax };
        List<float> yEdges = new List<float> { rootRect.yMin, rootRect.yMax };

        if (clearRects != null)
        {
            foreach (Rect clearRect in clearRects)
            {
                xEdges.Add(Mathf.Clamp(clearRect.xMin, rootRect.xMin, rootRect.xMax));
                xEdges.Add(Mathf.Clamp(clearRect.xMax, rootRect.xMin, rootRect.xMax));
                yEdges.Add(Mathf.Clamp(clearRect.yMin, rootRect.yMin, rootRect.yMax));
                yEdges.Add(Mathf.Clamp(clearRect.yMax, rootRect.yMin, rootRect.yMax));
            }
        }

        xEdges.Sort();
        yEdges.Sort();

        int usedPanelCount = 0;

        for (int xIndex = 0; xIndex < xEdges.Count - 1; xIndex++)
        {
            float xMin = xEdges[xIndex];
            float xMax = xEdges[xIndex + 1];

            if (Mathf.Approximately(xMin, xMax))
                continue;

            for (int yIndex = 0; yIndex < yEdges.Count - 1; yIndex++)
            {
                float yMin = yEdges[yIndex];
                float yMax = yEdges[yIndex + 1];

                if (Mathf.Approximately(yMin, yMax))
                    continue;

                Vector2 center = new Vector2((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f);
                if (IsInsideAnyRect(center, clearRects))
                    continue;

                RectTransform panel = EnsureDimPanel(usedPanelCount);
                SetPanelRect(panel, xMin, yMin, xMax, yMax);
                usedPanelCount++;
            }
        }

        for (int i = usedPanelCount; i < dimPanelPool.Count; i++)
            dimPanelPool[i].gameObject.SetActive(false);
    }

    private bool IsInsideAnyRect(Vector2 point, List<Rect> rects)
    {
        if (rects == null)
            return false;

        foreach (Rect rect in rects)
        {
            if (rect.Contains(point))
                return true;
        }

        return false;
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

    [Serializable]
    private class ActiveHighlight
    {
        public string TargetId;
        public RectTransform Frame;
        public CanvasGroup CanvasGroup;
        public Image Image;
        public Rect TargetRect;
        public Sequence PulseSequence;

        public void KillTweens()
        {
            if (PulseSequence != null)
            {
                PulseSequence.Kill();
                PulseSequence = null;
            }

            if (Frame != null)
            {
                Frame.DOKill();
                Frame.localScale = Vector3.one;
            }

            if (CanvasGroup != null)
                CanvasGroup.DOKill();

            if (Image != null)
                Image.DOKill();
        }
    }
}
