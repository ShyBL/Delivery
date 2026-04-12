// ============================================================
//  Player.cs
//  Place in: Assets/_Delivery/Scripts/
//  Layer   : Controller — MonoBehaviour
//
//  Manages the player-controlled delivery bot.
//  Owns ResourceInventory (required sibling component).
//  Exposes GetInventory() for GameManager and HUDView.
//
//  Attach to: Player root GameObject
//  Requires : ResourceInventory (auto-added via RequireComponent)
// ============================================================

using UnityEngine;

[RequireComponent(typeof(ResourceInventory))]
public class Player : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RobotAnimate m_RobotAnimate;
    [SerializeField] private RobotMove    m_RobotMove;

    private ResourceInventory m_Inventory;

    // -------------------------------------------------------
    //  Unity lifecycle
    // -------------------------------------------------------

    private void Awake()
    {
        if (!TryGetComponent(out m_Inventory))
            Debug.LogError($"[Player] Missing ResourceInventory on {gameObject.name}", this);
    }

    // -------------------------------------------------------
    //  Public API
    // -------------------------------------------------------

    public ResourceInventory GetInventory() => m_Inventory;

    /// Called by ResourceNodeMB.OnTriggerEnter when the player
    /// walks over a resource node.
    public void OnResourceCollected(ResourceType type, int amount)
    {
        if (m_Inventory.IsAtCapacity())
        {
            Debug.Log("[Player] Inventory full — cannot collect more.");
            return;
        }

        m_Inventory.Add(type, amount);

        if (m_RobotAnimate != null)
            m_RobotAnimate.AnimatePickup();
    }

    /// Enables or disables player movement control.
    public void ToggleControl(bool value)
    {
        if (m_RobotMove != null)
            m_RobotMove.enabled = value;
    }
}