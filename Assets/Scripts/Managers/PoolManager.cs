using System;
using System.Collections;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using Object = UnityEngine.Object;


public enum PoolerType
{
    NONE,
    GENERIC_SPRITE,
    PLAYER_BULLET,
    SIGNAL_RENDERER,
}

[Serializable]
public struct PoolConfig
{
    public PoolerType poolerType;
    public Component component;
    public int poolAmount;
}

public class PoolManager : MonoBehaviour
{
    public List<PoolConfig> poolPopulationConfig = new List<PoolConfig>();
    public static Dictionary<PoolerType, Component> poolLookup = new Dictionary<PoolerType, Component>();
    public static Dictionary<PoolerType, Queue<Component>> poolDictionary = new Dictionary<PoolerType, Queue<Component>>();
    private static bool inited = false;


    private void Awake()
    {
        poolDictionary.Clear();
        poolLookup.Clear();
        foreach (PoolConfig pConfig in poolPopulationConfig)
        {
            SetupPool(pConfig.component, pConfig.poolAmount, pConfig.poolerType);
        }
    }

    public static void EnqueueObject<T>(T item, PoolerType pType) where T : Component
    {
        //this was causing some endless items to be created cause they got turned off before being requeued
        //if (!item.gameObject.activeSelf)
        //    return;

        item.transform.position = Vector3.zero;
        poolDictionary[pType].Enqueue(item);
        item.gameObject.SetActive(false);
    }

    public static T EnqueueNewInstance<T>(T item, PoolerType key) where T : Component
    {
        T newInstance = Object.Instantiate(item);
        newInstance.gameObject.SetActive(false);
        newInstance.transform.position = Vector3.zero;
        poolDictionary[key].Enqueue(newInstance);
        return newInstance;
    }

    public static T DequeueObject<T>(PoolerType key) where T : Component
    {
        if (poolDictionary[key].TryDequeue(out var item))
        {
            return (T)item;
        }
        EnqueueNewInstance(poolLookup[key], key);
        return DequeueObject<T>(key);
    }

    public static void SetupPool<T>(T pooledItemPrefab, int poolsize, PoolerType dictionaryEntry) where T : Component
    {
        //if (inited)
        //{
        //    foreach (KeyValuePair<PoolerType, Queue<Component>> kvp in poolDictionary)
        //    {
        //        foreach (Component c in kvp.Value)
        //        {
        //            Destroy(c);
        //        }
        //        kvp.Value.Clear();
        //    }
        //    poolDictionary.Clear();
        //    poolLookup.Clear();
        //}
        //inited = true;

        poolDictionary.Add(dictionaryEntry, new Queue<Component>());
        poolLookup.Add(dictionaryEntry, pooledItemPrefab);
        for (int i = 0; i < poolsize; i++)
        {
            T pooledInstance = Object.Instantiate(pooledItemPrefab);
            pooledInstance.gameObject.SetActive(false);
            poolDictionary[dictionaryEntry].Enqueue((T)pooledInstance);
        }
    }
}
