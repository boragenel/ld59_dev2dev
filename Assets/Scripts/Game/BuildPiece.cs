using UnityEngine;

public class BuildPiece : MonoBehaviour {
    [SerializeField]
    private bool isLocked;
    [SerializeField]
    private GameObject lockSprite;

    public bool IsLocked => isLocked;

    private void Awake() {
        ApplyLockVisual();
    }

    private void OnValidate() {
        ApplyLockVisual();
    }

    public void SetLocked(bool locked) {
        isLocked = locked;
        ApplyLockVisual();
    }

    private void ApplyLockVisual() {
        if (lockSprite != null) {
            lockSprite.SetActive(isLocked);
        }
    }

    public void HandleCurrentZone()
    {
        Physics.Raycast(transform.position + Vector3.back * 0.5f, Vector3.forward, out RaycastHit hit, 25, LayerMask.GetMask("Zone"));
        if (hit.collider != null)
        {
            if (transform.parent != hit.transform.parent)
            {
                transform.SetParent(hit.transform.parent, true);
            }
        }
    }
    
    
}
