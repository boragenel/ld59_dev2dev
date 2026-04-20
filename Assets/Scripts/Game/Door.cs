using DG.Tweening;
using UnityEngine;

public class Door : MonoBehaviour {
    [SerializeField]
    private Transform box1;
    [SerializeField]
    private Transform box2;
    [SerializeField]
    private SignalMeshPointReceiver signalReceiver;
    [SerializeField]
    [Min(1)]
    private int openWhenStrengthAtLeast = 1;
    [SerializeField]
    private float openSlideDistance = 0.5f;
    [SerializeField]
    private float openDuration = 0.35f;

    public bool isOpen;

    private Vector3 box1ClosedLocal;
    private Vector3 box2ClosedLocal;
    private bool cached;
    private bool lastOpen;
    private Tweener tween1;
    private Tweener tween2;
    private float noSoundTimer = 2f;
    
    private void Awake() {
        CacheClosedPositions();
    }

    private void OnEnable() {
        if (!cached) {
            CacheClosedPositions();
        }
        if (signalReceiver != null) {
            signalReceiver.OnSignalStrengthChanged += OnSignalStrengthChanged;
            isOpen = signalReceiver.SignalStrength >= openWhenStrengthAtLeast;
        }
        if (cached && box1 != null && box2 != null) {
            lastOpen = !isOpen;
            TweenToCurrentState();
        }
    }

    private void OnDisable() {
        if (signalReceiver != null) {
            signalReceiver.OnSignalStrengthChanged -= OnSignalStrengthChanged;
        }
    }

    private void OnSignalStrengthChanged(int strength) {
        isOpen = strength >= openWhenStrengthAtLeast;
    }

    private void Update() {
        if (!cached || box1 == null || box2 == null) {
            return;
        }
        if (isOpen != lastOpen) {
            TweenToCurrentState();
        }
        noSoundTimer -= Time.deltaTime;
    }

    private void OnDestroy() {
        tween1?.Kill();
        tween2?.Kill();
    }

    private void CacheClosedPositions() {
        if (box1 == null || box2 == null) {
            return;
        }
        box1ClosedLocal = box1.localPosition;
        box2ClosedLocal = box2.localPosition;
        cached = true;
    }

    private void TweenToCurrentState() {
        float d = openSlideDistance;
        Vector3 t1 = isOpen ? box1ClosedLocal + new Vector3(d, 0f, 0f) : box1ClosedLocal;
        Vector3 t2 = isOpen ? box2ClosedLocal + new Vector3(-d, 0f, 0f) : box2ClosedLocal;
        float dur = Mathf.Max(0.0001f, openDuration);
        tween1?.Kill();
        tween2?.Kill();
        tween1 = box1.DOLocalMove(t1, dur).SetEase(Ease.InOutQuad).SetLink(gameObject);
        tween2 = box2.DOLocalMove(t2, dur).SetEase(Ease.InOutQuad).SetLink(gameObject);
        lastOpen = isOpen;

        if (noSoundTimer > 0)
            return;
        if (lastOpen)
        {
            SoundManager.Instance.PlayOneShot(SoundType.DOORS_OPEN,0.4f,Random.Range(0.9f,1.1f));
        }
        else
        {
            SoundManager.Instance.PlayOneShot(SoundType.DOOTS_CLOSED,0.4f,Random.Range(0.9f,1.1f));
        }
    }
}
