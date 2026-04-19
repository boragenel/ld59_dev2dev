using UnityEngine;

public class TestMirror : MonoBehaviour {
    public float Width = 1f;

    public Vector3 WorldA => transform.TransformPoint(new Vector3(-Width * 0.5f, 0f, 0f));
    public Vector3 WorldB => transform.TransformPoint(new Vector3(Width * 0.5f, 0f, 0f));

    public void GetWorldEndpoints(out Vector3 a, out Vector3 b) {
        a = WorldA;
        b = WorldB;
    }

    public Vector3 WorldSegmentDirection {
        get {
            Vector3 d = WorldB - WorldA;
            return d.sqrMagnitude > 1e-12f ? d.normalized : transform.right;
        }
    }

    public Vector3 WorldNormalXy {
        get {
            Vector3 d = WorldB - WorldA;
            float sq = d.x * d.x + d.y * d.y;
            if (sq < 1e-12f) {
                return Vector3.up;
            }
            return new Vector3(-d.y, d.x, 0f).normalized;
        }
    }

    private void OnDrawGizmos() {
        Vector3 a = WorldA;
        Vector3 b = WorldB;
        Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.95f);
        Gizmos.DrawLine(a, b);
        float r = Mathf.Max(0.02f, Width * 0.04f);
        Gizmos.DrawSphere(a, r);
        Gizmos.DrawSphere(b, r);
    }
}
