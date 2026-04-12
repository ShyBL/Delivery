// ============================================================
//  ResourceInventory.cs
//  Place in: Assets/_Delivery/Scripts/
//  Layer   : MonoBehaviour — attach as sibling on Player GameObject
//
//  Single source of truth for what resources the player carries.
//  Fires OnChanged so HUDView refreshes automatically.
//  Stock values visible in Inspector during play for debugging.
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

public class ResourceInventory : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int m_MaxCapacity = 20;

    // Fires on any Add, Remove, or Clear — HUDView / GameManager subscribe here
    public event Action OnChanged;

    // Runtime stock — visible in Inspector during play
    [Header("Current Stock (read-only in play)")]
    [SerializeField, Tooltip("Read-only — driven at runtime")]
    private int m_Scraps;
    [SerializeField, Tooltip("Read-only — driven at runtime")]
    private int m_RawMaterials;
    [SerializeField, Tooltip("Read-only — driven at runtime")]
    private int m_HighEndComponents;

    public int MaxCapacity => m_MaxCapacity;

    #region Unity Lifecycle

    private void OnEnable()
    {
        ResetAll();
    }

    #endregion

    #region Mutation

    public void Add(ResourceType type, int amount)
    {
        if (amount <= 0) return;
        SetStock(type, GetStock(type) + amount);
        Debug.Log($"[Inventory] +{amount} {type}  ({GetTotal()}/{m_MaxCapacity})");
        OnChanged?.Invoke();
    }

    public bool Remove(ResourceType type, int amount)
    {
        int current = GetStock(type);
        if (current < amount)
        {
            Debug.LogWarning($"[Inventory] Remove failed — need {amount} {type}, have {current}");
            return false;
        }
        SetStock(type, current - amount);
        OnChanged?.Invoke();
        return true;
    }

    public void Clear()
    {
        ResetAll();
        OnChanged?.Invoke();
    }

    #endregion

    #region Query

    public int  GetCount(ResourceType type)        => GetStock(type);
    public bool Has(ResourceType type, int amount) => GetStock(type) >= amount;
    public bool IsAtCapacity()                     => GetTotal() >= m_MaxCapacity;

    public int GetTotal() => m_Scraps + m_RawMaterials + m_HighEndComponents;

    public bool CanFulfill(List<ResourceRequirement> requirements)
    {
        foreach (var req in requirements)
            if (!Has(req.resourceType, req.amount)) return false;
        return true;
    }

    #endregion

    #region Helpers

    private int GetStock(ResourceType type)
    {
        switch (type)
        {
            case ResourceType.Scraps:            return m_Scraps;
            case ResourceType.RawMaterials:      return m_RawMaterials;
            case ResourceType.HighEndComponents: return m_HighEndComponents;
            default:                             return 0;
        }
    }

    private void SetStock(ResourceType type, int value)
    {
        switch (type)
        {
            case ResourceType.Scraps:            m_Scraps            = value; break;
            case ResourceType.RawMaterials:      m_RawMaterials      = value; break;
            case ResourceType.HighEndComponents: m_HighEndComponents = value; break;
        }
    }

    private void ResetAll()
    {
        m_Scraps            = 0;
        m_RawMaterials      = 0;
        m_HighEndComponents = 0;
    }

    #endregion
}