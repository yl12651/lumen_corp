using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BagPreviewPageAnimator : MonoBehaviour
{
    [Header("Preview References")]
    [SerializeField] private Image characterImage;
    [SerializeField] private TMP_Text subjectTitle;
    [SerializeField] private TMP_Text subjectCount;
    [SerializeField] private TMP_Text subjectDescription;

    [Header("Page Timing")]
    [SerializeField] private float oldTextFadeDuration = 0.25f;
    [SerializeField] private float oldImageOutDuration = 0.35f;
    [SerializeField] private float newTextEnterDuration = 0.25f;
    [SerializeField] private float newImageInDuration = 0.35f;

    [Header("Text Animation")]
    [SerializeField] private float textEnterVerticalDistance = 80f;

    [Header("Image Animation")]
    [SerializeField] private float imageSlideDistance = 220f;
    [SerializeField] private float imageRotationDegrees = 18f;
    [SerializeField] private AnimationCurve imageRotationProgressCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 1f, 1f),
        new Keyframe(0.85f, 0.85f, 1f, 1f),
        new Keyframe(1f, 1f, 0f, 0f)
    );
    [SerializeField] private float imageTurnScaleXMultiplier = 0.1f;
    [SerializeField] private float imageTurnScaleYMultiplier = 1f;

    private Sequence pageSequence;
    private bool hasCachedHomeTransforms;
    private Vector2 titleHomePosition;
    private Vector2 countHomePosition;
    private Vector2 descriptionHomePosition;
    private Vector2 imageHomePosition;
    private Vector3 titleHomeScale;
    private Vector3 countHomeScale;
    private Vector3 descriptionHomeScale;
    private Vector3 imageHomeScale;
    private Quaternion titleHomeRotation;
    private Quaternion countHomeRotation;
    private Quaternion descriptionHomeRotation;
    private Quaternion imageHomeRotation;
    private Color titleHomeColor = Color.white;
    private Color countHomeColor = Color.white;
    private Color descriptionHomeColor = Color.white;
    private Color imageHomeColor = Color.white;

    public bool IsAnimating => pageSequence != null && pageSequence.IsActive();

    private void Awake()
    {
        ResolveMissingReferences();
        CacheHomeTransforms();
    }

    private void OnDisable()
    {
        Stop();
        RestoreHomeTransforms();
    }

    private void OnDestroy()
    {
        Stop();
    }

    public void Play(System.Action setIncomingContent, bool nextDirection)
    {
        ResolveMissingReferences();
        CacheHomeTransforms();
        Stop();

        int directionMultiplier = nextDirection ? 1 : -1;

        TMP_Text outgoingTitle = CreateTextClone(subjectTitle);
        TMP_Text outgoingCount = CreateTextClone(subjectCount);
        TMP_Text outgoingDescription = CreateTextClone(subjectDescription);
        Image outgoingImage = CreateImageClone(characterImage);

        HideLivePreviewObjects();

        pageSequence = DOTween.Sequence();

        JoinOutgoingTextTween(
            pageSequence,
            outgoingTitle,
            oldTextFadeDuration
        );

        JoinOutgoingTextTween(
            pageSequence,
            outgoingCount,
            oldTextFadeDuration
        );

        JoinOutgoingTextTween(
            pageSequence,
            outgoingDescription,
            oldTextFadeDuration
        );

        JoinOutgoingImageTweens(pageSequence, outgoingImage, directionMultiplier);

        pageSequence.AppendCallback(() =>
        {
            setIncomingContent?.Invoke();
            PrepareIncomingObjects(directionMultiplier);
        });

        JoinIncomingTextTween(
            pageSequence,
            subjectTitle,
            titleHomePosition,
            titleHomeColor,
            newTextEnterDuration
        );

        JoinIncomingTextTween(
            pageSequence,
            subjectCount,
            countHomePosition,
            countHomeColor,
            newTextEnterDuration
        );

        JoinIncomingTextTween(
            pageSequence,
            subjectDescription,
            descriptionHomePosition,
            descriptionHomeColor,
            newTextEnterDuration
        );

        JoinIncomingImageTweens(pageSequence);

        bool hasCleanedUp = false;
        TweenCallback cleanup = () =>
        {
            if (hasCleanedUp)
                return;

            hasCleanedUp = true;
            DestroyPreviewClone(outgoingTitle);
            DestroyPreviewClone(outgoingCount);
            DestroyPreviewClone(outgoingDescription);
            DestroyPreviewClone(outgoingImage);
            RestoreHomeTransforms();
            pageSequence = null;
        };

        pageSequence.OnComplete(cleanup);
        pageSequence.OnKill(cleanup);
    }

    public void Stop()
    {
        if (pageSequence == null)
            return;

        pageSequence.Kill();
        pageSequence = null;
    }

    public void RestoreHomeTransforms()
    {
        if (!hasCachedHomeTransforms)
            return;

        RestoreTextHomeTransform(subjectTitle, titleHomePosition, titleHomeScale, titleHomeRotation, titleHomeColor);
        RestoreTextHomeTransform(subjectCount, countHomePosition, countHomeScale, countHomeRotation, countHomeColor);
        RestoreTextHomeTransform(subjectDescription, descriptionHomePosition, descriptionHomeScale, descriptionHomeRotation, descriptionHomeColor);

        if (characterImage != null)
        {
            RectTransform imageRect = characterImage.rectTransform;
            imageRect.anchoredPosition = imageHomePosition;
            imageRect.localScale = imageHomeScale;
            imageRect.localRotation = imageHomeRotation;
            characterImage.color = imageHomeColor;
        }
    }

    private void CacheHomeTransforms()
    {
        if (hasCachedHomeTransforms)
            return;

        CacheTextHomeTransform(subjectTitle, ref titleHomePosition, ref titleHomeScale, ref titleHomeRotation, ref titleHomeColor);
        CacheTextHomeTransform(subjectCount, ref countHomePosition, ref countHomeScale, ref countHomeRotation, ref countHomeColor);
        CacheTextHomeTransform(subjectDescription, ref descriptionHomePosition, ref descriptionHomeScale, ref descriptionHomeRotation, ref descriptionHomeColor);

        if (characterImage != null)
        {
            RectTransform imageRect = characterImage.rectTransform;
            imageHomePosition = imageRect.anchoredPosition;
            imageHomeScale = imageRect.localScale;
            imageHomeRotation = imageRect.localRotation;
            imageHomeColor = characterImage.color;
        }

        hasCachedHomeTransforms = true;
    }

    private void ResolveMissingReferences()
    {
        if (characterImage == null)
            characterImage = FindChildComponentByName<Image>("CharacterImage");

        if (subjectTitle == null)
            subjectTitle = FindChildComponentByName<TMP_Text>("SubjectTitle");

        if (subjectCount == null)
            subjectCount = FindChildComponentByName<TMP_Text>("SubjectCount");

        if (subjectDescription == null)
            subjectDescription = FindChildComponentByName<TMP_Text>("SubjectDescription");
    }

    private T FindChildComponentByName<T>(string childName) where T : Component
    {
        T[] components = GetComponentsInChildren<T>(true);

        foreach (T component in components)
        {
            if (component.name == childName)
                return component;
        }

        return null;
    }

    private void CacheTextHomeTransform(
        TMP_Text text,
        ref Vector2 homePosition,
        ref Vector3 homeScale,
        ref Quaternion homeRotation,
        ref Color homeColor)
    {
        if (text == null)
            return;

        RectTransform rect = text.rectTransform;
        homePosition = rect.anchoredPosition;
        homeScale = rect.localScale;
        homeRotation = rect.localRotation;
        homeColor = text.color;
    }

    private TMP_Text CreateTextClone(TMP_Text source)
    {
        if (source == null)
            return null;

        TMP_Text clone = Instantiate(source, source.transform.parent);
        clone.name = source.name + "_OutgoingPreviewClone";
        clone.raycastTarget = false;
        clone.transform.SetSiblingIndex(source.transform.GetSiblingIndex());
        clone.rectTransform.anchoredPosition = source.rectTransform.anchoredPosition;
        clone.rectTransform.localRotation = source.rectTransform.localRotation;
        clone.rectTransform.localScale = source.rectTransform.localScale;
        clone.color = source.color;
        clone.gameObject.SetActive(source.gameObject.activeSelf);

        return clone;
    }

    private Image CreateImageClone(Image source)
    {
        if (source == null)
            return null;

        Image clone = Instantiate(source, source.transform.parent);
        clone.name = source.name + "_OutgoingPreviewClone";
        clone.raycastTarget = false;
        clone.transform.SetSiblingIndex(source.transform.GetSiblingIndex());
        clone.rectTransform.anchoredPosition = source.rectTransform.anchoredPosition;
        clone.rectTransform.localRotation = source.rectTransform.localRotation;
        clone.rectTransform.localScale = source.rectTransform.localScale;
        clone.color = source.color;
        clone.gameObject.SetActive(source.gameObject.activeSelf);

        return clone;
    }

    private void PrepareIncomingObjects(int directionMultiplier)
    {
        PrepareIncomingText(subjectTitle, titleHomePosition, titleHomeScale, titleHomeRotation, titleHomeColor);
        PrepareIncomingText(subjectCount, countHomePosition, countHomeScale, countHomeRotation, countHomeColor);
        PrepareIncomingText(subjectDescription, descriptionHomePosition, descriptionHomeScale, descriptionHomeRotation, descriptionHomeColor);

        if (characterImage != null)
        {
            RectTransform imageRect = characterImage.rectTransform;
            Color color = imageHomeColor;
            color.a = 0f;

            imageRect.anchoredPosition = imageHomePosition - new Vector2(imageSlideDistance * directionMultiplier, 0f);
            imageRect.localRotation = Quaternion.Euler(0f, -imageRotationDegrees * directionMultiplier, 0f);
            imageRect.localScale = GetTurnCompressedScale();
            characterImage.color = color;
            characterImage.gameObject.SetActive(true);
        }
    }

    private void PrepareIncomingText(
        TMP_Text text,
        Vector2 homePosition,
        Vector3 homeScale,
        Quaternion homeRotation,
        Color homeColor)
    {
        if (text == null)
            return;

        Color color = homeColor;
        color.a = 0f;

        RectTransform rect = text.rectTransform;
        rect.anchoredPosition = homePosition - new Vector2(0f, textEnterVerticalDistance);
        rect.localScale = homeScale;
        rect.localRotation = homeRotation;
        text.color = color;
    }

    private void HideLivePreviewObjects()
    {
        SetTextAlpha(subjectTitle, 0f);
        SetTextAlpha(subjectCount, 0f);
        SetTextAlpha(subjectDescription, 0f);

        if (characterImage != null)
        {
            Color color = characterImage.color;
            color.a = 0f;
            characterImage.color = color;
        }
    }

    private void SetTextAlpha(TMP_Text text, float alpha)
    {
        if (text == null)
            return;

        Color color = text.color;
        color.a = alpha;
        text.color = color;
    }

    private void JoinOutgoingTextTween(
        Sequence sequence,
        TMP_Text outgoingText,
        float duration)
    {
        if (sequence == null)
            return;

        if (outgoingText != null)
            sequence.Join(outgoingText.DOFade(0f, GetDuration(duration)).SetEase(Ease.InOutSine));
    }

    private void JoinIncomingTextTween(
        Sequence sequence,
        TMP_Text incomingText,
        Vector2 homePosition,
        Color homeColor,
        float duration)
    {
        if (sequence == null || incomingText == null)
            return;

        float resolvedDuration = GetDuration(duration);
        sequence.Join(incomingText.rectTransform.DOAnchorPos(homePosition, resolvedDuration).SetEase(Ease.OutSine));
        sequence.Join(incomingText.DOFade(homeColor.a, resolvedDuration).SetEase(Ease.InOutSine));
    }

    private void JoinOutgoingImageTweens(Sequence sequence, Image outgoingImage, int directionMultiplier)
    {
        if (sequence == null)
            return;

        float duration = GetDuration(oldImageOutDuration);
        Vector3 compressedScale = GetTurnCompressedScale();

        if (outgoingImage != null)
        {
            RectTransform outgoingRect = outgoingImage.rectTransform;
            Vector2 outgoingTarget = imageHomePosition + new Vector2(imageSlideDistance * directionMultiplier, 0f);
            Vector3 outgoingRotation = new Vector3(0f, imageRotationDegrees * directionMultiplier, 0f);

            sequence.Join(outgoingRect.DOAnchorPos(outgoingTarget, duration).SetEase(Ease.InOutSine));
            sequence.Join(outgoingRect.DOLocalRotate(outgoingRotation, duration).SetEase(GetImageRotationCurve()));
            sequence.Join(outgoingRect.DOScale(compressedScale, duration).SetEase(Ease.InOutSine));
            sequence.Join(outgoingImage.DOFade(0f, duration).SetEase(Ease.InOutSine));
        }
    }

    private void JoinIncomingImageTweens(Sequence sequence)
    {
        if (sequence == null || characterImage == null)
            return;

        float duration = GetDuration(newImageInDuration);
        RectTransform incomingRect = characterImage.rectTransform;

        sequence.Join(incomingRect.DOAnchorPos(imageHomePosition, duration).SetEase(Ease.InOutSine));
        sequence.Join(incomingRect.DOLocalRotate(imageHomeRotation.eulerAngles, duration).SetEase(GetImageRotationCurve()));
        sequence.Join(incomingRect.DOScale(imageHomeScale, duration).SetEase(Ease.InOutSine));
        sequence.Join(characterImage.DOFade(imageHomeColor.a, duration).SetEase(Ease.InOutSine));
    }

    private Vector3 GetTurnCompressedScale()
    {
        float scaleXMultiplier = Mathf.Max(0f, imageTurnScaleXMultiplier);
        float scaleYMultiplier = Mathf.Max(0f, imageTurnScaleYMultiplier);
        return new Vector3(imageHomeScale.x * scaleXMultiplier, imageHomeScale.y * scaleYMultiplier, imageHomeScale.z);
    }

    private float GetDuration(float duration)
    {
        return Mathf.Max(0f, duration);
    }

    private AnimationCurve GetImageRotationCurve()
    {
        if (imageRotationProgressCurve != null && imageRotationProgressCurve.length > 0)
            return imageRotationProgressCurve;

        return AnimationCurve.Linear(0f, 0f, 1f, 1f);
    }

    private void DestroyPreviewClone(Component clone)
    {
        if (clone == null)
            return;

        Destroy(clone.gameObject);
    }

    private void RestoreTextHomeTransform(
        TMP_Text text,
        Vector2 homePosition,
        Vector3 homeScale,
        Quaternion homeRotation,
        Color homeColor)
    {
        if (text == null)
            return;

        RectTransform rect = text.rectTransform;
        rect.anchoredPosition = homePosition;
        rect.localScale = homeScale;
        rect.localRotation = homeRotation;
        text.color = homeColor;
    }
}
