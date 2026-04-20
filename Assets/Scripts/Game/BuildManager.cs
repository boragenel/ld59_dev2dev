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

    public Transform carriedPiece;
    private Vector3 storedWorldPosition;
    private Quaternion storedWorldRotation;
    private Plane placementPlane;
    public BuildPiece hoveredPiece;
    private GameObject dragGhostInstance;
    private Vector3 carryLinkCursorOnPlane;


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
        /*
        if (!buildModeEnabled) {
            return;
        }
        */
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
            bool linkSightClear = HasClearLineOfSightTo(carryLinkCursorOnPlane);
            GameManager.Instance.player.UpdateBuldingLink(carryLinkCursorOnPlane, linkSightClear);
            RotateCarried();
            SyncDragGhostRotation();
            if (Mouse.current.rightButton.wasPressedThisFrame) {
                CancelCarry();
                return;
            }
            if (Mouse.current.leftButton.wasReleasedThisFrame) {
                PlaceCarried();
            }
        }
    }

    public bool HasClearLineOfSightTo(Vector3 worldPoint) {
        Transform playerTransform = GameManager.Instance.player.transform;
        Vector3 origin = playerTransform.position;
        origin.z = 0f;
        worldPoint.z = 0f;
        Vector3 delta = worldPoint - origin;
        float dist = delta.magnitude;
        if (dist < 1e-4f) {
            return true;
        }
        Vector3 dir = delta / dist;
        return !Physics.Raycast(origin, dir, out _, dist, LayerMask.GetMask("Wall"));
    }

    public bool HasClearPathToPlayer(Transform piece) {
        if (piece == null) {
            return false;
        }
        return HasClearLineOfSightTo(piece.position);
    }

    private void HandleHover(Camera cam)
    {
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

        if (!HasClearPathToPlayer(piece.transform))
            return;


        hoveredPiece = piece;

    }
    
    private void TryPick(Camera cam)
    {

        hoveredPiece = null;
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, raycastMaxDistance, pickableLayers, QueryTriggerInteraction.Collide)) {
            GameManager.Instance.player.ToggleBuildingLinkVisibility(false);
            return;
        }
        BuildPiece piece = hit.collider.GetComponentInParent<BuildPiece>();
        if (piece == null || piece.IsLocked) {
            GameManager.Instance.player.ToggleBuildingLinkVisibility(false);
            return;
        }

        if (!HasClearPathToPlayer(piece.transform))
        {
            GameManager.Instance.player.ToggleBuildingLinkVisibility(false);
            return;
        }

        hoveredPiece = piece;

        GameManager.Instance.player.ToggleBuildingLinkVisibility(true);
        GameManager.Instance.player.UpdateBuldingLink(hoveredPiece.transform.position);
        
        if (Mouse.current.leftButton.wasPressedThisFrame) {
            carriedPiece = piece.transform;
            storedWorldPosition = carriedPiece.position;
            storedWorldRotation = carriedPiece.rotation;
            carriedPiece.GetComponentInChildren<Collider>().enabled = false;
            carryLinkCursorOnPlane = carriedPiece.position;
            CreateDragGhost(piece);
            SoundManager.Instance.PlayOneShot(SoundType.PIECE_PICKUP,0.15f,Random.Range(0.8f,1.2f));
        }
    }

    private void DragCarried(Camera cam) {
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!placementPlane.Raycast(ray, out float enter)) {
            SetDragGhostVisible(false);
            return;
        }
        Vector3 p = ray.GetPoint(enter);
        p.z = planeZ;
        carryLinkCursorOnPlane = p;
        if (HasClearLineOfSightTo(p)) {
            carriedPiece.position = p;
            SetDragGhostVisible(false);
        } else {
            SetDragGhostVisible(true);
            if (dragGhostInstance != null) {
                Vector3 ghostPos = p;
                ghostPos.z += GameManager.Instance.player.BuildingLinkZOffset;
                dragGhostInstance.transform.SetPositionAndRotation(ghostPos, carriedPiece.rotation);
            }
        }
    }

    private void CreateDragGhost(BuildPiece piece) {
        DestroyDragGhost();
        if (piece == null || piece.DragGhostPrefab == null) {
            return;
        }
        dragGhostInstance = Instantiate(piece.DragGhostPrefab);
        dragGhostInstance.SetActive(false);
    }

    private void SetDragGhostVisible(bool visible) {
        if (dragGhostInstance == null) {
            return;
        }
        dragGhostInstance.SetActive(visible);
    }

    private void SyncDragGhostRotation() {
        if (dragGhostInstance == null || !dragGhostInstance.activeSelf) {
            return;
        }
        Vector3 p = dragGhostInstance.transform.position;
        dragGhostInstance.transform.SetPositionAndRotation(p, carriedPiece.rotation);
    }

    private void DestroyDragGhost() {
        if (dragGhostInstance != null) {
            Destroy(dragGhostInstance);
            dragGhostInstance = null;
        }
    }

    private void RotateCarried() {
        float deltaZ = 0f;
        Vector2 scroll = Mouse.current.scroll.ReadValue();
        if (Mathf.Abs(scroll.y) > 0.01f) {
            deltaZ += Mathf.Sign(scroll.y) * rotateScrollDegrees;
        }
        if (Keyboard.current != null) {
            if (Keyboard.current.qKey.isPressed) {
                deltaZ += rotateKeyDegreesPerSecond * Time.deltaTime;
            }
            if (Keyboard.current.eKey.isPressed) {
                deltaZ -= rotateKeyDegreesPerSecond * Time.deltaTime;
            }
        }
        if (Mathf.Abs(deltaZ) > 0f) {
            carriedPiece.Rotate(0f, 0f, deltaZ, Space.World);
        }
    }

    private void PlaceCarried() {
        carriedPiece.GetComponentInChildren<Collider>().enabled = true;
        carriedPiece.GetComponent<BuildPiece>()?.HandleCurrentZone();
        carriedPiece = null;
        DestroyDragGhost();
        GameManager.Instance.player.ToggleBuildingLinkVisibility(false);
        SoundManager.Instance.PlayOneShot(SoundType.PLACE_PIECE,0.15f,Random.Range(0.8f,1.2f));
    }

    
    private void CancelCarry() {
        if (carriedPiece == null) {
            return;
        }
        carriedPiece.position = storedWorldPosition;
        carriedPiece.rotation = storedWorldRotation;
        carriedPiece.GetComponentInChildren<Collider>().enabled = true;
        carriedPiece.GetComponent<BuildPiece>()?.HandleCurrentZone();
        carriedPiece = null;
        DestroyDragGhost();
        SoundManager.Instance.PlayOneShot(SoundType.PLACE_PIECE,0.15f,Random.Range(0.8f,1.2f));
        GameManager.Instance.player.ToggleBuildingLinkVisibility(false);
    }

    public void SetBuildModeEnabled(bool enabled) {
        buildModeEnabled = enabled;
        if (!enabled && carriedPiece != null) {
            CancelCarry();
        }
    }
}
