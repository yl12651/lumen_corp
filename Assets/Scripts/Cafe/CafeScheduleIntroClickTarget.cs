using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class CafeScheduleIntroClickTarget : MonoBehaviour, IPointerClickHandler
{
    private Action clicked;

    public void Setup(Action onClicked)
    {
        clicked = onClicked;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        clicked?.Invoke();
    }
}
