using Live2D.Cubism.Framework.Motion;
using Live2D.Cubism.Framework.MotionFade;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CubismFadeController))]
[RequireComponent(typeof(CubismMotionController))]
public class Live2DMotionTrigger : MonoBehaviour
{
    [Header("Cubism")]
    [SerializeField] private CubismFadeMotionList fadeMotionList;

    [Header("Motions")]
    [SerializeField] private AnimationClip idleMotion;
    [SerializeField] private AnimationClip trigger1Motion;
    [SerializeField] private AnimationClip trigger3Motion;

    [Header("Playback")]
    [SerializeField] private bool playIdleOnStart = true;
    [SerializeField] private bool returnToIdleAfterTrigger = true;
    [SerializeField] private bool clearCurrentMotionBeforeTrigger = false;

    private CubismMotionController motionController;
    private CubismFadeController fadeController;
    private Coroutine returnToIdleRoutine;
    private bool nextStartedMotionReturnsToIdle;
    private int? activeTriggerMotionInstanceId;

    private void Awake()
    {
        motionController = GetComponent<CubismMotionController>();
        fadeController = GetComponent<CubismFadeController>();

        if (fadeMotionList != null)
            fadeController.CubismFadeMotionList = fadeMotionList;
    }

    private void OnEnable()
    {
        motionController.AnimationBeginHandler += OnAnimationBegan;
        motionController.AnimationEndHandler += OnAnimationEnded;
    }

    private void OnDisable()
    {
        if (motionController != null)
        {
            motionController.AnimationBeginHandler -= OnAnimationBegan;
            motionController.AnimationEndHandler -= OnAnimationEnded;
        }
    }

    private void Start()
    {
        if (playIdleOnStart)
            PlayIdleAsDefault();
    }

    public void PlayIdle()
    {
        Play(idleMotion, true, CubismMotionPriority.PriorityForce, clearCurrentMotionBeforeTrigger, true, false);
    }

    public void PlayTrigger1()
    {
        Play(trigger1Motion, false, CubismMotionPriority.PriorityForce, clearCurrentMotionBeforeTrigger, true, true);
    }

    public void PlayTrigger3()
    {
        Play(trigger3Motion, false, CubismMotionPriority.PriorityForce, clearCurrentMotionBeforeTrigger, true, true);
    }

    public void PlayTriggerByName(string triggerName)
    {
        switch (triggerName)
        {
            case "Trigger_1":
            case "1":
                PlayTrigger1();
                break;
            case "Trigger_3":
            case "3":
                PlayTrigger3();
                break;
            case "Idle":
                PlayIdle();
                break;
            default:
                Debug.LogWarning($"Unknown Live2D trigger: {triggerName}", this);
                break;
        }
    }

    private void Play(
        AnimationClip clip,
        bool loop,
        int priority,
        bool clearCurrentMotion,
        bool cancelPendingIdle,
        bool returnToIdleWhenFinished)
    {
        if (clip == null)
        {
            Debug.LogWarning("Live2D motion clip is not assigned.", this);
            return;
        }

        if (cancelPendingIdle && returnToIdleRoutine != null)
        {
            StopCoroutine(returnToIdleRoutine);
            returnToIdleRoutine = null;
        }

        if (clearCurrentMotion)
            motionController.StopAllAnimation();

        nextStartedMotionReturnsToIdle = returnToIdleWhenFinished;
        motionController.PlayAnimation(clip, isLoop: loop, priority: priority);
        nextStartedMotionReturnsToIdle = false;
    }

    private void OnAnimationBegan(int instanceId)
    {
        if (nextStartedMotionReturnsToIdle)
            activeTriggerMotionInstanceId = instanceId;
    }

    private void OnAnimationEnded(int instanceId)
    {
        if (!returnToIdleAfterTrigger || activeTriggerMotionInstanceId != instanceId)
            return;

        activeTriggerMotionInstanceId = null;

        if (returnToIdleRoutine != null)
            StopCoroutine(returnToIdleRoutine);

        returnToIdleRoutine = StartCoroutine(ReturnToIdleAfterCurrentMotion());
    }

    private void PlayIdleAsDefault()
    {
        Play(idleMotion, true, CubismMotionPriority.PriorityIdle, false, true, false);
    }

    private IEnumerator ReturnToIdleAfterCurrentMotion()
    {
        yield return null;

        Play(idleMotion, true, CubismMotionPriority.PriorityForce, clearCurrentMotionBeforeTrigger, false, false);
        returnToIdleRoutine = null;
    }
}
