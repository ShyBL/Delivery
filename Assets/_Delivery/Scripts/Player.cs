using System;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    public RobotAnimate robotAnimate;
    public RobotMove robotMove;
    private int m_Packages;
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


    public void OnPackageCollected()
    {
        RegisterPickup();
        robotAnimate.AnimatePickup();
    }
    
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
    
    public int GetPackagesStoredCount()
    {
        return m_Packages;
    }
    
    public int GetPackagesStorageCount()
    {
        return m_TotalPackages;
    }
}


