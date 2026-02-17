using System.Collections.Generic;
using UnityEngine;

public abstract class BaseSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Maximum number of entities this spawner can have active at once.")]
    public int m_MaxActiveEntities = 3;

    [Tooltip("Time in seconds between spawns (if applicable).")]
    public float m_SpawnInterval = 5f;

    [Header("Spawn Position")]
    [Tooltip("Radius around the spawn center where entities can spawn.")]
    public float m_SpawnRadius = 5f;

    [Tooltip("Minimum distance between spawned entities.")]
    public float m_MinDistanceBetweenEntities = 2f;

    [Tooltip("Vertical offset for spawned entities (Y position).")]
    public float m_SpawnHeight = 1.09f;

    [Tooltip("Maximum attempts to find a valid spawn position.")]
    public int m_MaxSpawnAttempts = 10;
    
    // Protected fields accessible to derived classes
    protected List<GameObject> m_ActiveEntities = new List<GameObject>();
    protected Vector3 m_SpawnCenter = Vector3.zero;
    protected bool m_IsActive = false;
    protected float m_SpawnTimer = 0f;
    protected GameManager m_GameManager;
    
    protected abstract void SpawnEntity();
    
    protected virtual void Update()
    {
        if (!m_IsActive) return;

        // Clean up null references (destroyed entities)
        m_ActiveEntities.RemoveAll(entity => entity == null);

        // Update spawn timer if needed by derived class
        UpdateSpawnTimer();
    }
    
    protected virtual void UpdateSpawnTimer()
    {
        if (m_ActiveEntities.Count >= m_MaxActiveEntities)
            return;

        m_SpawnTimer += Time.deltaTime;

        if (m_SpawnTimer >= m_SpawnInterval)
        {
            SpawnEntity();
            m_SpawnTimer = 0f;
        }
    }
    
    /// <param name="center">The world position to spawn entities around.</param>
    public virtual void TriggerSpawn(Vector3 center)
    {
        m_SpawnCenter = center;
        m_IsActive = true;
        m_SpawnTimer = 0f;

        // Spawn initial batch of entities
        SpawnInitialBatch();
    }
    
    protected virtual void SpawnInitialBatch()
    {
        int entitiesToSpawn = Mathf.Min(m_MaxActiveEntities, m_MaxActiveEntities - m_ActiveEntities.Count);

        for (int i = 0; i < entitiesToSpawn; i++)
        {
            SpawnEntity();
        }
    }
    
    /// <returns>A valid spawn position in world space.</returns>
    protected virtual Vector3 GetRandomSpawnPosition()
    {
        Vector3 spawnPosition = Vector3.zero;
        bool validPositionFound = false;
        int attempts = 0;

        while (!validPositionFound && attempts < m_MaxSpawnAttempts)
        {
            attempts++;

            // Generate random angle (0 to 360 degrees)
            float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

            // Generate random distance within the spawn radius
            float randomDistance = Random.Range(0f, m_SpawnRadius);

            // Calculate position on circle
            float x = m_SpawnCenter.x + randomDistance * Mathf.Cos(randomAngle);
            float z = m_SpawnCenter.z + randomDistance * Mathf.Sin(randomAngle);

            spawnPosition = new Vector3(x, m_SpawnHeight, z);

            // Check if position is far enough from all active entities
            validPositionFound = IsPositionValid(spawnPosition);
        }

        if (!validPositionFound)
        {
            Debug.LogWarning($"{GetType().Name}: Could not find valid spawn position after {m_MaxSpawnAttempts} attempts. Using last attempted position.");
        }

        return spawnPosition;
    }
    
    protected virtual bool IsPositionValid(Vector3 position)
    {
        foreach (GameObject entity in m_ActiveEntities)
        {
            if (entity != null)
            {
                // Calculate horizontal distance (ignore Y axis)
                Vector3 entityPos = entity.transform.position;
                float distance = Vector2.Distance(
                    new Vector2(position.x, position.z),
                    new Vector2(entityPos.x, entityPos.z)
                );

                if (distance < m_MinDistanceBetweenEntities)
                {
                    return false;
                }
            }
        }

        return true;
    }

    #region Gizmos For Editor

    protected virtual void OnDrawGizmos()
    {
        // Draw spawn center
        Gizmos.color = Color.cyan;
        Vector3 gizmoPosition = transform.position;
        gizmoPosition.y = m_SpawnHeight;

        Gizmos.DrawWireSphere(gizmoPosition, 0.5f);

        // Draw spawn radius circle
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        DrawCircle(gizmoPosition, m_SpawnRadius);
    }

    protected virtual void OnDrawGizmosSelected()
    {
        // Draw more detailed info when selected
        Gizmos.color = Color.yellow;
        Vector3 gizmoPosition = transform.position;
        gizmoPosition.y = m_SpawnHeight;

        Gizmos.DrawSphere(gizmoPosition, 0.5f);

        // Draw spawn range indicator
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        DrawCircle(gizmoPosition, m_SpawnRadius);

        // Draw minimum distance indicators for active entities
        if (Application.isPlaying)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            foreach (GameObject entity in m_ActiveEntities)
            {
                if (entity != null)
                {
                    Vector3 entityPos = entity.transform.position;
                    entityPos.y = m_SpawnHeight;
                    DrawCircle(entityPos, m_MinDistanceBetweenEntities);
                }
            }
        }
    }

    protected void DrawCircle(Vector3 center, float radius, int segments = 32)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );

            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }

    #endregion
}
