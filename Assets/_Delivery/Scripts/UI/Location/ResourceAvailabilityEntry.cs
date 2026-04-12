// ============================================================
//  ResourceAvailabilityEntry.cs
//  Place in: Assets/_Delivery/Scripts/
//  Layer   : Data — serializable for Inspector use
//
//  One row in LocationSO: how abundant a resource type is.
//  0 = none  1 = scarce  2 = moderate  3 = abundant
// ============================================================

using UnityEngine;

[System.Serializable]
public class ResourceAvailabilityEntry
{
    public ResourceType resourceType;

    [Range(0, 3)]
    [Tooltip("0 = none  1 = scarce  2 = moderate  3 = abundant")]
    public int abundance;
}
