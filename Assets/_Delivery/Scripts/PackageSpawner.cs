using System.Collections.Generic;
using UnityEngine;

public class PackageSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Array of package prefabs that can spawn at this location.")]
    public Package[] m_PackageTypes;

    [Tooltip("Time in seconds between package spawns.")]
    public float m_SpawnInterval = 10f;

    [Tooltip("Maximum number of packages this spawner can have active at once.")]
    public int m_MaxActivePackages = 3;

    [Tooltip("Spawn a package immediately when the game starts.")]
    public bool m_SpawnOnStart = true;

    [Header("Spawn Position")]
    [Tooltip("Vertical offset for spawned packages (Y position).")]
    public float m_SpawnHeight = 1.09f;

    [Tooltip("Radius of the circle where packages can spawn.")]
    public float m_SpawnRadius = 5f;

    [Tooltip("Minimum distance between spawned packages.")]
    public float m_MinDistanceBetweenPackages = 2f;

    [Tooltip("Maximum attempts to find a valid spawn position.")]
    public int m_MaxSpawnAttempts = 10;

    [Tooltip("Cooldown time in seconds after a package is collected before spawning again.")]
    public float m_CollectionCooldown = 2f;
    
    // Private variables
    private List<Package> m_ActivePackages = new List<Package>();
    private float m_SpawnTimer = 0f;
    private bool m_IsSpawning = true;
    private GameManager m_GameManager;
    private float m_CollectionCooldownTimer = 0f;
    private void Awake()
    {
        // Find the GameManager in the scene
        m_GameManager = GameManager.instance;
        
        if (m_GameManager == null)
        {
            Debug.LogError("Package: No GameManager found in scene!");
        }
    }

    private void Start()
    {
        // Validate package types array
        if (m_PackageTypes == null || m_PackageTypes.Length == 0)
        {
            Debug.LogWarning($"PackageSpawner '{gameObject.name}' has no package types assigned!");
            return;
        }

        // Spawn initial package if enabled
        if (m_SpawnOnStart)
        {
            SpawnPackage();
        }
    }

    private void Update()
    {
        if (!m_IsSpawning) return;

        // Clean up null references (destroyed packages)
        m_ActivePackages.RemoveAll(package => package == null);

        // Decrement collection cooldown timer
        if (m_CollectionCooldownTimer > 0f)
        {
            m_CollectionCooldownTimer -= Time.deltaTime;
            return; // Don't spawn while in cooldown
        }
        
        // Increment spawn timer
        m_SpawnTimer += Time.deltaTime;

        // Check if it's time to spawn a new package
        if (m_SpawnTimer >= m_SpawnInterval && m_ActivePackages.Count < m_MaxActivePackages)
        {
            SpawnPackage();
            m_SpawnTimer = 0f;
        }
    }

    private void SpawnPackage()
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

        // Set this spawner as the package's parent spawner
        newPackage.SetSpawner(this, m_GameManager);

        // Add to active packages list
        m_ActivePackages.Add(newPackage);

        Debug.Log($"Spawned {newPackage.GetPackageType()} package at {gameObject.name}. Active: {m_ActivePackages.Count}/{m_MaxActivePackages}");
    }

    // Called by Package when it's collected
    public void OnPackageCollected()
    {
        // Start the collection cooldown timer
        m_CollectionCooldownTimer = m_CollectionCooldown;
    
        // Reset spawn timer to prevent immediate spawn after cooldown
        m_SpawnTimer = 0f;
    }

    // === PUBLIC CONTROL METHODS ===

    public void StartSpawning()
    {
        m_IsSpawning = true;
    }

    public void StopSpawning()
    {
        m_IsSpawning = false;
    }

    public void ClearAllPackages()
    {
        foreach (var package in m_ActivePackages)
        {
            if (package != null)
            {
                Destroy(package.gameObject);
            }
        }
        m_ActivePackages.Clear();
    }

    // === GETTERS ===

    public int GetActivePackageCount()
    {
        // Clean null references before counting
        m_ActivePackages.RemoveAll(package => package == null);
        return m_ActivePackages.Count;
    }

    public bool CanSpawnMore()
    {
        return m_ActivePackages.Count < m_MaxActivePackages;
    }

    // === GIZMOS FOR EDITOR ===

    private void OnDrawGizmos()
    {
        // Draw spawn location in editor
        Gizmos.color = Color.cyan;
        Vector3 gizmoPosition = transform.position;
        gizmoPosition.y = m_SpawnHeight;
        
        Gizmos.DrawWireSphere(gizmoPosition, 1f);
        Gizmos.DrawLine(transform.position, gizmoPosition);
        
        // Draw spawn radius circle
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        DrawCircle(gizmoPosition, m_SpawnRadius);
    }

    private void OnDrawGizmosSelected()
    {
        // Draw more detailed info when selected
        Gizmos.color = Color.yellow;
        Vector3 gizmoPosition = transform.position;
        gizmoPosition.y = m_SpawnHeight;
        
        Gizmos.DrawSphere(gizmoPosition, 0.5f);
        
        // Draw spawn range indicator
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        DrawCircle(gizmoPosition, m_SpawnRadius);
        
        // Draw minimum distance indicators for active packages
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        foreach (Package package in m_ActivePackages)
        {
            if (package != null)
            {
                Vector3 packagePos = package.transform.position;
                packagePos.y = m_SpawnHeight;
                DrawCircle(packagePos, m_MinDistanceBetweenPackages);
            }
        }
    }

    private Vector3 GetRandomSpawnPosition()
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
            float x = transform.position.x + randomDistance * Mathf.Cos(randomAngle);
            float z = transform.position.z + randomDistance * Mathf.Sin(randomAngle);

            spawnPosition = new Vector3(x, m_SpawnHeight, z);

            // Check if position is far enough from all active packages
            validPositionFound = IsPositionValid(spawnPosition);
        }

        if (!validPositionFound)
        {
            Debug.LogWarning($"Could not find valid spawn position after {m_MaxSpawnAttempts} attempts. Using last attempted position.");
        }

        return spawnPosition;
    }

    private bool IsPositionValid(Vector3 position)
    {
        // Check distance to all active packages
        foreach (Package package in m_ActivePackages)
        {
            if (package != null)
            {
                // Calculate horizontal distance (ignore Y axis)
                Vector3 packagePos = package.transform.position;
                float distance = Vector2.Distance(
                    new Vector2(position.x, position.z),
                    new Vector2(packagePos.x, packagePos.z)
                );

                if (distance < m_MinDistanceBetweenPackages)
                {
                    return false;
                }
            }
        }

        return true;
    }
    
    private void DrawCircle(Vector3 center, float radius, int segments = 32)
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
}