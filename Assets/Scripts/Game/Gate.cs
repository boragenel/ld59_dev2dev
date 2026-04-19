using System;
using DG.Tweening;
using JetBrains.Annotations;
using UnityEngine;

public class Gate : MonoBehaviour
{
    public GameObject particle;

    bool isLocked;
    public void LockGate()
    {
        isLocked = true;
        particle.SetActive(false);
    }

    public void UnlockGate()
    {
        isLocked = false;
        particle.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        if (isLocked) return;
        if (other.CompareTag("Player"))
        {
            GameManager.OnLevelClear?.Invoke();
            GameManager.Instance.isTransitioning = true;

            GameObject levelFrom = GameManager.Instance.currentLevel.gameObject;
            GameObject levelTo = Instantiate(GameManager.Instance.GetNextLevelPrefab());

            GameManager.Instance.currentLevel = levelTo.GetComponent<LevelBase>();

            if (!GameManager.Instance.currentLevel.DontRotateOnInit)
                GameManager.Instance.currentLevel.transform.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));

            PlayerController pc = other.GetComponentInParent<PlayerController>();
            pc.collision.SetActive(false);
            pc.controlsEnabled = false;
            pc.transform.SetParent(null, true);
            pc.PlayerSignalReceiver.ResetSources();
            pc.transform.DOScale(0, 0.15f);
            levelFrom.transform.position += Vector3.forward * 30;
            //levelFrom.transform.DOScale(1.25f, 0.5f).OnComplete(() =>
            //{
            //    levelFrom.transform.DOScale(0, 1f).OnComplete(() =>
            //    {
            //        levelFrom.gameObject.SetActive(false);
            //        Destroy(levelFrom);
            //    });
            //});
            levelFrom.transform.DOScale(0, 1f).OnComplete(() =>
            {
                levelFrom.gameObject.SetActive(false);
                Destroy(levelFrom);
            });

            levelTo.SetActive(true);
            levelTo.transform.localScale = Vector3.one * 0.01f;
            levelTo.transform.DOScale(1f, 0.5f).OnComplete(() =>
            {
                GameManager.Instance.SetPlayerToStartPos();
            });


        }
    }
}
