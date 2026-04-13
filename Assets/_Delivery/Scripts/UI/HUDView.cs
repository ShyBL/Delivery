// ============================================================
//  HUDView.cs
//  Place in: Assets/_Delivery/Scripts/
//  Layer   : View — MonoBehaviour
//
//  In-level heads-up display. Shows:
//    - Current inventory counts per ResourceType
//    - Contract checklist (checkmark when requirement met)
//
//  Updated by GameManager via:
//    UpdateInventory()  -- wired to ResourceInventory.OnChanged
//    UpdateChecklist()  -- called alongside UpdateInventory
//
//  Attach to: HUD Canvas (always-on-screen overlay)
//  Inspector : assign TMP labels and checklist container
// ============================================================

using UnityEngine;
using TMPro;

public class HUDView : MonoBehaviour
{
    [Header("Inventory Counts")]
    [SerializeField] private TextMeshProUGUI m_ScrapsCount;
    [SerializeField] private TextMeshProUGUI m_RawCount;
    [SerializeField] private TextMeshProUGUI m_HiEndCount;
    [SerializeField] private TextMeshProUGUI m_TotalCount;

    [Header("Contract Checklist")]
    [SerializeField] private Transform       m_ChecklistContainer;
    [SerializeField] private GameObject m_ChecklistLinePrefab;

    // -------------------------------------------------------
    //  Inventory  (driven by ResourceInventory.OnChanged)
    // -------------------------------------------------------

    public void UpdateInventory(ResourceInventory inventory)
    {
        m_ScrapsCount.text = inventory.GetCount(ResourceType.Scraps).ToString();
        m_RawCount.text    = inventory.GetCount(ResourceType.RawMaterials).ToString();
        m_HiEndCount.text  = inventory.GetCount(ResourceType.HighEndComponents).ToString();
        m_TotalCount.text  = $"{inventory.GetTotal()} / {inventory.MaxCapacity}";
    }

    // -------------------------------------------------------
    //  Contract Checklist  (driven by GameManager)
    // -------------------------------------------------------

    public void UpdateChecklist(ContractSO contract, ResourceInventory inventory)
    {
        // Clear old lines
        foreach (Transform child in m_ChecklistContainer)
            Destroy(child.gameObject);

        if (contract == null) return;

        foreach (var req in contract.GetRequirements())
        {
            int  have = inventory.GetCount(req.resourceType);
            bool met  = have >= req.amount;

            var obj  = Instantiate(m_ChecklistLinePrefab, m_ChecklistContainer); 
            var line = obj.GetComponent<TextMeshProUGUI>();
            line.text  = $"{(met ? "✓" : "○")}  {req.resourceType}  {have} / {req.amount}";
            line.color = met ? Color.green : Color.white;
        }
    }
}
