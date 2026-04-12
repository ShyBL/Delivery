// ============================================================
//  GameManager.cs
//  Place in: Assets/_Delivery/Scripts/
//  Layer   : Controller — MonoBehaviour, singleton
//
//  Merges: MissionController + LevelController + BoardController
//
//  Phases:
//    Board      -> player picks a ContractSO and LocationSO
//    Scavenging -> player collects resources in the level
//
//  Inspector wiring:
//    - Contract pool     (List<ContractSO>)
//    - Fixed locations   (List<LocationSO>)
//    - Wildcard pool     (List<LocationSO>)
//    - ResourceSpawner   ref
//    - BoardView         ref
//    - HUDView           ref
//    - Player            ref
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    #region Inspector

    [Header("Contracts")]
    [SerializeField] private List<ContractSO> m_ContractPool;
    [SerializeField] private int              m_ContractsDealt = 3;

    [Header("Locations — Fixed Slots")]
    [Tooltip("Always dealt. Recommend: IndustrialRuin, MiningZone")]
    [SerializeField] private List<LocationSO> m_FixedLocations;

    [Header("Locations — Wildcard Pool")]
    [Tooltip("One drawn randomly per run")]
    [SerializeField] private List<LocationSO> m_WildcardPool;

    [Header("Scene References")]
    [SerializeField] private ResourceSpawner m_ResourceSpawner;
    [SerializeField] private BoardView       m_BoardView;
    [SerializeField] private HUDView         m_HUDView;
    [SerializeField] private Player          m_Player;

    [Header("Timing")]
    [SerializeField] private float m_StartDelay = 1f;
    [SerializeField] private float m_EndDelay   = 3f;

    #endregion

    #region Runtime State

    private GamePhase  m_Phase;
    private ContractSO m_SelectedContract;
    private LocationSO m_SelectedLocation;

    private List<ContractSO> m_DealtContracts = new List<ContractSO>();
    private List<LocationSO> m_DealtLocations = new List<LocationSO>();

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        m_BoardView.OnConfirmClicked += HandleBoardConfirmed;
        OpenBoard();
    }

    private void OnDestroy()
    {
        if (m_BoardView != null)
            m_BoardView.OnConfirmClicked -= HandleBoardConfirmed;
    }

    #endregion

    #region Phase: Board

    private void OpenBoard()
    {
        m_Phase = GamePhase.Board;

        foreach (var c in m_DealtContracts) c.ResetState();
        foreach (var l in m_DealtLocations) l.ResetState();

        m_SelectedContract = null;
        m_SelectedLocation = null;

        m_DealtContracts = DealContracts(m_ContractsDealt);
        m_DealtLocations = DealLocations();

        m_BoardView.Populate(m_DealtContracts, m_DealtLocations);
        m_BoardView.SetConfirmEnabled(false);
        m_BoardView.Show();
        m_HUDView.gameObject.SetActive(false);
    }

    #endregion

    #region Phase: Scavenging

    private void StartScavenging()
    {
        m_Phase = GamePhase.Scavenging;
        m_BoardView.Hide();
        m_HUDView.gameObject.SetActive(true);
        m_Player.GetInventory().Clear();

        m_Player.GetInventory().OnChanged += RefreshHUD;
        RefreshHUD();

        m_ResourceSpawner.SpawnForLocation(m_SelectedLocation);

        Debug.Log($"[GameManager] Scavenging -> {m_SelectedLocation} | {m_SelectedContract}");
    }

    #endregion

    #region Board Selection

    public void OnContractSelected(ContractSO contract)
    {
        if (m_SelectedContract != null) m_SelectedContract.Deselect();
        m_SelectedContract = contract;
        m_SelectedContract.Select();
        UpdateConfirmButton();
    }

    public void OnLocationSelected(LocationSO location)
    {
        if (m_SelectedLocation != null) m_SelectedLocation.Deselect();
        m_SelectedLocation = location;
        m_SelectedLocation.Select();
        UpdateConfirmButton();
    }

    private void UpdateConfirmButton()
    {
        bool ready = m_SelectedContract != null && m_SelectedLocation != null;
        m_BoardView.SetConfirmEnabled(ready);
    }

    private void HandleBoardConfirmed()
    {
        if (m_SelectedContract == null || m_SelectedLocation == null)
        {
            Debug.LogWarning("[GameManager] Confirm fired but selection is incomplete.");
            return;
        }

        StartCoroutine(TransitionToScavenging());
    }

    private IEnumerator TransitionToScavenging()
    {
        yield return new WaitForSeconds(m_StartDelay);
        StartScavenging();
    }

    #endregion

    #region HUD Refresh

    private void RefreshHUD()
    {
        m_HUDView.UpdateInventory(m_Player.GetInventory());
        m_HUDView.UpdateChecklist(m_SelectedContract, m_Player.GetInventory());
    }

    #endregion

    #region Run Management

    public void OnRunComplete()
    {
        m_Player.GetInventory().OnChanged -= RefreshHUD;
        m_ResourceSpawner.ClearNodes();
        StartCoroutine(TransitionToBoard());
    }

    private IEnumerator TransitionToBoard()
    {
        yield return new WaitForSeconds(m_EndDelay);
        OpenBoard();
    }

    #endregion

    #region Dealing Logic

    private List<ContractSO> DealContracts(int count)
    {
        if (m_ContractPool == null || m_ContractPool.Count == 0)
        {
            Debug.LogError("[GameManager] Contract pool is empty — assign ContractSOs in Inspector.");
            return new List<ContractSO>();
        }

        var shuffled = new List<ContractSO>(m_ContractPool);
        Shuffle(shuffled);

        var dealt = new List<ContractSO>();
        int take  = Mathf.Min(count, shuffled.Count);
        for (int i = 0; i < take; i++)
            dealt.Add(shuffled[i]);

        return dealt;
    }

    private List<LocationSO> DealLocations()
    {
        var dealt = new List<LocationSO>();

        if (m_FixedLocations == null || m_FixedLocations.Count == 0)
        {
            Debug.LogError("[GameManager] Fixed locations list is empty — assign LocationSOs in Inspector.");
            return dealt;
        }

        foreach (var loc in m_FixedLocations)
            dealt.Add(loc);

        if (m_WildcardPool != null && m_WildcardPool.Count > 0)
            dealt.Add(m_WildcardPool[Random.Range(0, m_WildcardPool.Count)]);
        else
            Debug.LogWarning("[GameManager] Wildcard pool is empty — only fixed slots dealt.");

        return dealt;
    }

    #endregion

    #region Accessors & Helpers

    public ContractSO GetSelectedContract() => m_SelectedContract;
    public LocationSO GetSelectedLocation() => m_SelectedLocation;

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