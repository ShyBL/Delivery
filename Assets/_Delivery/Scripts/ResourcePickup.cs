// ============================================================
//  ResourcePickup.cs
//  Place in: Assets/_Delivery/Scripts/
//  Layer   : View — MonoBehaviour, lives on resource prefabs
//
//  Minimal pickup script. Requires a trigger Collider on the prefab.
//  On player contact, destroys itself and notifies its parent node.
//
//  Attach to: resource prefab (one per ResourceType visual)
//  Inspector : nothing to wire — node assigns itself on spawn
// ============================================================

using UnityEngine;

public class ResourcePickup : MonoBehaviour
{
    // Set by ResourceNodeMB immediately after Instantiate
    private ResourceNodeMB m_OwnerNode;

    public void Initialise(ResourceNodeMB ownerNode)
    {
        m_OwnerNode = ownerNode;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out Player player)) return;

        player.OnResourceCollected(m_OwnerNode.ResourceType, 1);

        // Notify the node before destroying so it can update its count
        if (m_OwnerNode != null)
            m_OwnerNode.OnPickupCollected(this);

        Destroy(gameObject);
    }
}
