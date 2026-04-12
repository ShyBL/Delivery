// ============================================================
//  ResourceRequirement.cs
//  Place in: Assets/_Delivery/Scripts/
//  Layer   : Data — serializable for Inspector use
//
//  One line item in a ContractSO: e.g. "3x Scraps"
// ============================================================

using UnityEngine;

[System.Serializable]
public class ResourceRequirement
{
    public ResourceType resourceType;

    [Min(1)]
    public int amount;

    public override string ToString() => $"{amount}x {resourceType}";
}
