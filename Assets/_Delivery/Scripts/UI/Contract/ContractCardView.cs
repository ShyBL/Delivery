// ============================================================
//  ContractCardView.cs
//  Place in: Assets/_Delivery/Scripts/
//  Layer   : View — MonoBehaviour
//
//  Renders one ContractSO card on the Board.
//  On click, notifies GameManager.Instance.OnContractSelected().
//  Refreshes its own highlight and all sibling contract cards.
//
//  Attach to: ContractCard UI prefab
//  Inspector : wire TMP labels, requirement line prefab,
//              highlight Image, and Button
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ContractCardView : MonoBehaviour
{
    #region Inspector
    
    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI m_FactionLabel;
    [SerializeField] private TextMeshProUGUI m_NameLabel;

    [Header("Requirements")]
    [SerializeField] private Transform       m_RequirementsContainer;
    [SerializeField] private TextMeshProUGUI m_RequirementLinePrefab;

    [Header("Selection")]
    [SerializeField] private Image m_Highlight;
    [SerializeField] private Color m_NormalColor   = new Color(1f, 1f, 1f, 0f);
    [SerializeField] private Color m_SelectedColor = new Color(1f, 0.85f, 0f, 0.25f);

    [Header("Interaction")]
    [SerializeField] private Button m_Button;

    private ContractSO m_Bound;

    #endregion
    
    #region Unity lifecycle
    
    private void Awake()
    {
        m_Button.onClick.AddListener(OnClicked);
    }

    #endregion
    
    #region Binding  (called by BoardView.Populate)
    
    public void Bind(ContractSO contract)
    {
        m_Bound = contract;
        Render();
    }

    private void Render()
    {
        m_FactionLabel.text = m_Bound.faction.ToString();
        m_NameLabel.text    = m_Bound.displayName;

        foreach (Transform child in m_RequirementsContainer)
            Destroy(child.gameObject);

        foreach (var req in m_Bound.GetRequirements())
        {
            var textMeshPro = Instantiate(m_RequirementLinePrefab, m_RequirementsContainer);
            textMeshPro.text = req.ToString();
        }

        RefreshHighlight();
    }

    public void RefreshHighlight() =>
        m_Highlight.color = m_Bound.IsSelected() ? m_SelectedColor : m_NormalColor;

    #endregion
    
    #region Interaction
    
    private void OnClicked()
    {
        GameManager.Instance.OnContractSelected(m_Bound);

        // Refresh all contract cards so only this one shows as selected
        foreach (var card in FindObjectsByType<ContractCardView>(FindObjectsSortMode.None))
            card.RefreshHighlight();
    }
    
    #endregion
}
