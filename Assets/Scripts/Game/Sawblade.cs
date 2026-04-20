using DG.Tweening;
using UnityEngine;
using UnityStandardAssets.Utility;

public class Sawblade : MonoBehaviour
{
    private SignalMeshPointReceiver signalReceiver;
    private Tween backNForthTween;
    public AutoMoveAndRotate rotator;
    public bool isOn = false;

    public float movimentValue = 1.5f;

    public float moveDuration = 0;

    public float startDelay = 0f;
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
        bool hasSignalNow = signalReceiver != null && signalReceiver.SignalStrength > 0f;
        if (rotator.enabled && hasSignalNow)
        {
            backNForthTween.Pause();
        }
        else if (!rotator.enabled && !hasSignalNow)
        {
            backNForthTween.Play();
        }
        rotator.enabled = !hasSignalNow;
        isOn = !hasSignalNow;


    }
}
