// ============================================================
//  LocationSO.cs
//  Place in: Assets/_Delivery/Scripts/
//  Layer   : ScriptableObject — data authoring + runtime state
//
//  Implements ISelectable so LevelCardView can highlight it
//  directly without a plain-C# wrapper class.
//
//  Create via: Right-click > Create > Delivery > LocationSO
// ============================================================

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Delivery/LocationSO", fileName = "New Location")]
public class LocationSO : ScriptableObject, ISelectable
{
    [Header("Identity")]
    public string       locationName;
    public LocationType locationType;

    [Header("Slot Type")]
    [Tooltip("False = fixed slot always dealt. True = enters the wildcard pool.")]
    public bool isWildcard;

    [Header("Resource Availability")]
    [Tooltip("0 = none  1 = scarce  2 = moderate  3 = abundant")]
    public List<ResourceAvailabilityEntry> resourceAvailability;

    // Runtime selection state — reset by GameManager each Board phase
    private bool m_IsSelected;

    // -------------------------------------------------------
    //  ISelectable
    // -------------------------------------------------------

    public void Select()     => m_IsSelected = true;
    public void Deselect()   => m_IsSelected = false;
    public bool IsSelected() => m_IsSelected;

    // -------------------------------------------------------
    //  Query
    // -------------------------------------------------------

    public int GetAvailability(ResourceType type)
    {
        if (resourceAvailability == null) return 0;

        foreach (var entry in resourceAvailability)
            if (entry.resourceType == type)
                return entry.abundance;

        return 0;
    }

    public bool IsResourceAvailable(ResourceType type) =>
        GetAvailability(type) > 0;

    /// Resets runtime state — call at the start of each Board phase.
    public void ResetState() => m_IsSelected = false;

    public override string ToString() =>
        $"[LocationSO:{locationName}] {locationType}{(isWildcard ? " [wildcard]" : "")}";
}
