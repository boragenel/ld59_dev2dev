using System;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class SignalMeshPointReceiver : MonoBehaviour {
    [SerializeField]
    private Vector3 worldOffset;

    public int SignalStrength;

    public event Action<int> OnSignalStrengthChanged;

    private void LateUpdate() {
        Vector3 sample = transform.position + worldOffset;
        SignalMeshFieldManager m = SignalMeshFieldManager.Instance;
        int next = m != null ? m.CountMeshesContainingWorldPoint(sample) : 0;
        if (next == SignalStrength) {
            return;
        }
        SignalStrength = next;
        OnSignalStrengthChanged?.Invoke(SignalStrength);
    }

    public void ResetSources() {
    }
}
