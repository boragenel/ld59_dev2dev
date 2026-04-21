using DG.Tweening;
using UnityEngine;
using UnityStandardAssets.Utility;

public class Sawblade : MonoBehaviour
{
    private SignalMeshPointReceiver signalReceiver;
    private Tween backNForthTween;
    public AutoMoveAndRotate rotator;
    public bool isOn = false;

    public bool REVERSED;

    public float movimentValue = 1.5f;

    public float moveDuration = 0;

    public float startDelay = 0f;

    public bool IsOn
    {
        get
        {
            return REVERSED || isOn;
        }
        set => isOn = value;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        signalReceiver = GetComponentInChildren<SignalMeshPointReceiver>();

        if (moveDuration > 0)
        {
            if (backNForthTween != null)
            {
                backNForthTween.Kill();
            }

            backNForthTween = rotator.transform.DOLocalMoveY(movimentValue, moveDuration).SetLoops(-1, LoopType.Yoyo)
                .SetDelay(startDelay);
        }
    }

    private void LateUpdate()
    {
        if (GameManager.Instance.isTransitioning) return;
        bool hasSignalNow = signalReceiver != null && signalReceiver.SignalStrength > 0f;
        hasSignalNow = REVERSED ? !hasSignalNow : hasSignalNow;

        // if (rotator.enabled && hasSignalNow)
        if (hasSignalNow)
        {
            backNForthTween.Pause();
        }
        else if (!hasSignalNow)
        {
            backNForthTween.Play();
        }
        //rotator.enabled = !hasSignalNow;
        rotator.enabled = true;
        IsOn = !hasSignalNow;
    }
}
