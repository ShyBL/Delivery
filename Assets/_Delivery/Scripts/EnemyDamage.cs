using UnityEngine;
using UnityEngine.Events;

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
    [Tooltip("Optional effect at the package position on contact.")]
    [SerializeField] public UnityEvent OnTrigger;
    [SerializeField] public UnityEvent OnEnd;
    
    private EnemyMovement m_Movement;

    private void Awake()
    {
        m_Movement = GetComponent<EnemyMovement>();

        if (m_Movement == null)
            Debug.LogError($"EnemyDamage on '{gameObject.name}': missing EnemyMovement component.");
    }

    private void OnTriggerStay(Collider other)
    {
        // Only react to packages
        if (!other.TryGetComponent(out Package package)) return;

        // Play effects at the package's position before it's destroyed
        OnTrigger.Invoke();

        // Package owns its own death — we don't call Destroy directly.
        // Same reasoning as TankHealth.TakeDamage() in Tanks.
        package.DestroyPackage();

        // Immediately ask movement to find the next target rather than
        // waiting for the next update interval tick.
        m_Movement?.FindNextUnclaimedPackage();
        
        Invoke("OnEndDelay",1f);
    }

    private void OnEndDelay()
    {
        OnEnd.Invoke();
    }
}