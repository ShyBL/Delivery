using FMODUnity;
using UnityEngine;
using UnityEngine.UI;

public class PackagesInventory : MonoBehaviour
{
    public int m_Packages;
    public int m_TotalPackages;
    
    [Tooltip("Progress bar showing package completion")]
    public Slider m_ProgressBar;
    
    [Tooltip("Image that fills as progress increases")]
    public Image m_ProgressFillImage;
    
    [Header("Color Settings")]
    [Tooltip("Color when time is abundant (> 60s)")]
    public Color m_TimeNormalColor = Color.white;
        
    [Tooltip("Color when time is running low (< 60s)")]
    public Color m_TimeLowColor = Color.yellow;
        
    [Tooltip("Color when time is critical (< 30s)")]
    public Color m_TimeCriticalColor = Color.red;
        
    [Tooltip("Color for progress bar fill")]
    public Color m_ProgressFillColor = Color.green;
    
    [Header("Visual Settings")]
    [Tooltip("Particle effect to emit when this package is collected by the player.")]
    [SerializeField] private ParticleSystem m_CollectFX;

    [Tooltip("Sound effect to emit when this package is destroyed by an enemy.")]
    [SerializeField] private StudioEventEmitter m_CollectSFX;
        
    private void Awake()
    {
        if (m_ProgressBar != null)
        {
            m_ProgressBar.minValue = 0;
            m_ProgressBar.maxValue = 1;
            m_ProgressBar.value = 0;
                    
            if (m_ProgressFillImage != null)
            {
                m_ProgressFillImage.color = m_ProgressFillColor;
            }
                    
            UpdatePackageProgress();
        }
    }
            
    private void UpdatePackageProgress()
    {
        int packagesStoredCount = GetPackagesStoredCount();
        int totalPackagesStorage = GetPackagesStorageCount();
                
        // Update progress bar
        if (m_ProgressBar != null)
        {
            m_ProgressBar.maxValue = totalPackagesStorage;
            m_ProgressBar.value = packagesStoredCount;
        }
    }
        
    /// <summary>
    /// Called when the player collects a package.
    /// Registers the pickup and triggers the pickup animation.
    /// </summary>
    public void OnPackageCollected(Transform packagesTransform)
    {
        RegisterPickup();
        // robotAnimate.AnimatePickup();
                
        if (m_CollectSFX != null)
            m_CollectSFX.Play();
                
        if (m_CollectFX != null)
            Instantiate(m_CollectFX, packagesTransform.position, packagesTransform.rotation);
    }
            
        
            
    private void RegisterPickup()
    {
        m_Packages++;
        UpdatePackageProgress();
    }
            
    /// <summary>
    /// Returns the number of packages currently held by the player.
    /// </summary>
    public int GetPackagesStoredCount()
    {
        return m_Packages;
    }
            
    /// <summary>
    /// Returns the maximum number of packages the player can carry.
    /// </summary>
    public int GetPackagesStorageCount()
    {
        return m_TotalPackages;
    }
}