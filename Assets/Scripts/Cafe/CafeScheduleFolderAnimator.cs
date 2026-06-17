using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class CafeScheduleFolderAnimator : MonoBehaviour
{
    [SerializeField] private List<FolderArtLayer> layers = new List<FolderArtLayer>();
    [SerializeField] private Vector2 stackedAnchoredPosition;
    [SerializeField] private bool captureCurrentPositionsAsExpandedOnAwake = true;

    [Header("Fall")]
    [SerializeField] private float fallHeight = 260f;
    [SerializeField] private float fallDuration = 0.35f;
    [SerializeField] private Ease fallEase = Ease.OutCubic;

    [Header("Expand")]
    [SerializeField] private float expandDelay = 0.08f;
    [SerializeField] private float expandDuration = 0.35f;
    [SerializeField] private Ease expandEase = Ease.OutCubic;

    private Sequence animationSequence;

    private void Awake()
    {
        CaptureExpandedPositions();
    }

    private void OnDisable()
    {
        KillAnimation();
    }

    private void OnDestroy()
    {
        KillAnimation();
    }

    public void PlayOpenAnimation()
    {
        KillAnimation();
        ApplyRenderOrder();

        Vector2 fallStartPosition = stackedAnchoredPosition + Vector2.up * fallHeight;

        foreach (FolderArtLayer layer in layers)
        {
            if (layer == null || layer.RectTransform == null)
                continue;

            layer.RectTransform.anchoredPosition = fallStartPosition;
        }

        animationSequence = DOTween.Sequence();

        foreach (FolderArtLayer layer in layers)
        {
            if (layer == null || layer.RectTransform == null)
                continue;

            animationSequence.Join(layer.RectTransform
                .DOAnchorPos(stackedAnchoredPosition, fallDuration)
                .SetEase(fallEase));
        }

        float expandStartTime = fallDuration + expandDelay;

        foreach (FolderArtLayer layer in layers)
        {
            if (layer == null || layer.RectTransform == null)
                continue;

            Vector2 targetPosition = layer.StaysStacked
                ? stackedAnchoredPosition
                : layer.ExpandedAnchoredPosition;

            animationSequence.Insert(expandStartTime, layer.RectTransform
                .DOAnchorPos(targetPosition, expandDuration)
                .SetEase(expandEase));
        }
    }

    public void ResetToExpandedPositions()
    {
        KillAnimation();
        ApplyRenderOrder();

        foreach (FolderArtLayer layer in layers)
        {
            if (layer == null || layer.RectTransform == null)
                continue;

            layer.RectTransform.anchoredPosition = layer.StaysStacked
                ? stackedAnchoredPosition
                : layer.ExpandedAnchoredPosition;
        }
    }

    private void CaptureExpandedPositions()
    {
        if (!captureCurrentPositionsAsExpandedOnAwake)
            return;

        foreach (FolderArtLayer layer in layers)
        {
            if (layer == null || layer.RectTransform == null || layer.StaysStacked)
                continue;

            layer.CaptureExpandedPosition();
        }
    }

    private void ApplyRenderOrder()
    {
        for (int i = 0; i < layers.Count; i++)
        {
            FolderArtLayer layer = layers[i];

            if (layer == null || layer.RectTransform == null)
                continue;

            int siblingIndex = layer.RenderSiblingIndex >= 0 ? layer.RenderSiblingIndex : i;
            layer.RectTransform.SetSiblingIndex(siblingIndex);
        }
    }

    private void KillAnimation()
    {
        if (animationSequence != null && animationSequence.IsActive())
            animationSequence.Kill(false);

        animationSequence = null;
    }

    [Serializable]
    public class FolderArtLayer
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private bool staysStacked;
        [SerializeField] private Vector2 expandedAnchoredPosition;
        [Tooltip("Use -1 to render by this list order. Larger sibling indices render later/on top.")]
        [SerializeField] private int renderSiblingIndex = -1;

        public RectTransform RectTransform => rectTransform;
        public bool StaysStacked => staysStacked;
        public Vector2 ExpandedAnchoredPosition => expandedAnchoredPosition;
        public int RenderSiblingIndex => renderSiblingIndex;

        public void CaptureExpandedPosition()
        {
            expandedAnchoredPosition = rectTransform.anchoredPosition;
        }
    }
}
