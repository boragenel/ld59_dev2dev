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
        SoundManager.Instance.PlayOneShot(SoundType.EXIT_OPEN, 0.7f);
    }

    // Update is called once per frame
    void Update()
    {

    }

    private bool FUCK;
    private void OnTriggerEnter(Collider other)
    {
        if (isLocked) return;
        if (other.CompareTag("Player"))
        {
            if (FUCK) return;

            FUCK = true;
            SoundManager.Instance.PlayOneShot(SoundType.TRANSITION, 0.7f);
            GameManager.OnLevelClear?.Invoke();
            GameManager.Instance.isTransitioning = true;
            GameManager.Instance.PlaySignalLosFadeOut();
            //GameManager.Instance.ChangeGameState(GamePhase.BUILDING);

            GameObject levelFrom = GameManager.Instance.currentLevel.gameObject;
            GameObject levelTo = GameManager.Instance.GetNextLevelPrefab();
            if (levelTo != null)
            {
                levelTo = Instantiate(GameManager.Instance.GetNextLevelPrefab());
                GameManager.Instance.currentLevel = levelTo.GetComponent<LevelBase>();
            }

            PlayerController pc = other.GetComponentInParent<PlayerController>();
            pc.enabled = false;
            pc.transform.SetParent(null);
            pc.collision.SetActive(false);
            pc.controlsEnabled = false;
            pc.transform.SetParent(null, true);
            pc.PlayerSignalReceiver.ResetSources();
            pc.transform.DOScale(0, 0.15f);
            levelFrom.transform.position += Vector3.forward * 100;
            GameManager.Instance.transitionDepths.DOFade(1f, 0.25f).SetDelay(0.25f).OnComplete(() =>
            {
                foreach (BuildPiece bp in levelFrom.GetComponentsInChildren<BuildPiece>())
                {
                    bp.gameObject.SetActive(false);
                }
            });

            levelFrom.transform.DOScale(10f, 1f).OnComplete(() =>
            {
                levelFrom.gameObject.SetActive(false);
                Destroy(levelFrom);
                GameManager.Instance.transitionDepths.DOFade(0f, 0.5f);
            });
            /*
            levelFrom.transform.DOScale(0, 1f).OnComplete(() =>
            {
                levelFrom.gameObject.SetActive(false);
                Destroy(levelFrom);
            });
            */
            if (!levelTo)
            {
                GameManager.Instance.ending.SetActive(true);
                return;
            }
            levelTo.SetActive(true);
            levelTo.transform.localScale = Vector3.one * 0.01f;
            levelTo.transform.DOScale(1f, 1f).OnComplete(() =>
            {
                Camera.main.transform.DOShakePosition(0.25f, Vector3.one * 0.25f, 20);
                GameManager.Instance.SetPlayerToStartPos();
                pc.enabled = true;
            });


        }
    }
}
