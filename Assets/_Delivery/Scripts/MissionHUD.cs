using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MissionHUD : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text showing packages delivered (e.g., '5/10 Packages')")]
    public TextMeshProUGUI m_PackageProgressText;
        
    [Tooltip("Text showing time remaining (e.g., '2:34')")]
    public TextMeshProUGUI m_TimeRemainingText;
        
    [Tooltip("Optional: Progress bar showing package completion")]
    public Slider m_ProgressBar;
        
    [Tooltip("Optional: Image that fills as progress increases")]
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

    [Header("Animation")]
    [Tooltip("Flash the time text when in critical state")]
    public bool m_FlashOnCritical = true;
        
    [Tooltip("Flash speed in flashes per second")]
    public float m_FlashSpeed = 2f;

    // Private variables
    private GameManager m_GameManager;
    private float m_FlashTimer = 0f;
    private bool m_IsFlashVisible = true;

    /// <summary>
    /// Set the package progress
    /// </summary>
    public void SetPackageProgress(int delivered, int target)
    {
        if (m_PackageProgressText != null)
        {
            m_PackageProgressText.text = $"{delivered}/{target} Packages";
        }

        if (m_ProgressBar != null)
        {
            m_ProgressBar.maxValue = target;
            m_ProgressBar.value = delivered;
        }
    }
    
    private void Awake()
    {
        // Find the GameManager
        m_GameManager = FindAnyObjectByType<GameManager>();
            
        if (m_GameManager == null)
        {
            Debug.LogError("MissionHUD: No GameManager found in scene!");
        }
        else
        {
            m_GameManager.HUD = this;
        }
        
        // Setup progress bar if assigned
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

    private void Update()
    {
        if (m_GameManager == null) return;

        UpdateTimeRemaining();
    }

    /// <summary>
    /// Update the package progress display
    /// </summary>
    private void UpdatePackageProgress()
    {
        int delivered = m_GameManager.GetDeliveryCount();
        int target = m_GameManager.m_TotalPackagesToDeliver;

        // Update text
        if (m_PackageProgressText != null)
        {
            m_PackageProgressText.text = $"{delivered}/{target} Packages";
        }

        // Update progress bar
        if (m_ProgressBar != null)
        {
            m_ProgressBar.maxValue = target;
            m_ProgressBar.value = delivered;
        }
    }

    /// <summary>
    /// Update the time remaining display
    /// </summary>
    private void UpdateTimeRemaining()
    {
        if (m_TimeRemainingText == null) return;

        // Calculate time remaining
        float timeLimit = m_GameManager.m_MissionTimeLimit;
        float elapsed = m_GameManager.m_MissionTimer;
            
        // This is a simplified version - GameManager should expose current mission time
        float remaining = Mathf.Max(0, timeLimit - elapsed);

        // Format time as MM:SS
        int minutes = Mathf.FloorToInt(remaining / 60f);
        int seconds = Mathf.FloorToInt(remaining % 60f);
        m_TimeRemainingText.text = $"{minutes:00}:{seconds:00}";

        // Update color based on time remaining
        UpdateTimeColor(remaining);
    }

    /// <summary>
    /// Update the time text color based on remaining time
    /// </summary>
    private void UpdateTimeColor(float timeRemaining)
    {
        if (m_TimeRemainingText == null) return;

        Color targetColor;

        if (timeRemaining > 60f)
        {
            // Normal time
            targetColor = m_TimeNormalColor;
            m_TimeRemainingText.color = targetColor;
        }
        else if (timeRemaining > 30f)
        {
            // Low time
            targetColor = m_TimeLowColor;
            m_TimeRemainingText.color = targetColor;
        }
        else
        {
            // Critical time
            targetColor = m_TimeCriticalColor;
                
            // Flash if enabled
            if (m_FlashOnCritical)
            {
                m_FlashTimer += Time.deltaTime * m_FlashSpeed;
                    
                if (m_FlashTimer >= 1f)
                {
                    m_FlashTimer = 0f;
                    m_IsFlashVisible = !m_IsFlashVisible;
                }

                m_TimeRemainingText.color = m_IsFlashVisible ? targetColor : m_TimeNormalColor;
            }
            else
            {
                m_TimeRemainingText.color = targetColor;
            }
        }
    }


}