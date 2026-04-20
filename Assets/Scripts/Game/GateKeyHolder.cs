using System.Collections.Generic;
using UnityEngine;

public class GateKeyHolder : MonoBehaviour
{
    public GameObject keyHolePrefab;
    public Transform keyHoleParent;
    public List<GameObject> keyHoles = new List<GameObject>();
    public Material keyHoleMaterial;
    public Material keyHoleLitMaterial;

    private void Start()
    {
        Init(GameManager.Instance.currentLevel.keyCount);
    }

    private void OnEnable()
    {
        LevelBase.keyCountChanged += UpdateKeyholes;
    }

    private void OnDisable()
    {
        LevelBase.keyCountChanged -= UpdateKeyholes;
    }


    void Init(int keys)
    {
        if (keys <= 0)
        {
            return;
        }
        float degreesEach = 360f / keys;

        for (int i = 0; i < keys; i++)
        {
            GameObject keyHole = Instantiate(keyHolePrefab, keyHoleParent);
            MeshRenderer renderer = keyHole.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material = keyHoleMaterial;
            }
            keyHole.transform.localRotation = Quaternion.Euler(0, degreesEach * i, 0);
            keyHoles.Add(keyHole);
        }
    }

    void UpdateKeyholes(int keysLeft)
    {
        for (int i = 0; i < keyHoles.Count; i++)
        {
            MeshRenderer renderer = keyHoles[i].GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material = i < keysLeft ? keyHoleMaterial : keyHoleLitMaterial;
            }
        }
    }
}
