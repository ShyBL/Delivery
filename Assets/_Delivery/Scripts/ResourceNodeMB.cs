// ============================================================
//  ResourceNodeMB.cs
//  Place in: Assets/_Delivery/Scripts/
//  Layer   : MonoBehaviour — lives on resource node prefabs
//
//  A single collectable resource node in the world.
//  Spawned and tracked by ResourceSpawner.
//  Fires OnDepleted when fully collected so ResourceSpawner
//  can respawn a replacement node.
//
//  Collection is triggered by Player.OnResourceCollected()
//  via a trigger collider on the node prefab.
// ============================================================

using System;
using UnityEngine;

public class ResourceNodeMB : MonoBehaviour
{
    [Header("Resource")]
    [SerializeField] private ResourceType m_ResourceType;
    [SerializeField] private int          m_AmountPerCollect = 1;

    [Header("Visuals")]
    [SerializeField] private GameObject m_ActiveVisual;
    [SerializeField] private GameObject m_DepletedVisual;

    // Fired when the node has been fully collected.
    // ResourceSpawner subscribes to this to trigger a respawn.
    public event Action OnDepleted;

    private int  m_RemainingAmount;
    private bool m_IsDepleted;

    public ResourceType Type            => m_ResourceType;
    public bool         IsDepleted      => m_IsDepleted;
    public int          RemainingAmount => m_RemainingAmount;

    #region Initialisation

    public void Initialise(ResourceType type, int totalAmount)
    {
        m_ResourceType    = type;
        m_RemainingAmount = totalAmount;
        m_IsDepleted      = false;
        RefreshVisuals();
    }

    #endregion

    #region Collection Logic

    public bool IsAvailable() => !m_IsDepleted && m_RemainingAmount > 0;

    /// Collects one unit. Outputs what was collected.
    /// Returns false if already depleted.
    public bool TryCollect(out ResourceType type, out int amount)
    {
        type   = m_ResourceType;
        amount = 0;

        if (!IsAvailable()) return false;

        amount             = Mathf.Min(m_AmountPerCollect, m_RemainingAmount);
        m_RemainingAmount -= amount;

        if (m_RemainingAmount <= 0)
        {
            m_IsDepleted = true;
            RefreshVisuals();
            OnDepleted?.Invoke();
        }

        return true;
    }

    #endregion

    #region Trigger Events

    private void OnTriggerEnter(Collider other)
    {
        if (!IsAvailable()) return;

        if (other.TryGetComponent(out Player player))
        {
            if (TryCollect(out ResourceType type, out int amount))
                player.OnResourceCollected(type, amount);
        }
    }

    #endregion

    #region Visuals

    private void RefreshVisuals()
    {
        if (m_ActiveVisual != null)
            m_ActiveVisual.SetActive(!m_IsDepleted);

        if (m_DepletedVisual != null)
            m_DepletedVisual.SetActive(m_IsDepleted);
    }

    #endregion
}