// ============================================================
//  ResourceNodeMB.cs
//  Place in: Assets/_Delivery/Scripts/
//  Layer   : Controller — MonoBehaviour, inherits BaseSpawner
//
//  Lives on a fixed spawn point in the scene.
//  When activated by ResourceSpawner, scatters N resource prefab
//  instances around itself using BaseSpawner.m_SpawnRadius.
//
//  Each spawned prefab has a ResourcePickup script that calls
//  OnPickupCollected() when the player collects it.
//  When all pickups are collected, fires OnDepleted so
//  ResourceSpawner can respawn this node.
//
//  Attach to: spawn point GameObjects in the level scene
//  Inspector : assign ResourceType and pickup prefab
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

public class ResourceNodeMB : BaseSpawner
{
    [Header("Node Settings")]
    [SerializeField] private ResourceType  m_ResourceType;
    [SerializeField] private ResourcePickup m_PickupPrefab;

    [Header("Pickups Per Activation")]
    [Tooltip("How many pickup prefabs are spawned when this node is activated.")]
    [SerializeField] private int m_PickupsPerActivation = 3;

    // Fired when all pickups have been collected
    // ResourceSpawner subscribes to this
    public event Action OnDepleted;

    public ResourceType ResourceType => m_ResourceType;

    private readonly List<ResourcePickup> m_SpawnedPickups = new List<ResourcePickup>();
    private bool m_IsActive = false;

    // -------------------------------------------------------
    //  Public API  (called by ResourceSpawner)
    // -------------------------------------------------------

    /// Moves this node to the given world position then spawns its pickups.
    public void Activate(Vector3 position)
    {
        transform.position = position;
        m_SpawnCenter      = position;
        m_IsActive         = true;

        ClearPickups();
        SpawnInitialBatch();

        Debug.Log($"[ResourceNodeMB] Activated {m_ResourceType} node at {position}. " +
                  $"Spawned {m_SpawnedPickups.Count} pickups.");
    }

    /// Deactivates the node and clears all its pickups.
    public void Deactivate()
    {
        m_IsActive = false;
        ClearPickups();
    }

    // -------------------------------------------------------
    //  Called by ResourcePickup when collected by player
    // -------------------------------------------------------

    public void OnPickupCollected(ResourcePickup pickup)
    {
        m_SpawnedPickups.Remove(pickup);
        m_ActiveEntities.RemoveAll(e => e == null);

        if (m_SpawnedPickups.Count == 0 && m_IsActive)
        {
            m_IsActive = false;
            Debug.Log($"[ResourceNodeMB] {m_ResourceType} node depleted.");
            OnDepleted?.Invoke();
        }
    }

    // -------------------------------------------------------
    //  BaseSpawner implementation
    // -------------------------------------------------------

    protected override void SpawnEntity()
    {
        if (m_PickupPrefab == null)
        {
            Debug.LogWarning($"[ResourceNodeMB] No pickup prefab assigned on {gameObject.name}.");
            return;
        }

        Vector3 spawnPos = GetRandomSpawnPosition();

        ResourcePickup pickup = Instantiate(m_PickupPrefab, spawnPos, Quaternion.identity);
        pickup.Initialise(this);

        m_SpawnedPickups.Add(pickup);
        m_ActiveEntities.Add(pickup.gameObject);
    }

    // Override so activation count comes from m_PickupsPerActivation,
    // not m_MaxActiveEntities from BaseSpawner
    protected override void SpawnInitialBatch()
    {
        for (int i = 0; i < m_PickupsPerActivation; i++)
            SpawnEntity();
    }

    // -------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------

    private void ClearPickups()
    {
        foreach (var pickup in m_SpawnedPickups)
            if (pickup != null) Destroy(pickup.gameObject);

        m_SpawnedPickups.Clear();
        m_ActiveEntities.Clear();
    }
}