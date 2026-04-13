// ============================================================
//  ResourceSpawner.cs
//  Place in: Assets/_Delivery/Scripts/
//  Layer   : Controller — MonoBehaviour, inherits BaseSpawner
//
//  Spawns ResourceNodeMB prefabs based on a LocationSO's
//  resource availability profile.
//
//  Abundance -> node count mapping:
//    0 (none)     -> 0 nodes
//    1 (scarce)   -> 2 nodes
//    2 (moderate) -> 4 nodes
//    3 (abundant) -> 6 nodes
//
//  When a node fires OnDepleted, this spawner respawns
//  a fresh node of any available type at a new position.
//
//  Triggered by GameManager.StartScavenging() — not DeliveryZone.
//
//  Attach to: a ResourceSpawner GameObject in the level scene
//  Inspector : assign one prefab per ResourceType +
//              assign SpawnPoint Transforms in the scene
// ============================================================

using System.Collections.Generic;
using UnityEngine;

public class ResourceSpawner : BaseSpawner
{
    [Header("Resource Node Prefabs")]
    [SerializeField] private ResourceNodeMB m_ScrapsPrefab;
    [SerializeField] private ResourceNodeMB m_RawMaterialsPrefab;
    [SerializeField] private ResourceNodeMB m_HiEndPrefab;

    [Header("Spawn Points")]
    [Tooltip("Assign empty GameObjects in the scene as candidate spawn positions.")]
    [SerializeField] private List<Transform> m_SpawnPoints;

    // Tracks spawned nodes so we can subscribe/unsubscribe OnDepleted
    private readonly List<ResourceNodeMB> m_ActiveNodes = new List<ResourceNodeMB>();
    private LocationSO m_CurrentLocation;

    #region Public API
   
    /// Clears existing nodes and spawns fresh ones for the given location.
    public void SpawnForLocation(LocationSO location)
    {
        m_CurrentLocation = location;
        ClearNodes();
        SpawnAllNodes();
    }

    public void ClearNodes()
    {
        foreach (var node in m_ActiveNodes)
        {
            if (node != null)
            {
                node.OnDepleted -= OnNodeDepleted;
                Destroy(node.gameObject);
            }
        }
        m_ActiveNodes.Clear();
        m_ActiveEntities.Clear();
    }

    #endregion

    #region Spawning Logic
    [ContextMenu("Spawn")]
    private void SpawnAllNodes()
    {
        if (m_CurrentLocation == null) return;

        var availablePoints = new List<Transform>(m_SpawnPoints);
        Shuffle(availablePoints);

        int pointIdx = 0;
        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            int abundance = m_CurrentLocation.GetAvailability(type);
            int count     = abundance * 2; // 0->0  1->2  2->4  3->6

            ResourceNodeMB prefab = GetPrefab(type);
            if (prefab == null)
            {
                Debug.LogWarning($"[ResourceSpawner] No prefab assigned for {type}.");
                continue;
            }

            for (int i = 0; i < count && pointIdx < availablePoints.Count; i++, pointIdx++)
            {
                SpawnNode(prefab, type, availablePoints[pointIdx].position);
            }
        }

        Debug.Log($"[ResourceSpawner] Spawned {m_ActiveNodes.Count} nodes for '{m_CurrentLocation.locationName}'.");
    }

    private void SpawnNode(ResourceNodeMB prefab, ResourceType type, Vector3 position)
    {
        ResourceNodeMB node = Instantiate(prefab, position, Quaternion.identity);
        node.Initialise(type, 1);
        node.OnDepleted += OnNodeDepleted;

        m_ActiveNodes.Add(node);
        m_ActiveEntities.Add(node.gameObject);
    }

    #endregion

    #region Respawning

    private void OnNodeDepleted()
    {
        m_ActiveNodes.RemoveAll(n => n == null || n.IsDepleted);
        m_ActiveEntities.RemoveAll(e => e == null);

        if (m_CurrentLocation == null) return;

        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            if (m_CurrentLocation.GetAvailability(type) == 0) continue;

            ResourceNodeMB prefab = GetPrefab(type);
            if (prefab == null) continue;

            Vector3 spawnPos = GetRandomSpawnPosition();
            SpawnNode(prefab, type, spawnPos);

            Debug.Log($"[ResourceSpawner] Respawned {type} node.");
            return;
        }
    }

    #endregion

    #region Helpers & Overrides

    protected override void SpawnEntity() { }

    private ResourceNodeMB GetPrefab(ResourceType type)
    {
        switch (type)
        {
            case ResourceType.Scraps:            return m_ScrapsPrefab;
            case ResourceType.RawMaterials:      return m_RawMaterialsPrefab;
            case ResourceType.HighEndComponents: return m_HiEndPrefab;
            default:                             return null;
        }
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j    = Random.Range(0, i + 1);
            T   tmp  = list[i];
            list[i]  = list[j];
            list[j]  = tmp;
        }
    }

    #endregion
}