using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class BuildManager : MonoBehaviour {
    public static BuildManager Instance { get; private set; }
    [SerializeField]
    private bool buildModeEnabled = true;
    [SerializeField]
    private LayerMask pickableLayers;
    [SerializeField]
    private float raycastMaxDistance = 200f;
    [SerializeField]
    private float planeZ;
    [SerializeField]
    private float rotateScrollDegrees = 12f;
    [SerializeField]
    private float rotateKeyDegreesPerSecond = 120f;

    private Transform carriedPiece;
    private Vector3 storedWorldPosition;
    private Quaternion storedWorldRotation;
    private Plane placementPlane;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        placementPlane = new Plane(Vector3.forward, new Vector3(0f, 0f, planeZ));
    }

    private void OnDestroy() {
        if (Instance == this) {
            Instance = null;
        }
    }

    private void Update() {
        if (!buildModeEnabled) {
            return;
        }
        Camera cam = Camera.main;
        if (cam == null || Mouse.current == null) {
            return;
        }
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && carriedPiece != null) {
            CancelCarry();
            return;
        }
        if (carriedPiece == null) {
            TryPick(cam);
        } else {
            DragCarried(cam);
            RotateCarried();
            if (Mouse.current.rightButton.wasPressedThisFrame) {
                CancelCarry();
                return;
            }
            if (Mouse.current.leftButton.wasPressedThisFrame) {
                PlaceCarried();
            }
        }
    }

    private void TryPick(Camera cam) {
        if (!Mouse.current.leftButton.wasPressedThisFrame) {
            return;
        }
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, raycastMaxDistance, pickableLayers, QueryTriggerInteraction.Collide)) {
            return;
        }
        BuildPiece piece = hit.collider.GetComponentInParent<BuildPiece>();
        if (piece == null || piece.IsLocked) {
            return;
        }
        carriedPiece = piece.transform;
        storedWorldPosition = carriedPiece.position;
        storedWorldRotation = carriedPiece.rotation;
    }

    private void DragCarried(Camera cam) {
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (placementPlane.Raycast(ray, out float enter)) {
            Vector3 p = ray.GetPoint(enter);
            p.z = planeZ;
            carriedPiece.position = p;
        }
    }

    private void RotateCarried() {
        float deltaZ = 0f;
        Vector2 scroll = Mouse.current.scroll.ReadValue();
        if (Mathf.Abs(scroll.y) > 0.01f) {
            deltaZ += Mathf.Sign(scroll.y) * rotateScrollDegrees;
        }
        if (Keyboard.current != null) {
            if (Keyboard.current.aKey.isPressed) {
                deltaZ += rotateKeyDegreesPerSecond * Time.deltaTime;
            }
            if (Keyboard.current.dKey.isPressed) {
                deltaZ -= rotateKeyDegreesPerSecond * Time.deltaTime;
            }
        }
        if (Mathf.Abs(deltaZ) > 0f) {
            carriedPiece.Rotate(0f, 0f, deltaZ, Space.World);
        }
    }

    private void PlaceCarried() {
        carriedPiece = null;
    }

    private void CancelCarry() {
        if (carriedPiece == null) {
            return;
        }
        carriedPiece.position = storedWorldPosition;
        carriedPiece.rotation = storedWorldRotation;
        carriedPiece = null;
    }

    public void SetBuildModeEnabled(bool enabled) {
        buildModeEnabled = enabled;
        if (!enabled && carriedPiece != null) {
            CancelCarry();
        }
    }
}
