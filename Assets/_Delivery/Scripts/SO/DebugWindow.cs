using System;
using UnityEngine;

/// <summary>
/// Debug window that appears in-game to quickly adjust gameplay settings.
/// Uses OnGUI for immediate access without stopping play mode.
/// Window is open by default and the game is paused until "Apply to Scene" is clicked.
/// Finds all relevant objects in the scene and applies settings from a DebugConfig ScriptableObject.
/// </summary>
public class DebugWindow : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("The DebugConfig ScriptableObject containing default values.")]
    public DebugConfig m_Config;
    
    [Header("Visual Settings")]
    public Color m_BackgroundColor = new Color(0.2f, 0.2f, 0.2f, 1.0f);
    public Color m_TextColor = Color.white;
    public Color m_ButtonColor = Color.cyan;
    [Range(10, 40)]
    public int m_GlobalFontSize = 18;
    public GUIStyle m_HeaderStyle;
    public GUIStyle m_LabelStyle;
    public GUIStyle m_TextFieldStyle;
    public GUIStyle m_ButtonStyle;
    
    public KeyCode m_ToggleKey;
    
    private Rect m_WindowRect = new Rect(20, 20, 400, 1100);
    private Vector2 m_ScrollPosition = Vector2.zero;

    // Cached references to scene objects
    private Player m_Player;
    private RobotMove m_RobotMove;
    private DeliveryZone[] m_DeliveryZones;
    private EnemySpawner[] m_EnemySpawners;
    private GameManager m_GameManager;
    private Delivery m_Teleporter;

    // String fields for text input
    private string m_PlayerSpeedStr;
    private string m_PlayerTurnSpeedStr;
    private string m_ZoneBoundsSizeStr;
    private string m_VehicleSpeedStr;
    private string m_SpawnDelayStr;
    private string m_EnemySpawnRadiusStr;
    private string m_EnemySpawnRangeStr;
    private string m_TotalPackagesStr;
    private string m_MissionTimeLimitStr;
    private string m_TeleporterCooldownStr;
    private bool m_ShowWindow = true;
    private bool m_StylesInitialized = false; // Flag to prevent the error
    private void Start()
    {
        // Initialize string fields from config if available
        if (m_Config != null)
        {
            LoadFromConfig();
        }
        else
        {
            // Use default values
            InitializeDefaults();
        }
    }

    private void Update()
    {
        // Toggle window visibility
        if (Input.GetKeyDown(m_ToggleKey))
        {
            m_ShowWindow = !m_ShowWindow;
        }
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    private void OnGUI()
    {
        if (!m_ShowWindow) return;

        if (!m_StylesInitialized)
        {
            InitializeStyles();
            UpdateFontSize();
            m_StylesInitialized = true;
        }

        m_WindowRect = GUI.Window(0, m_WindowRect, DrawWindow, "Game State Menu");
    }

    private void InitializeStyles()
    {
        // Now GUI.skin is accessible!
        m_HeaderStyle = new GUIStyle(GUI.skin.box);
        m_HeaderStyle.fontStyle = FontStyle.Bold;
        m_HeaderStyle.alignment = TextAnchor.MiddleCenter;
        m_HeaderStyle.normal.textColor = m_TextColor;

        m_LabelStyle = new GUIStyle(GUI.skin.label);
        m_LabelStyle.fontStyle = FontStyle.Bold;
        m_LabelStyle.normal.textColor = m_TextColor;
        
        m_TextFieldStyle = new GUIStyle(GUI.skin.textField);
        m_TextFieldStyle.fontStyle = FontStyle.Bold;
        m_TextFieldStyle.normal.textColor = m_TextColor;
        
        m_ButtonStyle = new GUIStyle(GUI.skin.button);
        m_ButtonStyle.fontStyle = FontStyle.Bold;
        m_ButtonStyle.normal.textColor = m_TextColor;
    }

    private void UpdateFontSize()
    {
        m_HeaderStyle.fontSize = m_GlobalFontSize + 4;
        m_LabelStyle.fontSize = m_GlobalFontSize;
        m_TextFieldStyle.fontSize = m_GlobalFontSize;
        m_ButtonStyle.fontSize = m_GlobalFontSize;
        
        // Adjust window width so it doesn't look cramped when text is huge
        m_WindowRect.width = Mathf.Max(450, m_GlobalFontSize * 20); 
    }

    private void DrawWindow(int windowID)
    {
        GUILayout.BeginVertical();
        m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition);

        GUILayout.Label("Press Apply button to Play", m_HeaderStyle);
        GUILayout.Label("F1 to Close - Esc to Quit", m_HeaderStyle);

        GUILayout.Space(10);
        
        // Player Settings
        GUILayout.Label("=== PLAYER SETTINGS ===", m_HeaderStyle);
        GUILayout.Label("Speed (units/sec):", m_LabelStyle);
        m_PlayerSpeedStr = GUILayout.TextField(m_PlayerSpeedStr, 25, m_TextFieldStyle);
        
        GUILayout.Label("Turn Speed (deg/sec):", m_LabelStyle);
        m_PlayerTurnSpeedStr = GUILayout.TextField(m_PlayerTurnSpeedStr, 25, m_TextFieldStyle);
        
        GUILayout.Space(10);

        // Delivery Zone Settings
        GUILayout.Label("=== DELIVERY ZONE SETTINGS ===", m_HeaderStyle);
        GUILayout.Label("Manual Bounds Size (x,y,z):", m_LabelStyle);
        m_ZoneBoundsSizeStr = GUILayout.TextField(m_ZoneBoundsSizeStr, 25, m_TextFieldStyle);
        
        GUILayout.Label("Vehicle Speed:", m_LabelStyle);
        m_VehicleSpeedStr = GUILayout.TextField(m_VehicleSpeedStr, 25, m_TextFieldStyle);
        
        bool repeatScenario = GUILayout.Toggle(
            m_Config != null && m_Config.repeatScenario,
            "Loop zones after completion"
        );
        if (m_Config != null) m_Config.repeatScenario = repeatScenario;
        
        GUILayout.Label("Spawn Delay (seconds):", m_LabelStyle);
        m_SpawnDelayStr = GUILayout.TextField(m_SpawnDelayStr, 25, m_TextFieldStyle);
        
        GUILayout.Space(10);

        // Enemy Spawner Settings
        GUILayout.Label("=== ENEMY SPAWNER SETTINGS ===", m_HeaderStyle);
        GUILayout.Label("Spawn Radius:", m_LabelStyle);
        m_EnemySpawnRadiusStr = GUILayout.TextField(m_EnemySpawnRadiusStr, 25, m_TextFieldStyle);
        
        GUILayout.Label("Spawn Range From Packages:", m_LabelStyle);
        m_EnemySpawnRangeStr = GUILayout.TextField(m_EnemySpawnRangeStr, 25, m_TextFieldStyle);
        
        GUILayout.Space(10);

        // Game Manager Settings
        GUILayout.Label("=== GAME MANAGER SETTINGS ===", m_HeaderStyle);
        GUILayout.Label("Total Packages To Deliver:", m_LabelStyle);
        m_TotalPackagesStr = GUILayout.TextField(m_TotalPackagesStr, 25, m_TextFieldStyle);
        
        GUILayout.Label("Mission Time Limit (seconds):", m_LabelStyle);
        m_MissionTimeLimitStr = GUILayout.TextField(m_MissionTimeLimitStr, 25, m_TextFieldStyle);
        
        GUILayout.Space(10);

        // Teleporter Settings
        GUILayout.Label("=== TELEPORTER SETTINGS ===", m_HeaderStyle);
        GUILayout.Label("Cooldown (seconds):", m_LabelStyle);
        m_TeleporterCooldownStr = GUILayout.TextField(m_TeleporterCooldownStr, 25, m_TextFieldStyle);
        
        GUILayout.Space(20);

        // Action Buttons - Temporary change color for buttons
        GUI.backgroundColor = m_ButtonColor;
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Apply to Scene", GUILayout.Height(30))) ApplyToScene();
        if (GUILayout.Button("Load Config", GUILayout.Height(30))) LoadFromConfig();
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Save to Config", GUILayout.Height(30))) SaveToConfig();

        GUILayout.EndScrollView();
        GUILayout.EndVertical();

        GUI.DragWindow();
    }

    /// <summary>
    /// Finds all relevant objects in the scene and applies the current debug values to them.
    /// </summary>
    private void ApplyToScene()
    {
        // Find all scene objects
        m_Player = FindAnyObjectByType<Player>();
        m_RobotMove = FindAnyObjectByType<RobotMove>();
        m_DeliveryZones = FindObjectsByType<DeliveryZone>(FindObjectsSortMode.None);
        m_EnemySpawners = FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
        m_GameManager = FindAnyObjectByType<GameManager>();
        m_Teleporter = FindAnyObjectByType<Delivery>();

        // Apply Player settings
        if (m_RobotMove != null)
        {
            if (float.TryParse(m_PlayerSpeedStr, out float speed))
                m_RobotMove.m_Speed = speed;
            
            if (float.TryParse(m_PlayerTurnSpeedStr, out float turnSpeed))
                m_RobotMove.m_TurnSpeed = turnSpeed;
            
            Debug.Log($"Applied Player settings: Speed={m_RobotMove.m_Speed}, TurnSpeed={m_RobotMove.m_TurnSpeed}");
        }

        // Apply Delivery Zone settings
        if (m_DeliveryZones != null && m_DeliveryZones.Length > 0)
        {
            Vector3 boundsSize = ParseVector3(m_ZoneBoundsSizeStr);
            float vehicleSpeed = float.TryParse(m_VehicleSpeedStr, out float vs) ? vs : 5f;
            float spawnDelay = float.TryParse(m_SpawnDelayStr, out float sd) ? sd : 5f;
            bool repeat = m_Config != null && m_Config.repeatScenario;

            foreach (var zone in m_DeliveryZones)
            {
                zone.m_ManualBoundsSize = boundsSize;
                zone.m_VehicleMoveSpeed = vehicleSpeed;
                zone.m_RepeatScenario = repeat;
                zone.m_VehicleSpawnDelay = spawnDelay;
            }
            
            Debug.Log($"Applied settings to {m_DeliveryZones.Length} DeliveryZones");
        }

        // Apply Enemy Spawner settings
        if (m_EnemySpawners != null && m_EnemySpawners.Length > 0)
        {
            float spawnRadius = float.TryParse(m_EnemySpawnRadiusStr, out float sr) ? sr : 5f;
            float spawnRange = float.TryParse(m_EnemySpawnRangeStr, out float srp) ? srp : 8f;

            foreach (var spawner in m_EnemySpawners)
            {
                spawner.m_SpawnRadius = spawnRadius;
                spawner.m_SpawnRangeFromPackages = spawnRange;
            }
            
            Debug.Log($"Applied settings to {m_EnemySpawners.Length} EnemySpawners");
        }

        // Apply Game Manager settings
        if (m_GameManager != null)
        {
            if (int.TryParse(m_TotalPackagesStr, out int totalPackages))
                m_GameManager.m_TotalPackagesToDeliver = totalPackages;
            
            if (float.TryParse(m_MissionTimeLimitStr, out float timeLimit))
                m_GameManager.m_MissionTimeLimit = timeLimit;
            
            Debug.Log($"Applied GameManager settings: Packages={m_GameManager.m_TotalPackagesToDeliver}, TimeLimit={m_GameManager.m_MissionTimeLimit}");
        }

        // Apply Teleporter settings
        if (m_Teleporter != null)
        {
            if (float.TryParse(m_TeleporterCooldownStr, out float cooldown))
                m_Teleporter._cooldownTimer = cooldown;
            
            Debug.Log($"Applied Teleporter cooldown: {m_Teleporter._cooldownTimer}");
        }

        Debug.Log("=== All settings applied to scene ===");
        
        // Start the game if GameManager is in MainMenu state
        if (m_GameManager != null)
        {
            m_GameManager.StartGame();
            Debug.Log("Starting game with applied settings...");
        }
    }

    /// <summary>
    /// Loads values from the assigned DebugConfig ScriptableObject into the text fields.
    /// </summary>
    private void LoadFromConfig()
    {
        if (m_Config == null)
        {
            Debug.LogWarning("No DebugConfig assigned to DebugWindow!");
            InitializeDefaults();
            return;
        }

        m_PlayerSpeedStr = m_Config.playerSpeed.ToString();
        m_PlayerTurnSpeedStr = m_Config.playerTurnSpeed.ToString();
        m_ZoneBoundsSizeStr = Vector3ToString(m_Config.zoneManualBoundsSize);
        m_VehicleSpeedStr = m_Config.vehicleSpeed.ToString();
        m_SpawnDelayStr = m_Config.spawnDelay.ToString();
        m_EnemySpawnRadiusStr = m_Config.enemySpawnRadius.ToString();
        m_EnemySpawnRangeStr = m_Config.enemySpawnRangeFromPackages.ToString();
        m_TotalPackagesStr = m_Config.totalPackagesToDeliver.ToString();
        m_MissionTimeLimitStr = m_Config.missionTimeLimit.ToString();
        m_TeleporterCooldownStr = m_Config.teleporterCooldown.ToString();

        Debug.Log("Loaded values from DebugConfig");
    }

    /// <summary>
    /// Saves the current text field values back to the DebugConfig ScriptableObject.
    /// </summary>
    private void SaveToConfig()
    {
        if (m_Config == null)
        {
            Debug.LogWarning("No DebugConfig assigned to save to!");
            return;
        }

        if (float.TryParse(m_PlayerSpeedStr, out float speed))
            m_Config.playerSpeed = speed;
        
        if (float.TryParse(m_PlayerTurnSpeedStr, out float turnSpeed))
            m_Config.playerTurnSpeed = turnSpeed;

        m_Config.zoneManualBoundsSize = ParseVector3(m_ZoneBoundsSizeStr);

        if (float.TryParse(m_VehicleSpeedStr, out float vehicleSpeed))
            m_Config.vehicleSpeed = vehicleSpeed;
        
        if (float.TryParse(m_SpawnDelayStr, out float spawnDelay))
            m_Config.spawnDelay = spawnDelay;

        if (float.TryParse(m_EnemySpawnRadiusStr, out float enemyRadius))
            m_Config.enemySpawnRadius = enemyRadius;
        
        if (float.TryParse(m_EnemySpawnRangeStr, out float enemyRange))
            m_Config.enemySpawnRangeFromPackages = enemyRange;

        if (int.TryParse(m_TotalPackagesStr, out int totalPackages))
            m_Config.totalPackagesToDeliver = totalPackages;
        
        if (float.TryParse(m_MissionTimeLimitStr, out float timeLimit))
            m_Config.missionTimeLimit = timeLimit;

        if (float.TryParse(m_TeleporterCooldownStr, out float cooldown))
            m_Config.teleporterCooldown = cooldown;

        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(m_Config);
        #endif

        Debug.Log("Saved values to DebugConfig");
    }

    private void InitializeDefaults()
    {
        m_PlayerSpeedStr = "12";
        m_PlayerTurnSpeedStr = "180";
        m_ZoneBoundsSizeStr = "20,2,20";
        m_VehicleSpeedStr = "5";
        m_SpawnDelayStr = "5";
        m_EnemySpawnRadiusStr = "5";
        m_EnemySpawnRangeStr = "8";
        m_TotalPackagesStr = "10";
        m_MissionTimeLimitStr = "300";
        m_TeleporterCooldownStr = "2";
    }

    private Vector3 ParseVector3(string str)
    {
        string[] parts = str.Split(',');
        if (parts.Length == 3 &&
            float.TryParse(parts[0], out float x) &&
            float.TryParse(parts[1], out float y) &&
            float.TryParse(parts[2], out float z))
        {
            return new Vector3(x, y, z);
        }
        return new Vector3(20f, 2f, 20f);
    }

    private string Vector3ToString(Vector3 v)
    {
        return $"{v.x},{v.y},{v.z}";
    }
}