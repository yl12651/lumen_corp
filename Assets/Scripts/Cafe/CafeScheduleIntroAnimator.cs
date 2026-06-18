using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CafeScheduleIntroAnimator : MonoBehaviour
{
    [SerializeField] private GameObject set1;
    [SerializeField] private GameObject set2;
    [SerializeField] private GameObject set3;

    [Header("Fall")]
    [SerializeField] private Vector2 set1FallStartOffset = new Vector2(0f, 260f);
    [SerializeField] private float set1FallDuration = 0.35f;
    [SerializeField] private Ease set1FallEase = Ease.OutCubic;

    [Header("Set 1 To Set 2")]
    [SerializeField] private float set1FadeOutDuration = 0.2f;
    [SerializeField] private float set2FadeInDuration = 0.2f;
    [SerializeField] private Ease set1ToSet2FadeEase = Ease.InOutSine;

    [Header("Set 2 To Set 3")]
    [SerializeField] private float set2FadeOutDuration = 0.2f;
    [SerializeField] private float set3FadeInDuration = 0.2f;
    [SerializeField] private Ease set2ToSet3FadeEase = Ease.InOutSine;
    [Tooltip("Set2/Set3 graphics that represent the same shared visual and should not animate alpha during that transition. If both sets have a copy, assign both copies here.")]
    [SerializeField] private List<Graphic> set2ToSet3SharedGraphics = new List<Graphic>();

    private RectTransform set1RectTransform;
    private CanvasGroup set1CanvasGroup;
    private CanvasGroup set2CanvasGroup;
    private CanvasGroup set3CanvasGroup;
    private readonly Dictionary<Graphic, float> defaultGraphicAlphas = new Dictionary<Graphic, float>();
    private Vector2 set1HomeAnchoredPosition;
    private Sequence activeSequence;
    private bool introHasPlayed;
    private bool transitionInProgress;

    private void Awake()
    {
        ResolveSets();
        PrepareReferences();
        RegisterClickTargets();
        ShowSet3Immediate();
    }

    private void OnDisable()
    {
        KillActiveSequence();
    }

    private void OnDestroy()
    {
        KillActiveSequence();
    }

    public void HandleScheduleOpened()
    {
        if (introHasPlayed)
        {
            ShowSet3Immediate();
            return;
        }

        PlayInitialSet1Fall();
    }

    public void ShowSet2()
    {
        if (introHasPlayed || transitionInProgress)
            return;

        KillActiveSequence();
        transitionInProgress = true;

        SetState(set1, set1CanvasGroup, true, 1f, true);
        SetState(set2, set2CanvasGroup, true, 0f, false);
        SetState(set3, set3CanvasGroup, false, 0f, false);

        activeSequence = DOTween.Sequence()
            .Join(FadeSet(set1, 0f, set1FadeOutDuration, set1ToSet2FadeEase, null))
            .Join(FadeSet(set2, 1f, set2FadeInDuration, set1ToSet2FadeEase, null))
            .OnComplete(() =>
            {
                SetState(set1, set1CanvasGroup, false, 0f, false);
                SetState(set2, set2CanvasGroup, true, 1f, true);
                transitionInProgress = false;
            });
    }

    public void ShowSet3()
    {
        if (introHasPlayed || transitionInProgress)
            return;

        KillActiveSequence();
        transitionInProgress = true;

        SetState(set1, set1CanvasGroup, false, 0f, false);
        SetState(set2, set2CanvasGroup, true, 1f, true);
        SetState(set3, set3CanvasGroup, true, 0f, false);

        activeSequence = DOTween.Sequence()
            .Join(FadeSet(set2, 0f, set2FadeOutDuration, set2ToSet3FadeEase, set2ToSet3SharedGraphics))
            .Join(FadeSet(set3, 1f, set3FadeInDuration, set2ToSet3FadeEase, set2ToSet3SharedGraphics))
            .OnComplete(() =>
            {
                introHasPlayed = true;
                SetState(set2, set2CanvasGroup, false, 0f, false);
                SetState(set3, set3CanvasGroup, true, 1f, true);
                transitionInProgress = false;
            });
    }

    private void PlayInitialSet1Fall()
    {
        KillActiveSequence();
        transitionInProgress = true;

        SetState(set1, set1CanvasGroup, true, 1f, true);
        SetState(set2, set2CanvasGroup, false, 0f, false);
        SetState(set3, set3CanvasGroup, false, 0f, false);

        if (set1RectTransform == null)
        {
            transitionInProgress = false;
            return;
        }

        set1RectTransform.anchoredPosition = set1HomeAnchoredPosition + set1FallStartOffset;

        activeSequence = DOTween.Sequence()
            .Append(set1RectTransform
                .DOAnchorPos(set1HomeAnchoredPosition, set1FallDuration)
                .SetEase(set1FallEase))
            .OnComplete(() => transitionInProgress = false);
    }

    private void ResolveSets()
    {
        if (set1 == null)
            set1 = FindDirectChild("Set1");

        if (set2 == null)
            set2 = FindDirectChild("Set2");

        if (set3 == null)
            set3 = FindDirectChild("Set3");
    }

    private GameObject FindDirectChild(string childName)
    {
        Transform child = transform.Find(childName);
        return child == null ? null : child.gameObject;
    }

    private void PrepareReferences()
    {
        set1RectTransform = set1 == null ? null : set1.GetComponent<RectTransform>();

        if (set1RectTransform != null)
            set1HomeAnchoredPosition = set1RectTransform.anchoredPosition;

        set1CanvasGroup = GetOrAddCanvasGroup(set1);
        set2CanvasGroup = GetOrAddCanvasGroup(set2);
        set3CanvasGroup = GetOrAddCanvasGroup(set3);

        CaptureDefaultGraphicAlphas(set1);
        CaptureDefaultGraphicAlphas(set2);
        CaptureDefaultGraphicAlphas(set3);
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        if (target == null)
            return null;

        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = target.AddComponent<CanvasGroup>();

        return canvasGroup;
    }

    private void RegisterClickTargets()
    {
        RegisterClickTarget(set1, ShowSet2);
        RegisterClickTarget(set2, ShowSet3);
    }

    private void RegisterClickTarget(GameObject target, System.Action clicked)
    {
        if (target == null)
            return;

        CafeScheduleIntroClickTarget clickTarget = target.GetComponent<CafeScheduleIntroClickTarget>();

        if (clickTarget == null)
            clickTarget = target.AddComponent<CafeScheduleIntroClickTarget>();

        clickTarget.Setup(clicked);
    }

    private void ShowSet3Immediate()
    {
        KillActiveSequence();
        transitionInProgress = false;

        SetState(set1, set1CanvasGroup, false, 0f, false);
        SetState(set2, set2CanvasGroup, false, 0f, false);
        SetState(set3, set3CanvasGroup, true, 1f, true);
    }

    private void SetState(
        GameObject target,
        CanvasGroup canvasGroup,
        bool active,
        float alpha,
        bool interactable)
    {
        if (target != null)
            target.SetActive(active);

        if (canvasGroup == null)
            return;

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = active && interactable;
        canvasGroup.blocksRaycasts = active && interactable;

        SetSetGraphicAlpha(target, alpha);
    }

    private Tween FadeSet(
        GameObject target,
        float targetAlphaMultiplier,
        float duration,
        Ease ease,
        List<Graphic> excludedGraphics)
    {
        Sequence sequence = DOTween.Sequence();

        if (target == null)
            return sequence;

        Graphic[] graphics = target.GetComponentsInChildren<Graphic>(true);

        foreach (Graphic graphic in graphics)
        {
            if (graphic == null || IsExcluded(graphic, excludedGraphics))
                continue;

            float defaultAlpha = GetDefaultGraphicAlpha(graphic);
            sequence.Join(graphic
                .DOFade(defaultAlpha * targetAlphaMultiplier, duration)
                .SetEase(ease));
        }

        return sequence;
    }

    private void SetSetGraphicAlpha(GameObject target, float alphaMultiplier)
    {
        if (target == null)
            return;

        Graphic[] graphics = target.GetComponentsInChildren<Graphic>(true);

        foreach (Graphic graphic in graphics)
        {
            if (graphic == null)
                continue;

            Color color = graphic.color;
            color.a = GetDefaultGraphicAlpha(graphic) * alphaMultiplier;
            graphic.color = color;
        }
    }

    private void CaptureDefaultGraphicAlphas(GameObject target)
    {
        if (target == null)
            return;

        Graphic[] graphics = target.GetComponentsInChildren<Graphic>(true);

        foreach (Graphic graphic in graphics)
        {
            if (graphic != null && !defaultGraphicAlphas.ContainsKey(graphic))
                defaultGraphicAlphas.Add(graphic, graphic.color.a);
        }
    }

    private float GetDefaultGraphicAlpha(Graphic graphic)
    {
        if (graphic == null)
            return 1f;

        if (defaultGraphicAlphas.TryGetValue(graphic, out float alpha))
        {
            if (alpha <= 0f && graphic.color.a > 0f)
            {
                alpha = graphic.color.a;
                defaultGraphicAlphas[graphic] = alpha;
            }

            return alpha;
        }

        defaultGraphicAlphas.Add(graphic, graphic.color.a);
        return graphic.color.a;
    }

    private bool IsExcluded(Graphic graphic, List<Graphic> excludedGraphics)
    {
        return excludedGraphics != null && excludedGraphics.Contains(graphic);
    }

    private void KillActiveSequence()
    {
        if (activeSequence != null && activeSequence.IsActive())
            activeSequence.Kill(false);

        activeSequence = null;
    }
}
