using System.Collections;
using UnityEngine;

/// <summary>
/// Orchestrates the delivery scenario for one zone:
///   1. Waits m_VehicleSpawnDelay seconds.
///   2. Instantiates the vehicle prefab at m_VehicleStartPoint.
///   3. Lerps the vehicle to a random stop point inside the zone bounds.
///   4. Triggers EnemySpawner, and triggers PackageSpawner at the stop position.
///   5. Vehicle remains in place for the duration of the scenario.
///
/// The vehicle prefab needs no scripts — this zone drives it entirely.
/// Optionally add a child Transform named "PackageDropPoint" to offset spawn position.
/// </summary>
public class DeliveryZone : MonoBehaviour
{
    [Header("Zone Bounds")]
    [Tooltip("MeshFilter of a road or floor mesh whose world-space bounds define the zone. " +
             "Drag the MeshFilter component here (not the GameObject).")]
    public MeshFilter m_ZoneBoundsMesh;

    [Tooltip("Fallback zone size used when no mesh is assigned. Centred on this GameObject.")]
    public Vector3 m_ManualBoundsSize = new Vector3(20f, 2f, 20f);

    [Header("Vehicle")]
    [Tooltip("Visual vehicle prefab (cube for prototype, no scripts required). " +
             "Optionally add a child Transform named 'PackageDropPoint' to offset spawn.")]
    public GameObject m_VehiclePrefab;

    [Tooltip("Transform where the vehicle enters from. Place this outside the zone bounds.")]
    public Transform m_VehicleStartPoint;

    [Tooltip("Speed at which this zone moves the vehicle (units per second).")]
    public float m_VehicleMoveSpeed = 5f;

    [Tooltip("Seconds after Start before the scenario begins.")]
    public float m_VehicleSpawnDelay = 5f;

    [Tooltip("If true the scenario loops after the vehicle leaves. False = runs once.")]
    public bool m_RepeatScenario = false;
    
    [Tooltip("EnemySpawner child of this zone.")]
    public EnemySpawner m_EnemySpawner;

    #region PRIVATE VARIABLES
    
    private Bounds m_ZoneBounds; // Calculated zone bounds (mesh or manual)
    private GameObject m_CurrentVehicle;  // The live vehicle instance being driven by this zone
    private PackageSpawner m_PackageSpawner;
    private bool m_HasSpawned = false; // Whether the first scenario cycle has run
    private float m_SpawnTimer = 0f; // Countdown timer until next vehicle spawn
    
    #endregion

    #region UNITY LIFECYCLE

    private void Awake()
    {
        CalculateZoneBounds();
        ValidateReferences();
    }

    private void Start()
    {
        m_SpawnTimer = 0f;
    }

    private void Update()
    {
        // Don't repeat if the flag is off and we've already run once
        if (m_HasSpawned && !m_RepeatScenario) return;

        // Wait until there is no active vehicle before starting a new cycle
        if (m_CurrentVehicle != null) return;

        m_SpawnTimer += Time.deltaTime;

        if (m_SpawnTimer >= m_VehicleSpawnDelay)
        {
            m_SpawnTimer = 0f;
            m_HasSpawned = true;
            StartCoroutine(VehicleSequence());
        }
    }
    
    #endregion

    #region SCENARIO SEQUENCE

    /// <summary>
    /// Full coroutine driving the vehicle through its lifecycle.
    /// The zone is the only thing that touches the vehicle's transform.
    /// </summary>
    private IEnumerator VehicleSequence()
    {
        // Spawn vehicle
        m_CurrentVehicle = Instantiate(
            m_VehiclePrefab,
            m_VehicleStartPoint.position,
            Quaternion.identity
        );
        
        // Pick a random stop point inside the zone
        Vector3 stopPoint = GetRandomPointInBounds();

        // Drive vehicle in
        yield return StartCoroutine(
            MoveVehicle(m_CurrentVehicle.transform, stopPoint)
        );
        
        // Trigger spawners at the drop point
        m_PackageSpawner = m_CurrentVehicle.GetComponentInChildren<PackageSpawner>();
        
        Transform dropPoint = m_PackageSpawner.transform;
        Vector3 spawnOrigin = dropPoint != null ? dropPoint.position : stopPoint;
        
        m_PackageSpawner?.TriggerSpawn();
        m_EnemySpawner?.TriggerSpawn(m_PackageSpawner);

        Debug.Log($"DeliveryZone '{gameObject.name}': vehicle stopped. Spawners triggered at {spawnOrigin}.");
    }

