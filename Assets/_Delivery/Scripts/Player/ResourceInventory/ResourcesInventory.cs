using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;

public class ResourcesInventory : MonoBehaviour
{
    // Views subscribe here — fired on any Add, Remove, or Clear
    public event Action OnChanged;
    private readonly Dictionary<ResourceType, int> _stock =
        new Dictionary<ResourceType, int>();

    public int maxCapacity { get; private set; }

    private void Start()
    {
        maxCapacity = maxCapacity;

        // Seed every type at 0 so GetCount never throws KeyNotFound
        foreach (ResourceType t in Enum.GetValues(typeof(ResourceType)))
            _stock[t] = 0;
        
        SyncInspectorView();

    }

    #region Mutation
    
    public void Add(ResourceType type, int amount)
    {
        if (amount <= 0) return;
        _stock[type] += amount;
        Debug.Log($"[Inventory] +{amount} {type}  ({GetTotal()}/{maxCapacity})");
        OnChanged?.Invoke();
        
        SyncInspectorView();
    }

    public bool Remove(ResourceType type, int amount)
    {
        if (!Has(type, amount))
        {
            Debug.LogWarning($"[Inventory] Remove failed — need {amount} {type}, have {_stock[type]}");
            return false;
        }
        _stock[type] -= amount;
        OnChanged?.Invoke();
        
        SyncInspectorView();
        
        return true;
    }

    public void Clear()
    {
        foreach (ResourceType t in Enum.GetValues(typeof(ResourceType)))
            _stock[t] = 0;
        OnChanged?.Invoke();
        
        SyncInspectorView();
    }
    
    #endregion
    
    #region Query
    
    public int  GetCount(ResourceType type) => _stock[type];
    public bool Has(ResourceType type, int amount) => _stock[type] >= amount;
    
    public bool IsAtCapacity() => GetTotal() >= maxCapacity;
    public int GetTotal()
    {
        int n = 0;
        foreach (int v in _stock.Values) n += v;
        return n;
    }

    public bool CanFulfill(List<ResourceRequirement> requirements)
    {
        foreach (var req in requirements)
            if (!Has(req.resourceType, req.amount)) return false;
        return true;
    }

    public override string ToString()
    {
        var sb = new StringBuilder("[Inventory] ");
        foreach (var kv in _stock)
            sb.Append($"{kv.Key}:{kv.Value}  ");
        return sb.ToString();
    }
    
    #endregion
    
    #region Serialization Helpers
    
    [Serializable]
    public class ResourceInventory
    {
        public ResourceType type;
        public int amount;
    }
    
    [SerializeField]
    private List<ResourceInventory> dictionaryInspectorView = new List<ResourceInventory>();
    
    private void SyncInspectorView()
    {
        dictionaryInspectorView.Clear();

        foreach (var kv in _stock)
        {
            dictionaryInspectorView.Add(new ResourceInventory
            {
                type = kv.Key,
                amount = kv.Value
            });
        }
    }
    
    #endregion
}