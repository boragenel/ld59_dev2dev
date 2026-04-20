using System.Collections.Generic;
using UnityEngine;

public class SignalMeshFieldManager : MonoBehaviour {
    public static SignalMeshFieldManager Instance { get; private set; }
    public static float CurrentLosFade { get; private set; } = 1f;

    private readonly List<TestSource> sources = new List<TestSource>();

    public int CountMeshesContainingWorldPoint(Vector3 worldPoint) {
        int total = 0;
        for (int i = 0; i < sources.Count; i++) {
            TestSource s = sources[i];
            if (s != null && s.isActiveAndEnabled) {
                total += s.CountMeshesContainingWorldPoint(worldPoint);
            }
        }
        return total;
    }
    
    public List<Vector3> GetMeshesContainingWorldPoint(Vector3 worldPoint) {
        List<Vector3> result = new List<Vector3>();
        for (int i = 0; i < sources.Count; i++) {
            TestSource s = sources[i];
            if (s != null && s.isActiveAndEnabled)
            {
                result.AddRange(s.GetMeshesContainingWorldPoint(worldPoint));
            }
        }
        return result;
    }
    
    public void RegisterSource(TestSource source) {
        if (source != null && !sources.Contains(source)) {
            sources.Add(source);
        }
    }

    public void UnregisterSource(TestSource source) {
        sources.Remove(source);
    }

    public void RefreshSources() {
        sources.Clear();
        TestSource[] found = Object.FindObjectsByType<TestSource>(FindObjectsSortMode.None);
        for (int i = 0; i < found.Length; i++) {
            RegisterSource(found[i]);
        }
    }

    public void ApplyLosFadeToAllSources(float fade) {
        CurrentLosFade = fade;
        for (int i = 0; i < sources.Count; i++) {
            TestSource s = sources[i];
            if (s != null) {
                s.SetLosMaterialFade(fade);
            }
        }
    }

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        RefreshSources();
    }

    private void OnDestroy() {
        if (Instance == this) {
            Instance = null;
            CurrentLosFade = 1f;
        }
    }
}
