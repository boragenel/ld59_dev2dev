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
}
