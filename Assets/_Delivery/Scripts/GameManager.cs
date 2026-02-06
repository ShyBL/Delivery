using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // Which state the game is currently in
    public enum GameState
    {
        MainMenu,
        Game
    }
    
    public static GameManager instance;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            
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
    
    private GameState m_CurrentState;
    private WaitForSeconds m_StartWait;
    private WaitForSeconds m_EndWait;
    public float m_StartDelay = 3f;             // The delay between the start of RoundStarting and RoundPlaying phases.
    public float m_EndDelay = 3f;               // The delay between the end of RoundPlaying and RoundEnding phases.
    
    [Header("Delivery Mission Settings")]
    public int m_TotalPackagesToDeliver = 10;       // Total packages needed to complete mission
    public float m_MissionTimeLimit = 300f;         // 5 minutes mission time limit
    
    private int m_PackagesDelivered = 0;            // Current number of packages delivered
    public float m_MissionTimer = 0f;              // Current mission elapsed time

    public MissionHUD HUD;
    private void Start()
    {
        StartGame();

    }
    
    // Called by the menu
    public void StartGame()
    {
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

    
    // This is called from start and will run each phase of the game one after another.

    private IEnumerator GameLoop()
    {
        // Start the mission
        yield return StartCoroutine(MissionStarting());

        // Run the active mission phase
        yield return StartCoroutine(MissionActive());

        // End the mission and show results
        yield return StartCoroutine(MissionEnding());

        // Restart the level after mission completion
        SceneManager.LoadScene(0);
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
        
        while (!MissionComplete() && !MissionFailed())
        {
            m_MissionTimer += Time.deltaTime;
            yield return null;
        }
    }
    
    
    private IEnumerator MissionEnding()
    {
        Debug.Log("Ending Mission");
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
    
    public void RegisterDelivery()
    {
        m_PackagesDelivered++;
        HUD.SetPackageProgress(m_PackagesDelivered, m_TotalPackagesToDeliver);
    }

    public int GetDeliveryCount()
    {
        return m_PackagesDelivered;
    }


}