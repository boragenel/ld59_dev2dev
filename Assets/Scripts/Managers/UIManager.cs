using System;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("HUD Elements")] 
    public Button pauseButton;
    public Transform signalStickContainer;
    [Header("Popups")]
    public GameObject settings;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Weapon.OnReceptionChanged += UpdateReceptionVisuals;
    }

    private void OnDestroy()
    {
        Weapon.OnReceptionChanged -= UpdateReceptionVisuals;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateReceptionVisuals(float inReception)
    {
        DisableAllSignalSticks();
        if (inReception <= 0)
            return;
        
        for (int i = 0; i < Mathf.Clamp(Mathf.FloorToInt(signalStickContainer.childCount * inReception),1,8); i++)
        {
            signalStickContainer.GetChild(i).gameObject.SetActive(true);
        }
    }

    void DisableAllSignalSticks()
    {
        foreach (Transform stick in signalStickContainer)
        {
            stick.gameObject.SetActive(false);
        }
    }
    
}
