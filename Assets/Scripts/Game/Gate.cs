using System;
using DG.Tweening;
using UnityEngine;

public class Gate : MonoBehaviour
{
    public GameObject levelFrom;

    public GameObject levelTo;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController pc =  other.GetComponent<PlayerController>();
            pc.controlsEnabled = false;
            pc.transform.SetParent(null, true);

            pc.GetWeapon().ResetSources();
            
            pc.transform.DOScale(0, 0.15f);
            levelFrom.transform.position += Vector3.forward * 30;
            levelFrom.transform.DOScale(1.25f, 0.5f).OnComplete(() =>
            {
                levelFrom.transform.DOScale(0, 1f).OnComplete(() =>
                {
                    levelFrom.gameObject.SetActive(false);
                });
            });

            levelTo.SetActive(true);
            levelTo.transform.localScale = Vector3.one * 0.01f;
            levelTo.transform.DOScale(1f, 0.5f).OnComplete(() =>
            {
                Transform entranceGate = levelTo.transform.Find("EntranceGate");
                if (entranceGate)
                {
                    Vector3 pos = entranceGate.position;
                    pos.z = 0;
                    pc.transform.position = pos;
                    pc.transform.DOScale(1f, 0.25f).OnComplete(() =>
                    {
                        pc.controlsEnabled = true;
                        entranceGate.DOScale(0, 0.5f).SetDelay(0.4f);

                    });    
                }
                else
                {
                    Debug.LogError("Entrance gate not found");
                }
                
            });


        }
    }
}
