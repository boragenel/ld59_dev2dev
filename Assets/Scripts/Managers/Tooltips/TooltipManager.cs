using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TooltipManager : MonoBehaviour
{
    private RectTransform rectTransform;
    public RectTransform tooltipContainer;
    public TextMeshProUGUI tooltipT;
    [SerializeField]
    private Image arrow;
    
    private GameObject currentInstigator = null;
    
    private CanvasScaler canvasScaler;
    public bool tooltipFollowsMouse = true;

    public static UnityAction<TooltipAsker> OnTooltipAsked;
    public static UnityAction<TooltipAsker> OnTooltipReleased;
    private Vector2 localMousePos;
    
    // Start is called before the first frame update
    void Start()
    {
        OnTooltipAsked += NewTooltipAsked;
        OnTooltipReleased += TooltipReleased;
        rectTransform = GetComponent<RectTransform>();
        canvasScaler = GameManager.Instance.uiManager.GetComponent<CanvasScaler>();
    }

    private void OnDestroy()
    {
        OnTooltipAsked -= NewTooltipAsked;
        OnTooltipReleased -= TooltipReleased;
    }

    
    void TooltipReleased(TooltipAsker asker)
    {
        HideTooltip(asker.gameObject,asker.forceHideOnExit);
    }
    void NewTooltipAsked(TooltipAsker asker)
    {
        if (asker.worldToScreen)
        {
            ShowTooltipOnWorldObject(asker.tooltipContent,asker.gameObject);
        }
        else if(asker.useAnchoredPosition)
        {
            ShowTooltip(asker.tooltipContent, (asker.transform as RectTransform).anchoredPosition, asker.gameObject);
        }
        else
        {
            ShowTooltip(asker.tooltipContent, asker.transform.position, asker.gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (tooltipFollowsMouse && currentInstigator)
        {
            tooltipContainer.anchoredPosition = transform.InverseTransformPoint(Mouse.current.position.ReadValue());
            ReadjustTooltipPosition();
        }
    }

    public void ShowTooltip(string tooltipText,Vector2 inPos,GameObject instigator)
    {
        currentInstigator = instigator;
        tooltipContainer.gameObject.SetActive(true);
        tooltipT.text = tooltipText;
        tooltipContainer.anchoredPosition = inPos;
        PlayShowAnimation();
        StartCoroutine(DirtyTooltipContainer());

    }
    
    public void ShowTooltip(string tooltipText,Vector3 inPos,GameObject instigator)
    {
        currentInstigator = instigator;
        tooltipT.text = tooltipText;
        tooltipContainer.transform.position = inPos;
        tooltipContainer.gameObject.SetActive(true);
        PlayShowAnimation();
        StartCoroutine(DirtyTooltipContainer());
    }
    
    public void ShowTooltipOnWorldObject(string tooltipText,GameObject worldObject)
    {
        currentInstigator = worldObject;
        tooltipContainer.transform.position = Camera.main.WorldToScreenPoint(worldObject.transform.position);
        tooltipT.text = tooltipText;
        tooltipContainer.gameObject.SetActive(true);
        PlayShowAnimation();
        StartCoroutine(DirtyTooltipContainer());
    }

    public void PlayShowAnimation()
    {
        tooltipContainer.transform.DOKill(true);
        tooltipContainer.gameObject.SetActive(true);
        tooltipContainer.transform.localScale = Vector3.zero;
        tooltipContainer.transform.DOScale(Vector3.one, 0.1f).SetEase(Ease.OutBack).SetUpdate(true);
    }
    
    public void PlayHideAnimation()
    {
        tooltipContainer.transform.DOKill(true);
        tooltipContainer.transform.localScale = Vector3.one;
        tooltipContainer.transform.DOScale(Vector3.zero, 0.1f).SetEase(Ease.InBack).SetEase(Ease.OutBack).SetUpdate(true).OnComplete(() =>
        {
            tooltipContainer.gameObject.SetActive(false);
        });
    }

    public void HideTooltip(GameObject instigator,bool forced=false)
    {
        if (currentInstigator == instigator || forced)
        {
            PlayHideAnimation();
            currentInstigator = null;
        }
    }

    public IEnumerator DirtyTooltipContainer()
    {
        ReadjustTooltipPosition();
        tooltipContainer.GetComponent<HorizontalOrVerticalLayoutGroup>().SetLayoutHorizontal();
        yield return new WaitForSeconds(0.01f);
        tooltipContainer.GetComponent<HorizontalOrVerticalLayoutGroup>().SetLayoutVertical();
        yield return new WaitForSeconds(0.01f);
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipContainer);
        yield return new WaitForSeconds(0.01f);
        Canvas.ForceUpdateCanvases();
        yield return new WaitForSeconds(0.01f);
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipContainer);
        yield return new WaitForSeconds(0.01f);
    }
    
    public void ReadjustTooltipPosition()
    {
        Vector2 maxPos = tooltipContainer.anchoredPosition + new Vector2(tooltipContainer.sizeDelta.x,tooltipContainer.sizeDelta.y);
        if (arrow)
        {
            arrow.gameObject.SetActive(true);    
        }
        if (maxPos.x > canvasScaler.referenceResolution.x*0.5f)
        {
            tooltipContainer.anchoredPosition = new Vector2((canvasScaler.referenceResolution.x*0.5f) - tooltipContainer.sizeDelta.x, tooltipContainer.anchoredPosition.y);
            if (arrow)
            {
                arrow.gameObject.SetActive(false);    
            }
        }
        else if (maxPos.x < -canvasScaler.referenceResolution.x*0.5f)
        {
            tooltipContainer.anchoredPosition = new Vector2((-canvasScaler.referenceResolution.x*0.5f) + tooltipContainer.sizeDelta.x, tooltipContainer.anchoredPosition.y);
            if (arrow)
            {
                arrow.gameObject.SetActive(false);    
            }
        }
        

        if (maxPos.y  < -canvasScaler.referenceResolution.y*0.5f)
        {
            tooltipContainer.anchoredPosition = new Vector2(tooltipContainer.anchoredPosition.x, (-canvasScaler.referenceResolution.y*0.5f)+tooltipContainer.sizeDelta.y);
            if (arrow)
            {
                arrow.gameObject.SetActive(false);    
            }
        }
        
        if (maxPos.y  > canvasScaler.referenceResolution.y*0.5f)
        {
            tooltipContainer.anchoredPosition = new Vector2(tooltipContainer.anchoredPosition.x, (canvasScaler.referenceResolution.y*0.5f)-tooltipContainer.sizeDelta.y);
            if (arrow)
            {
                arrow.gameObject.SetActive(false);    
            }
        }
        
    }
    
}
