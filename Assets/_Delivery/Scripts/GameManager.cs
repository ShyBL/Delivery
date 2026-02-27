using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the game state and mission flow for the Delivery game.
/// Starts in MainMenu state and waits for DebugWindow to call StartGame().
/// After mission completion (win or lose), reloads the scene and returns to MainMenu state.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    
    private GameState m_CurrentState;                       // State the game is currently in
    private WaitForSeconds m_StartWait;                     
    private WaitForSeconds m_EndWait;
    public float m_StartDelay = 3f;                         // The delay between the start of RoundStarting and RoundPlaying phases.
    public float m_EndDelay = 3f;                           // The delay between the end of RoundPlaying and RoundEnding phases.
    
    [Header("Delivery Mission Settings")]
    public int m_TotalPackagesToDeliver = 10;               // Total packages needed to complete mission
    public float m_MissionTimeLimit = 300f;                 // 5 minutes mission time limit
    
    private int m_PackagesDelivered = 0;                    // Current number of packages delivered
    public float m_MissionTimer = 0f;                       // Current mission elapsed time

    public MissionHUD HUD;
    public Player player;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            player = FindFirstObjectByType<Player>();
            m_CurrentState = GameState.MainMenu;
            m_StartWait = new WaitForSeconds (m_StartDelay);
            m_EndWait = new WaitForSeconds (m_EndDelay); 
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        // Freeze the game until DebugWindow calls StartGame()
        Time.timeScale = 0f;
        Debug.Log("GameManager: Game PAUSED in MainMenu state. Configure settings and click 'Apply to Scene' to start.");
    }
    
    /// <summary>
    /// Called by DebugWindow when "Apply to Scene" is clicked.
    /// Transitions from MainMenu to active game state and unfreezes time.
    /// </summary>
    public void StartGame()
    {
        // Unfreeze time when starting the game
        Time.timeScale = 1f;
        ChangeGameState(GameState.Game);
    }

    private void ChangeGameState(GameState newState)
    {
        m_CurrentState = newState;

        switch (m_CurrentState)
        {
            case GameState.Game:
                GameStart();
                break;
        }
    }

    private void GameStart()
    {
        // Create the delays so they only have to be made once.
        m_StartWait = new WaitForSeconds (m_StartDelay);
        m_EndWait = new WaitForSeconds (m_EndDelay);
        
        StartCoroutine (GameLoop());
    }
    
    /// <summary>
    /// This is called from start and will run each phase of the game one after another.
    /// </summary>
    private IEnumerator GameLoop()
    {
        // Start the mission
        yield return StartCoroutine(MissionStarting());

        // Run the active mission phase
        yield return StartCoroutine(MissionActive());

        // End the mission and show results
        yield return StartCoroutine(MissionEnding());

        // Return to MainMenu state by reloading the scene
        Debug.Log("Mission ended. Reloading scene to MainMenu state...");
        
        // Reset time scale before reload so next session starts frozen
        Time.timeScale = 0f;
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private IEnumerator MissionStarting()
    {
        Debug.Log("Starting Mission");
        yield return m_StartWait;
    }
    
    private IEnumerator MissionActive()
    {
        m_PackagesDelivered = 0;
        m_MissionTimer = 0f;
        
        Debug.Log("Mission Active - Deliver packages before time runs out!");
        
        while (!MissionComplete() && !MissionFailed())
        {
            m_MissionTimer += Time.deltaTime;
            yield return null;
        }
    }
    
    
    private IEnumerator MissionEnding()
    {
        if (MissionComplete())
        {
            Debug.Log($"MISSION SUCCESS! Delivered {m_PackagesDelivered}/{m_TotalPackagesToDeliver} packages in {m_MissionTimer:F1} seconds!");
        }
        else if (MissionFailed())
        {
            Debug.Log($"MISSION FAILED! Time limit reached. Only delivered {m_PackagesDelivered}/{m_TotalPackagesToDeliver} packages.");
        }
        
        yield return m_EndWait;
    }
    
    private bool MissionComplete()
    {
        return m_PackagesDelivered >= m_TotalPackagesToDeliver;
    }

    private bool MissionFailed()
    {
        return m_MissionTimer >= m_MissionTimeLimit;
    }
    
    /// <summary>
    /// Called by Package when collected by the player.
    /// Increments delivery count and updates HUD.
    /// </summary>
    public void RegisterDelivery()
    {
        m_PackagesDelivered++;
        player.m_Packages--;
        
        HUD?.SetPackageProgress(m_PackagesDelivered, m_TotalPackagesToDeliver);
        
        Debug.Log($"Package collected! Progress: {m_PackagesDelivered}/{m_TotalPackagesToDeliver}");
    }

    public int GetDeliveryCount()
    {
        return m_PackagesDelivered;
    }
}