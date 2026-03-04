using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider))]
public class Teleport : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Teleport destinationTeleporter;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float teleportCooldown = 0.5f;
    [SerializeField] private bool isMasterHub = false;

    [Header("Events")]
    [SerializeField] private UnityEvent onTeleport;

    private BoxCollider m_collider;
    private bool m_isTeleporting = false;
    private Coroutine m_teleportCoroutine;
    private Coroutine m_cooldownCoroutine;

    private void Awake()
    {
        m_collider = GetComponent<BoxCollider>();
    }

    private void OnDisable()
    {
        StopTeleport();
        m_isTeleporting = false;

        // Restore collider in case we were disabled mid-cooldown
        if (m_collider != null)
            m_collider.enabled = true;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent(out Player player))
            TryTeleport(player);
    }

    private void TryTeleport(Player player)
    {
        if (m_isTeleporting || destinationTeleporter == null) return;

        if (!isMasterHub) UpdateMasterHub();

        StopTeleport();
        m_teleportCoroutine = StartCoroutine(PerformTeleport(player));
    }

    private IEnumerator PerformTeleport(Player player)
    {
        m_isTeleporting = true;
        player.enabled = false;

        try
        {
            Transform target = destinationTeleporter.spawnPoint != null
                ? destinationTeleporter.spawnPoint
                : destinationTeleporter.transform;

            player.transform.position = target.position;

            // Wait one physics frame for the engine to settle the new position
            yield return null;

            onTeleport.Invoke();
        }
        finally
        {
            player.enabled = true;
            m_isTeleporting = false;
            m_teleportCoroutine = null;

            m_cooldownCoroutine = StartCoroutine(CooldownRoutine());
        }
    }

    private IEnumerator CooldownRoutine()
    {
        m_collider.enabled = false;
        yield return new WaitForSeconds(teleportCooldown);
        m_collider.enabled = true;
        m_cooldownCoroutine = null;
    }

    private void StopTeleport()
    {
        if (m_teleportCoroutine != null)
        {
            StopCoroutine(m_teleportCoroutine);
            m_teleportCoroutine = null;
        }

        if (m_cooldownCoroutine != null)
        {
            StopCoroutine(m_cooldownCoroutine);
            m_cooldownCoroutine = null;
        }
    }

    private void UpdateMasterHub()
    {
        Teleport[] allTeleporters = FindObjectsByType<Teleport>(FindObjectsSortMode.None);
        foreach (var tp in allTeleporters)
        {
            if (tp.isMasterHub)
            {
                tp.destinationTeleporter = this;
                break;
            }
        }
    }

    #region GIZMOS
    private void OnDrawGizmos()
    {
        if (m_collider == null) m_collider = GetComponent<BoxCollider>();
        Color mainColor = isMasterHub ? Color.cyan : Color.green;

        Gizmos.color = mainColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(m_collider.center, m_collider.size);
        Gizmos.matrix = Matrix4x4.identity;

        if (spawnPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
            Gizmos.DrawLine(transform.position, spawnPoint.position);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (m_collider == null) m_collider = GetComponent<BoxCollider>();
        Gizmos.color = isMasterHub ? new Color(0, 1, 1, 0.1f) : new Color(0, 1, 0, 0.1f);

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(m_collider.center, m_collider.size);
        Gizmos.matrix = Matrix4x4.identity;

        if (destinationTeleporter != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, destinationTeleporter.transform.position);
        }
    }
    #endregion
}