using UnityEngine;

[System.Serializable]
public class VfxInstancer
{
    public GameObject vfxPrefab;
    public float destroyDelay = 1f;

    public void SpawnVfx(Vector3 position, Quaternion rotation)
    {
        GameObject vfxInstance = Object.Instantiate(vfxPrefab, position, rotation);
        Object.Destroy(vfxInstance, destroyDelay);
    }
}
