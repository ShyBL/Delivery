// ============================================================
//  ContractSO.cs
//  Place in: Assets/_Delivery/Scripts/
//  Layer   : ScriptableObject — data authoring + runtime state
//
//  Implements ISelectable so ContractCardView can highlight it
//  directly without a plain-C# wrapper class.
//
//  Create via: Right-click > Create > Delivery > ContractSO
// ============================================================

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Delivery/ContractSO", fileName = "New Contract")]
public class ContractSO : ScriptableObject, ISelectable
{
    [Header("Identity")]
    public string        displayName;
    public string        clientName;
    public ClientFaction faction;
    public int           payout;

    [Header("Requirements")]
    public List<ResourceRequirement> requirements;

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

    public List<ResourceRequirement> GetRequirements() =>
        new List<ResourceRequirement>(requirements);

    public bool IsCompletable(ResourceInventory inventory) =>
        inventory.CanFulfill(requirements);

    /// Resets runtime state — call at the start of each Board phase.
    public void ResetState() => m_IsSelected = false;

    public override string ToString() =>
        $"[ContractSO:{displayName}] {faction}";
}
