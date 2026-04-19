using UnityEngine;

[DefaultExecutionOrder(101)]
public class RotatingEnemy : MonoBehaviour {
    [SerializeField]
    private Transform pivot;
    [SerializeField]
    private float rotationSpeedDegreesPerSecond = 90f;

    private SignalMeshPointReceiver signalReceiver;
    private bool hasReceivedSignal;

    private void Awake() {
        signalReceiver = GetComponent<SignalMeshPointReceiver>();
    }

    private void LateUpdate() {
        if (signalReceiver != null && signalReceiver.SignalStrength > 0) {
            hasReceivedSignal = true;
        }
        if (pivot == null || !hasReceivedSignal) {
            return;
        }
        transform.RotateAround(pivot.position, Vector3.forward, rotationSpeedDegreesPerSecond * Time.deltaTime);
    }
}
