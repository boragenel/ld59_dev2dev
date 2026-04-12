using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipAsker : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
{
    [Multiline]
    public string tooltipContent = "Some Tooltip Text";
    public bool worldToScreen;
    public bool forceHideOnExit = false;
    public bool useAnchoredPosition = false;


    private void Start()
    {
        if (worldToScreen && GetComponent<Collider>() == null)
        {
            Debug.LogWarning(gameObject.name+" doesn't have a collider so worldToScreen tooltips won't work");
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        TooltipManager.OnTooltipAsked?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.OnTooltipReleased?.Invoke(this);
    }

    private void OnMouseEnter()
    {
        TooltipManager.OnTooltipAsked?.Invoke(this);
    }

    private void OnMouseExit()
    {
        TooltipManager.OnTooltipReleased?.Invoke(this);
    }
}
