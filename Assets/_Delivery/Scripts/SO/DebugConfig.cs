using UnityEngine;

/// <summary>
/// ScriptableObject that stores debug configuration values for quick testing.
/// Allows designers to save and load different test scenarios without manually
/// adjusting values in the Inspector every time.
/// Create via Assets > Create > Delivery > Debug Config.
/// </summary>
[CreateAssetMenu(fileName = "DebugConfig", menuName = "Delivery/Debug Config", order = 1)]
public class DebugConfig : ScriptableObject
{
    [Header("Player Settings")]
    [Tooltip("Movement speed of the player robot in units per second.")]
    public float playerSpeed = 12f;
    
    [Tooltip("Turn speed of the player robot in degrees per second.")]
    public float playerTurnSpeed = 180f;

    [Header("Delivery Zone Settings")]
    [Tooltip("Manual bounds size for zones (when no mesh is assigned).")]
    public Vector3 zoneManualBoundsSize = new Vector3(20f, 2f, 20f);
    
    [Tooltip("Speed at which vehicles move through the zone.")]
    public float vehicleSpeed = 5f;
    
    [Tooltip("Whether zones should repeat their scenario after completion.")]
    public bool repeatScenario = false;
    
    [Tooltip("Delay in seconds before the vehicle spawns.")]
    public float spawnDelay = 5f;

    [Header("Enemy Spawner Settings")]
    [Tooltip("Radius around spawn center where enemies are placed.")]
    public float enemySpawnRadius = 5f;
    
    [Tooltip("Distance from package center where enemies spawn.")]
    public float enemySpawnRangeFromPackages = 8f;

    [Header("Game Manager Settings")]
    [Tooltip("Total number of packages player must deliver to complete mission.")]
    public int totalPackagesToDeliver = 10;
    
    [Tooltip("Mission time limit in seconds (5 minutes = 300).")]
    public float missionTimeLimit = 300f;

    [Header("Teleporter Settings")]
    [Tooltip("Cooldown time in seconds between teleporter uses.")]
    public float teleporterCooldown = 2f;
}