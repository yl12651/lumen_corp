using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class ArkSquareLineEffect : MonoBehaviour
{
    private const string GeneratedRootName = "__ArkSquareLineEffectGenerated";

    [Header("Sprite")]
    [SerializeField] private Sprite squareSprite;
    [SerializeField] private Color squareColor = Color.white;
    [SerializeField] private bool preserveAspect = true;

    [Header("Line Layout")]
    [SerializeField] private int squaresPerLine = 4;
    [SerializeField] private Vector2 squareSize = new Vector2(64f, 64f);
    [SerializeField] private float spacing = 96f;
    [SerializeField] private Vector2 localStartOffset = Vector2.zero;

    [Header("Motion")]
    [SerializeField] private float speed = 80f;
    [SerializeField] private float angleDegrees = 40f;
    [SerializeField] private bool useUnscaledTime = true;

    private readonly List<RectTransform> lineRects = new List<RectTransform>();
    private RectTransform generatedRoot;
    private float loopLength;
    private Vector2 direction;

    private void OnEnable()
    {
        Rebuild();
    }

    private void Update()
    {
        if (lineRects.Count != 2)
            return;

        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        Vector2 movement = direction * speed * deltaTime;

        for (int i = 0; i < lineRects.Count; i++)
        {
            RectTransform lineRect = lineRects[i];
            if (lineRect == null)
                continue;

            lineRect.anchoredPosition += movement;

            float distanceFromStart = Vector2.Dot(lineRect.anchoredPosition - localStartOffset, direction);
            if (distanceFromStart >= loopLength)
            {
                lineRect.anchoredPosition -= direction * loopLength * 2f;
            }
        }
    }

    [ContextMenu("Rebuild Lines")]
    public void Rebuild()
    {
        squaresPerLine = Mathf.Max(1, squaresPerLine);
        spacing = Mathf.Max(1f, spacing);

        direction = DirectionFromAngle(angleDegrees);
        loopLength = spacing * squaresPerLine;

        ClearGeneratedChildren();
        lineRects.Clear();

        generatedRoot = CreateGeneratedRoot();

        lineRects.Add(CreateLine("Ark Square Line A", localStartOffset));
        lineRects.Add(CreateLine("Ark Square Line B", localStartOffset - direction * loopLength));
    }

    private RectTransform CreateLine(string lineName, Vector2 anchoredPosition)
    {
        GameObject lineObject = new GameObject(lineName, typeof(RectTransform));
        lineObject.transform.SetParent(generatedRoot, false);

        RectTransform lineRect = lineObject.GetComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0.5f, 0.5f);
        lineRect.anchorMax = new Vector2(0.5f, 0.5f);
        lineRect.pivot = new Vector2(0.5f, 0.5f);
        lineRect.anchoredPosition = anchoredPosition;
        lineRect.localRotation = Quaternion.Euler(0f, 0f, angleDegrees);
        lineRect.sizeDelta = new Vector2(loopLength, squareSize.y);

        for (int i = 0; i < squaresPerLine; i++)
        {
            CreateSquare(lineRect, i);
        }

        return lineRect;
    }

    private void CreateSquare(RectTransform lineRect, int index)
    {
        GameObject squareObject = new GameObject("Ark Square " + index, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        squareObject.transform.SetParent(lineRect, false);

        RectTransform squareRect = squareObject.GetComponent<RectTransform>();
        squareRect.anchorMin = new Vector2(0.5f, 0.5f);
        squareRect.anchorMax = new Vector2(0.5f, 0.5f);
        squareRect.pivot = new Vector2(0.5f, 0.5f);
        squareRect.sizeDelta = squareSize;
        squareRect.anchoredPosition = new Vector2(index * spacing, 0f);

        Image image = squareObject.GetComponent<Image>();
        image.sprite = squareSprite;
        image.color = squareColor;
        image.preserveAspect = preserveAspect;
        image.raycastTarget = false;
    }

    private void ClearGeneratedChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name != GeneratedRootName)
                continue;

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        generatedRoot = null;
    }

    private RectTransform CreateGeneratedRoot()
    {
        GameObject rootObject = new GameObject(GeneratedRootName, typeof(RectTransform));
        rootObject.transform.SetParent(transform, false);

        RectTransform rootRect = rootObject.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = Vector2.zero;

        return rootRect;
    }

    private static Vector2 DirectionFromAngle(float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)).normalized;
    }
}
