// ============================================================
//  ResourceSpawner.cs
//  Place in: Assets/_Delivery/Scripts/
//  Layer   : Controller — MonoBehaviour, inherits BaseSpawner
//
//  Manages one ResourceType for a level.
//  On SpawnForLocation(), reads the abundance for its type from
//  the LocationSO and activates that many nodes, scattering
//  their positions using BaseSpawner.GetRandomSpawnPosition().
//
//  Abundance -> active node count:
//    0 (none)     -> 0 nodes
//    1 (scarce)   -> 1 node
//    2 (moderate) -> 2 nodes
//    3 (abundant) -> 3 nodes
//
//  When a node fires OnDepleted, this spawner reactivates it
//  at a new scattered position.
//
//  Attach to: a ResourceSpawner GameObject in the level scene
//  Inspector : set ResourceType, assign ResourceNodeMB refs
// ============================================================

using System.Collections.Generic;
using UnityEngine;

public class ResourceSpawner : BaseSpawner
{
    [Header("Spawner Type")]
    [Tooltip("This spawner only activates nodes of this type.")]
    [SerializeField] private ResourceType m_ResourceType;

    [Header("Nodes")]
    [Tooltip("Assign the ResourceNodeMB GameObjects placed in the scene.")]
    [SerializeField] private List<ResourceNodeMB> m_Nodes;

    private LocationSO m_CurrentLocation;

    // -------------------------------------------------------
    //  Public API  (called by GameManager)
    // -------------------------------------------------------

    /// Reads abundance for this spawner's type and activates nodes accordingly.
    public void SpawnForLocation(LocationSO location)
    {
        m_CurrentLocation = location;
        m_SpawnCenter     = transform.position;

        DeactivateAllNodes();

        int abundance   = m_CurrentLocation.GetAvailability(m_ResourceType);
        int activeCount = Mathf.Clamp(abundance, 0, m_Nodes.Count);

        if (activeCount == 0)
        {
            Debug.Log($"[ResourceSpawner] {m_ResourceType}: abundance 0 — no nodes activated.");
            return;
        }

        for (int i = 0; i < activeCount; i++)
        {
            Vector3 position = GetRandomSpawnPosition();
            ActivateNode(m_Nodes[i], position);
            m_ActiveEntities.Add(m_Nodes[i].gameObject);
        }

        Debug.Log($"[ResourceSpawner] {m_ResourceType}: activated {activeCount} node(s) " +
                  $"(abundance {abundance}).");
    }

    public void ClearNodes()
    {
        foreach (var node in m_Nodes)
        {
            if (node != null)
                node.OnDepleted -= GetDepletedHandler(node);
        }

        DeactivateAllNodes();
        m_ActiveEntities.Clear();
    }

    // -------------------------------------------------------
    //  Respawn on depletion
    // -------------------------------------------------------

    // Stores one named handler per node so subscribe/unsubscribe work correctly
    private readonly Dictionary<ResourceNodeMB, System.Action> m_DepletedHandlers
        = new Dictionary<ResourceNodeMB, System.Action>();

    private System.Action GetDepletedHandler(ResourceNodeMB node)
    {
        if (!m_DepletedHandlers.TryGetValue(node, out System.Action handler))
        {
            handler = () => OnNodeDepleted(node);
            m_DepletedHandlers[node] = handler;
        }
        return handler;
    }

    private void ActivateNode(ResourceNodeMB node, Vector3 position)
    {
        // Always unsubscribe first to avoid stacking handlers across activations
        node.OnDepleted -= GetDepletedHandler(node);
        node.Activate(position);
        node.OnDepleted += GetDepletedHandler(node);
    }

    private void OnNodeDepleted(ResourceNodeMB node)
    {
        m_ActiveEntities.RemoveAll(e => e == null);

        Vector3 newPosition = GetRandomSpawnPosition();
        ActivateNode(node, newPosition);

        Debug.Log($"[ResourceSpawner] {m_ResourceType} node respawned at {newPosition}.");
    }

    // -------------------------------------------------------
    //  BaseSpawner implementation
    // -------------------------------------------------------

    /// Not used — ResourceSpawner activates existing scene nodes
    /// rather than instantiating new ones.
    protected override void SpawnEntity() { }

    // -------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------

    private void DeactivateAllNodes()
    {
        foreach (var node in m_Nodes)
        {
            if (node != null)
                node.Deactivate();
        }
    }
}