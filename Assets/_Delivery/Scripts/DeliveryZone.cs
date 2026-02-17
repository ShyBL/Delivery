using UnityEngine;

public class DeliveryZone : MonoBehaviour
{
    [Header("Zone Bounds")]
    [Tooltip("Mesh whose bounds define the delivery zone")]
    public MeshFilter m_ZoneBoundsMesh;
    
    [Tooltip("Manual bounds override if no mesh is assigned.")]
    public Vector3 m_ManualBoundsSize = new Vector3(20f, 2f, 20f);
    
    [Header("Vehicle Settings")]
    [Tooltip("Visual vehicle prefab. Will be moved by this zone.")]
    public GameObject m_VehiclePrefab;
    
    [Tooltip("How long to wait before spawning the vehicle (seconds).")]
    public float m_VehicleSpawnDelay = 5f;

    [Tooltip("Transform where the vehicle starts.")]
    public Transform m_VehicleStartPoint;

    [Tooltip("Speed at which the vehicle moves (units per second).")]
    public float m_VehicleMoveSpeed = 5f;
    
    [Header("Spawner References")]
    [Tooltip("Reference to the PackageSpawner in this zone.")]
    public PackageSpawnerV2 m_PackageSpawner;

    [Tooltip("Reference to the EnemySpawner in this zone.")]
    // public EnemySpawner m_EnemySpawner;
    
    // Private state
    private Bounds m_ZoneBounds;
    private GameObject m_CurrentVehicleInstance;
    private Transform m_PackageDropPoint; // Will be a child of the vehicle or the vehicle itself
    private bool m_ScenarioActive = false;
    private float m_SpawnTimer = 0f;
    private bool m_HasSpawned = false;
}