    /// <summary>
    /// Lerps a Transform from its current position to target over time.
    /// Rotates the vehicle to face the direction of travel.
    /// </summary>
    private IEnumerator MoveVehicle(Transform vehicle, Vector3 target)
    {
        Vector3 origin = vehicle.position;
        float distance = Vector3.Distance(origin, target);
        float duration = distance / m_VehicleMoveSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (vehicle == null) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            vehicle.position = Vector3.Lerp(origin, target, t);

            // Rotate to face movement direction
            Vector3 dir = (target - vehicle.position).normalized;
            if (dir.sqrMagnitude > 0.001f)
                vehicle.rotation = Quaternion.LookRotation(dir);

            yield return null;
        }

        if (vehicle != null)
            vehicle.position = target;
    }
    
    #endregion

    #region BOUNDS
    
    private void CalculateZoneBounds()
    {
        if (m_ZoneBoundsMesh != null && m_ZoneBoundsMesh.sharedMesh != null)
        {
            Bounds local = m_ZoneBoundsMesh.sharedMesh.bounds;
            Matrix4x4 local2World = m_ZoneBoundsMesh.transform.localToWorldMatrix;
            m_ZoneBounds = new Bounds(
                local2World.MultiplyPoint3x4(local.center),
                local2World.MultiplyVector(local.size)
            );
        }
        else
        {
            m_ZoneBounds = new Bounds(transform.position, m_ManualBoundsSize);
        }
    }

    private Vector3 GetRandomPointInBounds()
    {
        return new Vector3(
            Random.Range(m_ZoneBounds.min.x, m_ZoneBounds.max.x),
            m_ZoneBounds.center.y,
            Random.Range(m_ZoneBounds.min.z, m_ZoneBounds.max.z)
        );
    }

    /// <summary>
    /// Checks whether the given world position is inside this zone's bounds.
    /// </summary>
    public bool IsInZone(Vector3 position) => m_ZoneBounds.Contains(position);
    
    /// <summary>
    /// Returns the calculated world-space bounds of this delivery zone.
    /// </summary>
    public Bounds GetZoneBounds() => m_ZoneBounds;

    
    #endregion
    
    #region VALIDATION
    
    private void ValidateReferences()
    {
        if (m_VehiclePrefab == null)
            Debug.LogError($"DeliveryZone '{gameObject.name}': m_VehiclePrefab is not assigned.");

        if (m_VehicleStartPoint == null)
        {
            Debug.LogWarning($"DeliveryZone '{gameObject.name}': m_VehicleStartPoint not assigned. " +
                             "Creating a default one to the left of the zone.");

            GameObject fallback = new GameObject("DefaultVehicleStartPoint");
            fallback.transform.SetParent(transform);
            fallback.transform.position = m_ZoneBounds.center
                                          + new Vector3(-m_ZoneBounds.extents.x - 5f, 0f, 0f);
            m_VehicleStartPoint = fallback.transform;
        }

        if (m_PackageSpawner == null)
            Debug.LogWarning($"DeliveryZone '{gameObject.name}': m_PackageSpawner not assigned.");

        if (m_EnemySpawner == null)
            Debug.LogWarning($"DeliveryZone '{gameObject.name}': m_EnemySpawner not assigned.");
    }

    #endregion
    
    #region PUBLIC CONTROL

    /// <summary>
    /// Stops the current scenario, destroys the vehicle, and clears all spawned entities.
    /// Resets timers so the scenario can start again.
    /// </summary>
    public void ResetScenario()
    {
        StopAllCoroutines();

        if (m_CurrentVehicle != null)
        {
            Destroy(m_CurrentVehicle);
            m_CurrentVehicle = null;
        }

        m_PackageSpawner?.CleanupEntities();
        m_EnemySpawner?.CleanupEntities();

        m_HasSpawned = false;
        m_SpawnTimer = 0f;
    }


    #endregion

    #region GIZMOS
    
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            CalculateZoneBounds();

        // Zone bounds
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.DrawWireCube(m_ZoneBounds.center, m_ZoneBounds.size);

        // Start point
        if (m_VehicleStartPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(m_VehicleStartPoint.position, 0.8f);
            Gizmos.DrawLine(m_VehicleStartPoint.position, m_ZoneBounds.center);
        }

        // Live vehicle position
        if (Application.isPlaying && m_CurrentVehicle != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(m_CurrentVehicle.transform.position, 0.5f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            CalculateZoneBounds();

        Gizmos.color = new Color(0f, 1f, 0f, 0.08f);
        Gizmos.DrawCube(m_ZoneBounds.center, m_ZoneBounds.size);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            m_ZoneBounds.center + Vector3.up * (m_ZoneBounds.extents.y + 1f),
            $"{gameObject.name}\nDelay: {m_VehicleSpawnDelay}s | Repeat: {m_RepeatScenario}"
        );
#endif
    }
    
    #endregion
}