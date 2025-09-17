using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    private readonly Dictionary<EItemType, int> mItems = new Dictionary<EItemType, int>();

    public event Action<EItemType, int> OnItemCountChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void AddItem(EItemType type, int count = 1)
    {
        if (!mItems.ContainsKey(type))
        {
            mItems[type] = 0;
        }
        mItems[type] += count;
        Debug.Log($"Added {count} of {type}. Total: {mItems[type]}");
        OnItemCountChanged?.Invoke(type, mItems[type]);
    }

    public bool UseItem(EItemType type)
    {
        if (mItems.ContainsKey(type) && mItems[type] > 0)
        {
            mItems[type]--;
            Debug.Log($"Used {type}. Remaining: {mItems[type]}");
            OnItemCountChanged?.Invoke(type, mItems[type]);
            return true;
        }
        Debug.LogWarning($"Attempted to use {type}, but none available.");
        return false;
    }

    public int GetItemCount(EItemType type)
    {
        mItems.TryGetValue(type, out int count);
        return count;
    }
}
