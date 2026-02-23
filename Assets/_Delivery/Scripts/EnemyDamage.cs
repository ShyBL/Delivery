using UnityEngine;

/// <summary>
/// Handles what happens when an enemy touches a package.
/// Tells the package to destroy itself, plays effects, then tells
/// EnemyMovement to find its next target.
///
/// Kept separate from EnemyMovement following the Tanks pattern of splitting
/// movement and damage into distinct components (TankMovement / TankHealth).
/// </summary>
public class EnemyDamage : MonoBehaviour
{
    [Header("Effects")]
    [Tooltip("Optional particle effect spawned at the package position on contact.")]
    public ParticleSystem m_DestroyPackageFX;

    [Tooltip("Optional audio clip played when a package is destroyed.")]
    public AudioClip m_DestroySound;

    private EnemyMovement m_Movement;
    private AudioSource m_AudioSource;

    private void Awake()
    {
        m_Movement = GetComponent<EnemyMovement>();
        m_AudioSource = GetComponent<AudioSource>();

        if (m_Movement == null)
            Debug.LogError($"EnemyDamage on '{gameObject.name}': missing EnemyMovement component.");
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only react to packages
        if (!other.TryGetComponent(out Package package)) return;

        // Play effects at the package's position before it's destroyed
        if (m_DestroyPackageFX != null)
            Instantiate(m_DestroyPackageFX, other.transform.position, Quaternion.identity);

        if (m_DestroySound != null && m_AudioSource != null)
            m_AudioSource.PlayOneShot(m_DestroySound);

        // Package owns its own death — we don't call Destroy directly.
        // Same reasoning as TankHealth.TakeDamage() in Tanks.
        package.DestroyPackage();

        // Immediately ask movement to find the next target rather than
        // waiting for the next update interval tick.
        m_Movement?.FindNextUnclaimedPackage();
    }
}