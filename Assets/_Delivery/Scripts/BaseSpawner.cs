using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Abstract base class for all spawners in the Delivery game.
/// Provides shared logic for entity tracking, position validation, and cleanup.
/// Concrete spawners inherit from this and implement SpawnEntity().
/// Mirrors the shared structure seen across Tanks manager classes.
/// </summary>
public abstract class BaseSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Maximum number of entities this spawner can have active at once.")]
    public int m_MaxActiveEntities = 3;

    [Header("Spawn Position")]
    [Tooltip("Radius around the spawn center within which entities are placed.")]
    public float m_SpawnRadius = 5f;

    [Tooltip("Minimum horizontal distance required between any two spawned entities.")]
    public float m_MinDistanceBetweenEntities = 2f;

    [Tooltip("Y position used when placing spawned entities.")]
    public float m_SpawnHeight = 1.09f;

    [Tooltip("How many attempts to find a valid spawn position before giving up.")]
    public int m_MaxSpawnAttempts = 10;

    protected Vector3 m_SpawnCenter = Vector3.zero;     // The world-space center point set when TriggerSpawn is called
    protected List<GameObject> m_ActiveEntities = new List<GameObject>();     // Tracks all living entities this spawner owns
    protected GameManager m_GameManager;

    protected virtual void Start()
    {
        m_GameManager = GameManager.instance;

        if (m_GameManager == null)
            Debug.LogWarning($"{GetType().Name}: No GameManager found in scene.");
    }

    #region PUBLIC INTERFACE

    /// <summary>
    /// Called by DeliveryZone when the vehicle stops.
    /// Sets the spawn center and spawns the initial batch of entities.
    /// </summary>
    public virtual void TriggerSpawn(PackageSpawner packageSpawner)
    {
        m_SpawnCenter = transform.position;
        SpawnInitialBatch();
    }

    /// <summary>
    /// Destroys all active entities and resets state.
    /// Called when the scenario ends or the zone resets.
    /// </summary>
    public virtual void CleanupEntities()
    {
        foreach (GameObject entity in m_ActiveEntities)
        {
            if (entity != null)
                Destroy(entity);
        }

        m_ActiveEntities.Clear();
    }
    #endregion
    
    #region SPAWNING

    /// <summary>
    /// Spawns up to m_MaxActiveEntities entities in one go.
    /// </summary>
    protected virtual void SpawnInitialBatch()
    {
        int toSpawn = m_MaxActiveEntities - m_ActiveEntities.Count;

        for (int i = 0; i < toSpawn; i++)
        {
            SpawnEntity();
        }
    }

    /// <summary>
    /// Spawn a single entity. Must be implemented by derived classes.
    /// </summary>
    protected abstract void SpawnEntity();
    #endregion
    
    #region POSITION HELPERS

    /// <summary>
    /// Returns a random position within m_SpawnRadius of m_SpawnCenter,
    /// separated from existing entities by at least m_MinDistanceBetweenEntities.
    /// </summary>
    protected Vector3 GetRandomSpawnPosition()
    {
        Vector3 result = m_SpawnCenter;
        result.y = m_SpawnHeight;

        for (int attempt = 0; attempt < m_MaxSpawnAttempts; attempt++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(0f, m_SpawnRadius);

            Vector3 candidate = new Vector3(
                m_SpawnCenter.x + distance * Mathf.Cos(angle),
                m_SpawnHeight,
                m_SpawnCenter.z + distance * Mathf.Sin(angle)
            );

            if (IsPositionValid(candidate))
                return candidate;
        }

        Debug.LogWarning($"{GetType().Name}: Could not find a valid spawn position after " +
                         $"{m_MaxSpawnAttempts} attempts. Using spawn center.");
        return result;
    }

    /// <summary>
    /// Returns true if the candidate position is far enough from all active entities.
    /// </summary>
    protected bool IsPositionValid(Vector3 candidate)
    {
        foreach (GameObject entity in m_ActiveEntities)
        {
            if (entity == null) continue;

            float dist = Vector2.Distance(
                new Vector2(candidate.x, candidate.z),
                new Vector2(entity.transform.position.x, entity.transform.position.z)
            );

            if (dist < m_MinDistanceBetweenEntities)
                return false;
        }

        return true;
    }
    #endregion
    
    #region GETTERS

    public Vector3 GetSpawnCenter() => m_SpawnCenter;

    public int GetActiveEntityCount()
    {
        m_ActiveEntities.RemoveAll(e => e == null);
        return m_ActiveEntities.Count;
    }
    #endregion
    
    #region GIZMOS

    protected virtual void OnDrawGizmos()
    {
        Vector3 center = Application.isPlaying ? m_SpawnCenter : transform.position;
        center.y = m_SpawnHeight;

        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        DrawCircle(center, m_SpawnRadius);
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? m_SpawnCenter : transform.position;
        center.y = m_SpawnHeight;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(center, 0.4f);

        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        DrawCircle(center, m_SpawnRadius);

        if (!Application.isPlaying) return;

        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        foreach (GameObject entity in m_ActiveEntities)
        {
            if (entity == null) continue;
            Vector3 pos = entity.transform.position;
            pos.y = m_SpawnHeight;
            DrawCircle(pos, m_MinDistanceBetweenEntities);
        }
    }

    protected void DrawCircle(Vector3 center, float radius, int segments = 32)
    {
        float step = 360f / segments;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float rad = i * step * Mathf.Deg2Rad;
            Vector3 next = center + new Vector3(Mathf.Cos(rad) * radius, 0f, Mathf.Sin(rad) * radius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
    #endregion
}