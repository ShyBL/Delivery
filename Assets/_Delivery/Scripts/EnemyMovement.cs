using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls enemy movement and target selection.
/// Each enemy is assigned one package at spawn time by EnemySpawner.
/// If that package is gone, the enemy searches the known list for the
/// next unclaimed package. If none remain it idles.
///
/// Uses direct Rigidbody movement (no NavMesh) since enemies spawn
/// close to their targets in an open drop zone — NavMesh would be overkill.
/// Mirrors the single-responsibility pattern of TankMovement in Tanks.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Movement speed in units per second.")]
    public float m_MoveSpeed = 3f;

    [Tooltip("Rotation speed in degrees per second.")]
    public float m_RotationSpeed = 120f;

    [Header("Target Search")]
    [Tooltip("How often (in seconds) the enemy re-evaluates its target. " +
             "Searching every frame is wasteful — 0.5s is imperceptible.")]
    public float m_UpdateTargetInterval = 0.5f;

    // The package this enemy was assigned at spawn time by EnemySpawner
    private Transform m_AssignedTarget;

    // What the enemy is actually chasing right now.
    // Starts as m_AssignedTarget; may switch to a fallback after target is destroyed.
    private Transform m_CurrentTarget;

    // Full package list supplied by EnemySpawner — used for fallback searching
    private List<GameObject> m_KnownPackages = new List<GameObject>();

    private Rigidbody m_Rigidbody;
    private float m_UpdateTargetTimer = 0f;

    private void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();

        // Kinematic — we drive position ourselves via MovePosition, not physics forces.
        // Same pattern as TankMovement in Tanks.
        m_Rigidbody.isKinematic = true;
    }

    private void Start()
    {
        // Apply the assigned target as the current chase target
        m_CurrentTarget = m_AssignedTarget;
    }

    private void Update()
    {
        // Periodically re-evaluate in case the current target was destroyed
        m_UpdateTargetTimer += Time.deltaTime;

        if (m_UpdateTargetTimer >= m_UpdateTargetInterval)
        {
            m_UpdateTargetTimer = 0f;
            ValidateCurrentTarget();
        }
    }

    private void FixedUpdate()
    {
        if (m_CurrentTarget == null) return;

        RotateTowardTarget();
        MoveForward();
    }

    // ==================== MOVEMENT ====================

    private void RotateTowardTarget()
    {
        Vector3 toTarget = m_CurrentTarget.position - m_Rigidbody.position;
        toTarget.y = 0f; // Ignore vertical difference — stay on the horizontal plane

        if (toTarget.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(toTarget);
        Quaternion newRotation = Quaternion.RotateTowards(
            m_Rigidbody.rotation,
            targetRotation,
            m_RotationSpeed * Time.fixedDeltaTime
        );

        m_Rigidbody.MoveRotation(newRotation);
    }

    private void MoveForward()
    {
        Vector3 movement = m_Rigidbody.rotation * Vector3.forward
                           * m_MoveSpeed * Time.fixedDeltaTime;

        m_Rigidbody.MovePosition(m_Rigidbody.position + movement);
    }

    // ==================== TARGET MANAGEMENT ====================

    /// <summary>
    /// Checks whether the current target still exists.
    /// If it was destroyed, triggers a fallback search.
    /// </summary>
    private void ValidateCurrentTarget()
    {
        if (m_CurrentTarget != null) return;

        FindNextUnclaimedPackage();
    }

    /// <summary>
    /// Searches m_KnownPackages for the nearest unclaimed package and claims it.
    ///
    /// Called:
    ///   - When the assigned target is gone (via ValidateCurrentTarget)
    ///   - Immediately after EnemyDamage destroys a package (for instant retarget)
    ///
    /// Public so EnemyDamage can call it without needing to know how targeting works.
    /// </summary>
    public void FindNextUnclaimedPackage()
    {
        m_KnownPackages.RemoveAll(p => p == null);

        GameObject nearest = null;
        float nearestDist = float.MaxValue;

        foreach (GameObject packageGO in m_KnownPackages)
        {
            if (packageGO == null) continue;

            Package package = packageGO.GetComponent<Package>();

            if (package == null) continue;
            if (package.IsClaimed()) continue;

            float dist = Vector3.Distance(transform.position, packageGO.transform.position);

            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = packageGO;
            }
        }

        if (nearest != null)
        {
            nearest.GetComponent<Package>().Claim();
            m_CurrentTarget = nearest.transform;
            Debug.Log($"Enemy '{gameObject.name}': retargeted to '{nearest.name}'.");
        }
        else
        {
            m_CurrentTarget = null;
            Debug.Log($"Enemy '{gameObject.name}': no unclaimed packages remain. Idling.");
        }
    }

    // ==================== SETUP (called by EnemySpawner) ====================

    /// <summary>
    /// Assigns the specific package this enemy should hunt first.
    /// Called by EnemySpawner immediately after instantiation, before Start runs.
    /// Passing null is valid — enemy will idle until a package becomes available.
    /// </summary>
    public void SetAssignedTarget(Transform target)
    {
        m_AssignedTarget = target;

        // If Start has already run, apply immediately
        m_CurrentTarget = target;
    }

    /// <summary>
    /// Provides the complete list of active packages so the enemy can
    /// find a fallback target if its assigned one is destroyed.
    /// Called by EnemySpawner immediately after instantiation.
    /// </summary>
    public void SetPackages(List<GameObject> packages)
    {
        m_KnownPackages = packages;
    }

    // ==================== GIZMOS ====================

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (m_CurrentTarget == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position + Vector3.up * 0.5f,
                        m_CurrentTarget.position + Vector3.up * 0.5f);
        Gizmos.DrawWireSphere(m_CurrentTarget.position, 0.4f);
    }
}