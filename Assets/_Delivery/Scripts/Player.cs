using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the player-controlled delivery bot.
/// Tracks collected packages, handles collection events, and manages player control state.
/// </summary>
public class Player : MonoBehaviour
{
    public RobotAnimate robotAnimate;
    public RobotMove robotMove;
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
    public void OnPackageCollected()
    {
        RegisterPickup();
        robotAnimate.AnimatePickup();
    }
    
    /// <summary>
    /// Enables or disables player movement control.
    /// </summary>
    /// <param name="value">True to enable control, false to disable.</param>
    public void ToggleControl(bool value)
    {
        if (robotMove != null)
            robotMove.enabled = value;
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


