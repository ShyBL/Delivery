using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns packages around the vehicle's drop point when triggered by DeliveryZone.
/// Inherits common spawning logic from BaseSpawner.
/// Exposes GetActivePackages() so EnemySpawner can read the live package list
/// for enemy coordination at spawn time.
/// </summary>
public class PackageSpawner : BaseSpawner
{
    [Header("Package Settings")]
    [Tooltip("Array of package prefabs that can be chosen at random when spawning.")]
    public Package[] m_PackageTypes;

    [Tooltip("Optional particle effect played at each package's spawn position.")]
    public ParticleSystem m_SpawnFX;

    protected override void Start()
    {
        base.Start();

        if (m_PackageTypes == null || m_PackageTypes.Length == 0)
            Debug.LogWarning($"PackageSpawnerV2 '{gameObject.name}': no package types assigned.");
    }

    // ==================== SPAWNING ====================

    /// <summary>
    /// Instantiates a single package at a random valid position around the spawn center.
    /// </summary>
    protected override void SpawnEntity()
    {
        if (m_PackageTypes == null || m_PackageTypes.Length == 0) return;

        Package prefab = m_PackageTypes[Random.Range(0, m_PackageTypes.Length)];

        if (prefab == null)
        {
            Debug.LogWarning($"PackageSpawnerV2 '{gameObject.name}': null entry in m_PackageTypes.");
            return;
        }

        Vector3 spawnPos = GetRandomSpawnPosition();
        Package instance = Instantiate(prefab, spawnPos, Quaternion.identity);

        // Wire up spawner and game manager references on the package
        instance.SetSpawner(this, m_GameManager);

        m_ActiveEntities.Add(instance.gameObject);

        if (m_SpawnFX != null)
            Instantiate(m_SpawnFX, spawnPos, Quaternion.identity);

        Debug.Log($"PackageSpawnerV2: spawned {instance.GetPackageType()} package. " +
                  $"Active: {m_ActiveEntities.Count}/{m_MaxActiveEntities}");
    }

    // ==================== PACKAGE LIFECYCLE ====================

    /// <summary>
    /// Called by PackageV2 when it is collected by the player or destroyed by an enemy.
    /// Removes the package from the active list so the count stays accurate.
    /// </summary>
    public void OnPackageCollected(GameObject package)
    {
        m_ActiveEntities.Remove(package);
    }

    // ==================== ENEMY COORDINATION ====================

    /// <summary>
    /// Returns the current list of active package GameObjects.
    /// Called by EnemySpawner at spawn time so it can assign one package per enemy.
    /// The list is a direct reference — nulls are cleaned before returning.
    /// </summary>
    public List<GameObject> GetActivePackages()
    {
        m_ActiveEntities.RemoveAll(e => e == null);
        return m_ActiveEntities;
    }
}