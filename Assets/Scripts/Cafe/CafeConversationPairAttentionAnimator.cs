using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class CafeConversationPairAttentionAnimator : MonoBehaviour
{
    [SerializeField] private string pairKey;
    [SerializeField] private List<Transform> animatedTargets = new List<Transform>();
    [SerializeField] private float jumpHeight = 0.08f;
    [SerializeField] private float jumpDuration = 0.35f;
    [SerializeField] private float staggerDelay = 0.08f;
    [SerializeField] private Ease jumpEase = Ease.InOutSine;

    private readonly Dictionary<Transform, TransformSnapshot> defaultTransforms =
        new Dictionary<Transform, TransformSnapshot>();

    private readonly List<Tween> activeTweens = new List<Tween>();

    public string PairKey => pairKey;

    private void Awake()
    {
        if (animatedTargets.Count == 0)
            CollectSpriteTargets();

        CaptureDefaultTransforms();
    }

    private void OnDisable()
    {
        StopAttentionLoop(true);
    }

    private void OnDestroy()
    {
        StopAttentionLoop(true);
    }

    public bool MatchesPairKey(string candidatePairKey)
    {
        return !string.IsNullOrEmpty(pairKey) && pairKey == candidatePairKey;
    }

    public void StartAttentionLoop()
    {
        StopAttentionLoop(true);
        CaptureDefaultTransforms();

        for (int i = 0; i < animatedTargets.Count; i++)
        {
            Transform target = animatedTargets[i];

            if (target == null)
                continue;

            if (!defaultTransforms.TryGetValue(target, out TransformSnapshot snapshot))
                continue;

            target.localPosition = snapshot.LocalPosition;
            target.localRotation = snapshot.LocalRotation;
            target.localScale = snapshot.LocalScale;

            Tween tween = target
                .DOLocalMoveY(snapshot.LocalPosition.y + jumpHeight, jumpDuration)
                .SetEase(jumpEase)
                .SetLoops(-1, LoopType.Yoyo)
                .SetDelay(i * staggerDelay);

            activeTweens.Add(tween);
        }
    }

    public void StopAttentionLoop(bool restoreDefaultTransform)
    {
        foreach (Tween tween in activeTweens)
        {
            if (tween != null && tween.IsActive())
                tween.Kill(false);
        }

        activeTweens.Clear();

        if (restoreDefaultTransform)
            RestoreDefaultTransforms();
    }

    private void CollectSpriteTargets()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != null && renderer.transform != null && !animatedTargets.Contains(renderer.transform))
                animatedTargets.Add(renderer.transform);
        }
    }

    private void CaptureDefaultTransforms()
    {
        foreach (Transform target in animatedTargets)
        {
            if (target == null)
                continue;

            defaultTransforms[target] = new TransformSnapshot(
                target.localPosition,
                target.localRotation,
                target.localScale
            );
        }
    }

    private void RestoreDefaultTransforms()
    {
        foreach (KeyValuePair<Transform, TransformSnapshot> pair in defaultTransforms)
        {
            Transform target = pair.Key;

            if (target == null)
                continue;

            TransformSnapshot snapshot = pair.Value;
            target.localPosition = snapshot.LocalPosition;
            target.localRotation = snapshot.LocalRotation;
            target.localScale = snapshot.LocalScale;
        }
    }

    private readonly struct TransformSnapshot
    {
        public readonly Vector3 LocalPosition;
        public readonly Quaternion LocalRotation;
        public readonly Vector3 LocalScale;

        public TransformSnapshot(Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
        {
            LocalPosition = localPosition;
            LocalRotation = localRotation;
            LocalScale = localScale;
        }
    }
}
