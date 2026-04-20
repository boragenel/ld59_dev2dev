using System;
using UnityEngine;

public class LevelBase : MonoBehaviour {
    public GameObject levelEntrance;
    [ReadOnly] public Gate levelExit;
    [ReadOnly] public int keyCount = 0;
    public static event Action<int> keyCountChanged;


    private void Awake()
    {
        keyCount = GetComponentsInChildren<CollectableKey>()?.Length ?? 0;

        levelEntrance = FindDeepChild(transform, "EntranceGate")?.gameObject;
        levelExit = GetComponentInChildren<Gate>();

        if (keyCount > 0) levelExit.LockGate();
    }
    public void OnCollectKey()
    {
        keyCount--;
        keyCountChanged?.Invoke(keyCount);
        if (keyCount <= 0) levelExit.UnlockGate();
    }

    public static Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            Transform result = FindDeepChild(child, name);
            if (result != null)
                return result;
        }
        return null;
    }
    public void StartLevel()
    {

    }
}
