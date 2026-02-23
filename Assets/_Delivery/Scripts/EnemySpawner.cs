using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns enemies and coordinates package assignment at spawn time.
/// Each enemy is assigned one unique package so enemies don't pile onto the same target.
///
/// Coordination rules:
///   - More enemies than packages  → excess enemies get no target and idle.
///   - More packages than enemies  → unassigned packages are left unclaimed;
///                                   enemies will claim them as fallback when
///                                   their assigned package is destroyed.
///   - Equal counts               → perfect 1-to-1 assignment.
///
/// Inherits shared spawning logic (position, tracking, gizmos) from BaseSpawner.
/// </summary>
public class EnemySpawner : BaseSpawner
{
    [Header("Enemy Settings")]
    [Tooltip("Array of enemy prefabs to choose from at random when spawning.")]
    public GameObject[] m_EnemyTypes;
    
    [Tooltip("How far from the package spawn center enemies are placed. " +
             "Should be larger than the package spawn radius so enemies start outside the package circle.")]
    public float m_SpawnRangeFromPackages = 8f;

    [Tooltip("Optional particle effect spawned at each enemy's spawn position.")]
    public ParticleSystem m_SpawnFX;

    private PackageSpawner m_TargetPackageSpawner;
    
    protected override void Start()
    {
        base.Start();

        if (m_EnemyTypes == null || m_EnemyTypes.Length == 0)
            Debug.LogWarning($"EnemySpawner '{gameObject.name}': no enemy types assigned.");
    }

    // ==================== TRIGGER ====================

    /// <summary>
    /// Called by DeliveryZone when the vehicle stops.
    /// Sets the spawn center and runs the coordinated spawn batch.
    /// </summary>
    public override void TriggerSpawn(PackageSpawner packageSpawner)
    {
        m_TargetPackageSpawner = packageSpawner;
        m_SpawnCenter = transform.position;
        SpawnCoordinatedBatch();
    }

    // ==================== COORDINATED SPAWNING ====================

    /// <summary>
    /// Spawns up to m_MaxActiveEntities enemies and assigns one package per enemy.
    /// Coordination happens here at spawn time — not spread across multiple systems.
    /// </summary>
    private void SpawnCoordinatedBatch()
    {
        // Get the live package list from the package spawner
        List<GameObject> packages = m_TargetPackageSpawner != null
            ? m_TargetPackageSpawner.GetActivePackages()
            : new List<GameObject>();

        // Track which packages have already been assigned this batch
        int nextPackageIndex = 0;

        for (int i = 0; i < m_MaxActiveEntities; i++)
        {
            GameObject enemyGO = InstantiateEnemy();
            if (enemyGO == null) continue;

            EnemyMovement movement = enemyGO.GetComponent<EnemyMovement>();
            if (movement == null)
            {
                Debug.LogError($"EnemySpawner: enemy prefab is missing EnemyMovement component.");
                continue;
            }

            // Always give the enemy the full package list for fallback searching
            movement.SetPackages(packages);

            // Assign a unique package if one is still available
            if (nextPackageIndex < packages.Count)
            {
                GameObject assignedPackageGO = packages[nextPackageIndex];
                Package assignedPackage = assignedPackageGO.GetComponent<Package>();

                if (assignedPackage != null)
                {
                    // Claim the package so no other enemy is assigned it this batch
                    assignedPackage.Claim();
                    movement.SetAssignedTarget(assignedPackageGO.transform);

                    Debug.Log($"EnemySpawner: assigned enemy {i} → package '{assignedPackageGO.name}'.");
                }

                nextPackageIndex++;
            }
            else
            {
                // More enemies than packages — this enemy idles until a package frees up
                movement.SetAssignedTarget(null);
                Debug.Log($"EnemySpawner: enemy {i} has no package to assign. Will idle.");
            }

            m_ActiveEntities.Add(enemyGO);
        }
    }

    /// <summary>
    /// Instantiates a random enemy prefab at a position outside the package circle.
    /// Returns the new GameObject, or null if instantiation failed.
    /// </summary>
    private GameObject InstantiateEnemy()
    {
        if (m_EnemyTypes == null || m_EnemyTypes.Length == 0) return null;

        GameObject prefab = m_EnemyTypes[Random.Range(0, m_EnemyTypes.Length)];
        if (prefab == null) return null;

        Vector3 spawnPos = GetEnemySpawnPosition();
        GameObject instance = Instantiate(prefab, spawnPos, Quaternion.identity);

        if (m_SpawnFX != null)
            Instantiate(m_SpawnFX, spawnPos, Quaternion.identity);

        return instance;
    }

    // ==================== POSITION ====================

    /// <summary>
    /// Returns a spawn position on the outer ring around the package spawn center.
    /// Enemies start outside the package circle so they visibly move inward.
    /// </summary>
    private Vector3 GetEnemySpawnPosition()
    {
        Vector3 result = m_SpawnCenter;

        for (int attempt = 0; attempt < m_MaxSpawnAttempts; attempt++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

            Vector3 candidate = new Vector3(
                m_SpawnCenter.x + m_SpawnRangeFromPackages * Mathf.Cos(angle),
                m_SpawnHeight,
                m_SpawnCenter.z + m_SpawnRangeFromPackages * Mathf.Sin(angle)
            );

            if (IsPositionValid(candidate))
                return candidate;
        }

        return result;
    }

    // ==================== BASE OVERRIDE ====================

    /// <summary>
    /// Not used — EnemySpawner uses SpawnCoordinatedBatch instead of the
    /// single-entity pattern from BaseSpawner. Required by abstract contract.
    /// </summary>
    protected override void SpawnEntity()
    {
        // Intentionally empty — coordination requires spawning the whole batch at once.
        // SpawnCoordinatedBatch() handles everything.
    }

    // ==================== GIZMOS ====================

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        // Draw the enemy spawn ring
        if (m_TargetPackageSpawner != null)
        {
            Vector3 center = Application.isPlaying
                ? m_TargetPackageSpawner.GetSpawnCenter()
                : m_TargetPackageSpawner.transform.position;

            center.y = m_SpawnHeight;

            Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
            DrawCircle(center, m_SpawnRangeFromPackages);
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        if (m_TargetPackageSpawner != null)
        {
            Vector3 center = Application.isPlaying
                ? m_TargetPackageSpawner.GetSpawnCenter()
                : m_TargetPackageSpawner.transform.position;

            center.y = m_SpawnHeight;

            // Show the enemy spawn ring
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            DrawCircle(center, m_SpawnRangeFromPackages);

            // Show the link between the two spawners
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, center);
        }
    }
}