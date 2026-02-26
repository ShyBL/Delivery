using UnityEngine;

/// <summary>
/// Collectible package that can be picked up by the player or destroyed by enemies.
/// Supports a claim system so enemies coordinate and don't double-target the same package.
/// Notifies the spawner on collection or destruction so the active list stays accurate.
/// </summary>
public class Package : MonoBehaviour
{
    public enum PackageType
    {
        Standard,
        Rare,
        Unique
    }

    [Header("Package Properties")]
    [Tooltip("Select the type of package.")]
    [SerializeField] private PackageType m_PackageType = PackageType.Standard;

    [Header("Visual Settings")]
    [Tooltip("Particle effect to emit when this package is collected by the player.")]
    [SerializeField] private ParticleSystem m_CollectFX;

    [Tooltip("Particle effect to emit when this package is destroyed by an enemy.")]
    [SerializeField] private ParticleSystem m_DestroyFX;

    [Tooltip("The visual model of the package (will rotate automatically).")]
    [SerializeField] private GameObject m_PackageModel;

    [Tooltip("Rotation speed in degrees per second.")]
    [SerializeField] private float m_RotationSpeed = 50f;
    
    private PackageSpawner m_Spawner; // Reference set by spawner
    private GameManager m_GameManager; // Reference set by spawner
    private bool m_IsClaimed = false;    // Claim state — used by enemy coordination system
    private bool m_IsBeingProcessed = false;  // Guard against double destruction (player collects same frame enemy reaches it)
    private bool _rotate = true;
    
    private void Update()
    {
        if (_rotate)
        {
            m_PackageModel.transform.Rotate(Vector3.forward, m_RotationSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (m_IsBeingProcessed) return;

        if (other.TryGetComponent(out Player player))
        {
            CollectPackage(player);
        }
    }

    #region COLLECTION (by player)

    private void CollectPackage(Player player)
    {
        m_IsBeingProcessed = true;

        player.OnPackageCollected();
        m_Spawner?.OnPackageCollected(gameObject);
        
        if (m_CollectFX != null)
            Instantiate(m_CollectFX, transform.position, Quaternion.identity);

        Destroy(gameObject, 0.1f);
    }
    #endregion

    #region DESTRUCTION (by enemy)

    /// <summary>
    /// Called by EnemyDamage when an enemy reaches this package.
    /// The package owns its own death so lifecycle logic stays centralised here,
    /// mirroring how TankHealth.OnDeath() is kept private in Tanks.
    /// </summary>
    public void DestroyPackage()
    {
        if (m_IsBeingProcessed) return;
        m_IsBeingProcessed = true;

        m_Spawner?.OnPackageCollected(gameObject);

        if (m_DestroyFX != null)
            Instantiate(m_DestroyFX, transform.position, Quaternion.identity);

        Destroy(gameObject, 0.1f);
    }
    #endregion

    #region CLAIM SYSTEM

    /// <summary>
    /// Mark this package as claimed by an enemy so other enemies skip it.
    /// Called by EnemySpawner during spawn-time coordination.
    /// </summary>
    public void Claim()
    {
        m_IsClaimed = true;
    }

    /// <summary>
    /// Returns whether this package has already been claimed by an enemy.
    /// Used by enemies searching for a fallback target after their assigned one is gone.
    /// </summary>
    public bool IsClaimed()
    {
        return m_IsClaimed;
    }
    #endregion

    #region SETUP

    /// <summary>
    /// Called by PackageSpawner immediately after instantiation to wire up references.
    /// </summary>
    public void SetSpawner(PackageSpawner spawner, GameManager gameManager)
    {
        m_Spawner = spawner;
        m_GameManager = gameManager;
    }

    public PackageType GetPackageType()
    {
        return m_PackageType;
    }
    #endregion

}