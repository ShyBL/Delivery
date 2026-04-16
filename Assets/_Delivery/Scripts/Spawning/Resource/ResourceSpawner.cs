using System.Collections.Generic;
using UnityEngine;


public class ResourceSpawner : BaseSpawner
{
    [Header("Resource Settings")]
    [Tooltip("Array of Resource prefabs that can be chosen at random when spawning.")]
    public Resource[] m_ResourceTypes;

    [Tooltip("Optional particle effect played at each package's spawn position.")]
    public ParticleSystem m_SpawnFX;

    protected override void Start()
    {
        base.Start();

        if (m_ResourceTypes == null || m_ResourceTypes.Length == 0)
            Debug.LogWarning($"PackageSpawnerV2 '{gameObject.name}': no package types assigned.");
    }

    #region SPAWNING

    /// <summary>
    /// Instantiates a single package at a random valid position around the spawn center.
    /// </summary>
    protected override void SpawnEntity()
    {
        if (m_ResourceTypes == null || m_ResourceTypes.Length == 0) return;

        Resource prefab = m_ResourceTypes[Random.Range(0, m_ResourceTypes.Length)];

        if (prefab == null)
        {
            Debug.LogWarning($"PackageSpawnerV2 '{gameObject.name}': null entry in m_PackageTypes.");
            return;
        }

        Vector3 spawnPos = GetRandomSpawnPosition();
        Resource instance = Instantiate(prefab, spawnPos, Quaternion.identity);

        // Wire up spawner and game manager references on the package
        instance.SetSpawner(this, m_GameManager);

        m_ActiveEntities.Add(instance.gameObject);

        if (m_SpawnFX != null)
            Instantiate(m_SpawnFX, spawnPos, Quaternion.identity);

        Debug.Log($"PackageSpawnerV2: spawned {instance.GetPackageType()} package. " +
                  $"Active: {m_ActiveEntities.Count}/{m_MaxActiveEntities}");
    }
    #endregion
    
    #region PACKAGE LIFECYCLE

    /// <summary>
    /// Called by PackageV2 when it is collected by the player or destroyed by an enemy.
    /// Removes the package from the active list so the count stays accurate.
    /// </summary>
    public void OnPackageCollected(GameObject package)
    {
        m_ActiveEntities.Remove(package);
    }
    #endregion
    
    #region ENEMY COORDINATION

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
    #endregion
}