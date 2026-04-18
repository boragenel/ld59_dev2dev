using UnityEngine;

public class LevelBase : MonoBehaviour
{
    public GameObject levelEntrance;
    public GameObject levelExit; //for maybe having to do stuff to unlock exit?


    private void Awake()
    {
        levelEntrance = FindDeepChild(transform, "EntranceGate")?.gameObject;
        levelExit= FindDeepChild(transform, "ExitGate")?.gameObject;
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
