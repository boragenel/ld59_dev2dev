using UnityEngine;

public class SignalSource : MonoBehaviour {
    [SerializeField]
    private Transform signalRendererHolder;

    [Header("Setup")]
    public float maxSignalRadius;
    public bool isUnblockableSignal;
    public float signalSourceSize = 0.2f;

    [SerializeField]
    private LayerMask signalSearchMask;
    [SerializeField]
    private LayerMask signalBlockMask;

    public float signalLossPerUnitDistance = 0.1f;
    public float minDistanceToReceiveMaxSignal = 1.25f;

    private void Awake() {
        GameManager.OnLevelClear += OnLevelClear;
    }

    private void OnLevelClear() {
        GameManager.OnLevelClear -= OnLevelClear;
        Destroy(this);
    }
}
