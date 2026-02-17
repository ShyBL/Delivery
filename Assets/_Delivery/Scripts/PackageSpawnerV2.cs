using System;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Spawns packages around a specified location.
/// Inherits from BaseSpawner for common spawning logic.
/// Triggered by Vehicle when it stops in the DeliveryZone.
/// </summary>
public class PackageSpawnerV2 : BaseSpawner
{
    [Header("Package Settings")]
    [Tooltip("Array of package prefabs that can spawn at this location.")]
    public Package[] m_PackageTypes;

    private void OnEnable()
    {
        // Validate package types array
        if (m_PackageTypes == null || m_PackageTypes.Length == 0)
        {
            Debug.LogWarning($"PackageSpawner '{gameObject.name}' has no package types assigned!");
        }
    }

    /// <summary>
    /// Spawn a single package at a random valid position.
    /// </summary>
    protected override void SpawnEntity()
    {
        if (m_PackageTypes == null || m_PackageTypes.Length == 0)
        {
            Debug.LogWarning($"Cannot spawn package - no package types assigned to '{gameObject.name}'");
            return;
        }

        // Select a random package type
        int randomIndex = Random.Range(0, m_PackageTypes.Length);
        Package selectedPackage = m_PackageTypes[randomIndex];

        if (selectedPackage == null)
        {
            Debug.LogWarning($"Package type at index {randomIndex} is null in '{gameObject.name}'");
            return;
        }

        // Get random spawn position with collision avoidance
        Vector3 spawnPosition = GetRandomSpawnPosition();

        // Instantiate the package
        Package newPackage = Instantiate(
            selectedPackage,
            spawnPosition,
            Quaternion.identity
        );

        // Set spawner reference on the package
        newPackage.SetSpawner(this, m_GameManager);

        // Add to active entities list
        m_ActiveEntities.Add(newPackage.gameObject);

        Debug.Log($"Spawned {newPackage.GetPackageType()} package. Active: {m_ActiveEntities.Count}/{m_MaxActiveEntities}");
    }

    /// <summary>
    /// Called by Package when it's collected.
    /// Removes it from the active list.
    /// </summary>
    public void OnPackageCollected(GameObject package)
    {
        if (m_ActiveEntities.Contains(package))
        {
            m_ActiveEntities.Remove(package);
            Debug.Log($"Package collected. Remaining: {m_ActiveEntities.Count}");
        }
    }

    /// <summary>
    /// Override to prevent continuous spawning.
    /// Packages only spawn when initially triggered, not over time.
    /// </summary>
    protected override void UpdateSpawnTimer()
    {
        // Packages don't respawn automatically in this scenario
        // They only spawn once when the vehicle arrives
        // So we override this to do nothing
    }
}