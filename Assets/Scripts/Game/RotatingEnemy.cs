using UnityEngine;

[DefaultExecutionOrder(101)]
public class RotatingEnemy : MonoBehaviour {
    [SerializeField]
    private Transform pivot;
    [SerializeField]
    private float rotationSpeedDegreesPerSecond = 90f;
    [SerializeField]
    private GameObject visualActive;
    [SerializeField]
    private GameObject visualInactive;

    private SignalMeshPointReceiver signalReceiver;
    private GamePhase lastTrackedPhase = GamePhase.NONE;
    private bool gameplaySignalLatched;

    private void Awake() {
        signalReceiver = GetComponent<SignalMeshPointReceiver>();
    }

    private void LateUpdate() {
        GamePhase phase = GameManager.Instance != null
            ? GameManager.Instance.GetCurrentGamePhase()
            : GamePhase.NONE;

        if (phase == GamePhase.GAMEPLAY && lastTrackedPhase != GamePhase.GAMEPLAY) {
            gameplaySignalLatched = false;
        }
        lastTrackedPhase = phase;

        bool hasSignalNow = signalReceiver != null && signalReceiver.SignalStrength > 0f;
        if (phase == GamePhase.GAMEPLAY && hasSignalNow) {
            gameplaySignalLatched = true;
        }

        bool showActive = false;
        if (GameManager.Instance != null) {
            if (phase == GamePhase.BUILDING) {
                showActive = hasSignalNow;
            } else if (phase == GamePhase.GAMEPLAY) {
                showActive = gameplaySignalLatched;
            }
        }
        ApplyVisuals(showActive);

        if (phase == GamePhase.BUILDING) {
            return;
        }
        if (phase != GamePhase.GAMEPLAY || pivot == null || !gameplaySignalLatched) {
            return;
        }
        transform.RotateAround(pivot.position, Vector3.forward, rotationSpeedDegreesPerSecond * Time.deltaTime);
    }

    private void ApplyVisuals(bool active) {
        if (visualActive != null) {
            visualActive.SetActive(active);
        }
        if (visualInactive != null) {
            visualInactive.SetActive(!active);
        }
    }
}
