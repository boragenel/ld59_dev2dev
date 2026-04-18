using System;
using DG.Tweening;
using JetBrains.Annotations;
using UnityEngine;

public class Gate : MonoBehaviour
{


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
            GameManager.OnLevelComplete?.Invoke();

            GameObject levelFrom = GameManager.Instance.currentLevel.gameObject;
            GameObject levelTo = Instantiate(GameManager.Instance.GetNextLevelPrefab());

            GameManager.Instance.currentLevel = levelTo.GetComponent<LevelBase>();
            GameManager.Instance.isTransitioning = true;

            PlayerController pc = other.GetComponentInParent<PlayerController>();
            pc.collision.SetActive(false);
            pc.controlsEnabled = false;
            pc.transform.SetParent(null, true);
            pc.PlayerSignalReceiver.ResetSources();
            pc.transform.DOScale(0, 0.15f);
            levelFrom.transform.position += Vector3.forward * 30;
            levelFrom.transform.DOScale(1.25f, 0.5f).OnComplete(() =>
            {
                levelFrom.transform.DOScale(0, 1f).OnComplete(() =>
                {
                    levelFrom.gameObject.SetActive(false);
                    Destroy(levelFrom);
                });
            });

            levelTo.SetActive(true);
            levelTo.transform.localScale = Vector3.one * 0.01f;
            levelTo.transform.DOScale(1f, 0.5f).OnComplete(() =>
            {
                Transform entranceGate = GameManager.Instance.currentLevel.levelEntrance.transform;
                if (entranceGate)
                {
                    Vector3 pos = entranceGate.position;
                    pos.z = 0;
                    pc.transform.position = pos;
                    pc.transform.DOScale(1f, 0.25f).OnComplete(() =>
                    {
                        GameManager.Instance.isTransitioning = false;
                        pc.controlsEnabled = true;
                        pc.collision.SetActive(true);
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
