using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CafeCharacterPairHoverGroup : MonoBehaviour
{
    [SerializeField] private List<CafeCharacterSpriteHoverHighlight> hoverTargets =
        new List<CafeCharacterSpriteHoverHighlight>();

    private readonly HashSet<CafeCharacterSpriteHoverHighlight> activeHoverTargets =
        new HashSet<CafeCharacterSpriteHoverHighlight>();

    private Coroutine disableCoroutine;

    private void Awake()
    {
        if (hoverTargets.Count == 0)
            GetComponentsInChildren(true, hoverTargets);

        SetGroupHighlighted(false);
    }

    public void NotifyHoverEntered(CafeCharacterSpriteHoverHighlight hoverTarget)
    {
        if (disableCoroutine != null)
        {
            StopCoroutine(disableCoroutine);
            disableCoroutine = null;
        }

        if (hoverTarget != null)
            activeHoverTargets.Add(hoverTarget);

        SetGroupHighlighted(true);
    }

    public void NotifyHoverExited(CafeCharacterSpriteHoverHighlight hoverTarget)
    {
        if (hoverTarget != null)
            activeHoverTargets.Remove(hoverTarget);

        if (activeHoverTargets.Count == 0 && disableCoroutine == null)
            disableCoroutine = StartCoroutine(DisableAfterFrame());
    }

    public void SetGroupHighlighted(bool highlighted)
    {
        foreach (CafeCharacterSpriteHoverHighlight hoverTarget in hoverTargets)
        {
            if (hoverTarget != null)
                hoverTarget.SetHighlighted(highlighted);
        }
    }

    private IEnumerator DisableAfterFrame()
    {
        yield return null;

        if (activeHoverTargets.Count == 0)
            SetGroupHighlighted(false);

        disableCoroutine = null;
    }
}
