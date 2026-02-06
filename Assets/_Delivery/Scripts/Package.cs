using System;
using UnityEngine;

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
    [Tooltip("Particle effect to emit when this package is collected.")]
    [SerializeField] private ParticleSystem m_CollectFX;
    
    [Tooltip("The visual model of the package (will rotate automatically).")]
    [SerializeField] private GameObject m_PackageModel;

    [Tooltip("Rotation speed in degrees per second.")]
    [SerializeField] private float m_RotationSpeed = 50f;
    
    private PackageSpawner m_Spawner;
    private GameManager m_GameManager;
    private Player m_Player;
    
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
        if (other.TryGetComponent(out Player player))
        {
            m_Player = player;
            _rotate = false;
            CollectPackage();
        }
    }

    private void CollectPackage()
    {
        if (m_Player != null)
        {
            m_Player.OnPackageCollected();
        }
        
        if (m_Spawner != null)
        {
            m_Spawner.OnPackageCollected();
        }

        if (m_GameManager != null)
        {
            m_GameManager.RegisterDelivery();
        }

        if (m_CollectFX != null)
        {
            Instantiate(m_CollectFX, transform.position, Quaternion.identity);
        }
        
        Destroy(gameObject,1f);
    }

    // Called by PackageSpawner to set the reference
    public void SetSpawner(PackageSpawner spawner, GameManager gameManager)
    {
        m_Spawner = spawner;
        m_GameManager = gameManager;
    }
    
    public PackageType GetPackageType()
    {
        return m_PackageType;
    }
}