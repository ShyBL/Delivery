// ============================================================
//  LevelCardView.cs
//  Place in: Assets/_Delivery/Scripts/
//  Layer   : View — MonoBehaviour
//
//  Renders one LocationSO card on the Board.
//  On click, notifies GameManager.Instance.OnLocationSelected().
//  Refreshes its own highlight and all sibling level cards.
//
//  Attach to: LevelCard UI prefab
//  Inspector : wire TMP labels, availability line prefab,
//              highlight Image, and Button
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelCardView : MonoBehaviour
{
    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI m_NameLabel;
    [SerializeField] private TextMeshProUGUI m_TypeLabel;

    [Header("Availability Lines")]
    [SerializeField] private Transform       m_AvailabilityContainer;
    [SerializeField] private TextMeshProUGUI m_AvailabilityLinePrefab;

    [Header("Selection")]
    [SerializeField] private Image m_Highlight;
    [SerializeField] private Color m_NormalColor   = new Color(1f, 1f, 1f, 0f);
    [SerializeField] private Color m_SelectedColor = new Color(0f, 0.8f, 1f, 0.25f);

    [Header("Interaction")]
    [SerializeField] private Button m_Button;

    private LocationSO m_Bound;

    private static readonly string[] AbundanceLabels =
        { "none", "scarce", "moderate", "abundant" };

    // -------------------------------------------------------
    //  Unity lifecycle
    // -------------------------------------------------------

    private void Awake()
    {
        m_Button.onClick.AddListener(OnClicked);
    }

    // -------------------------------------------------------
    //  Binding  (called by BoardView.Populate)
    // -------------------------------------------------------

    public void Bind(LocationSO location)
    {
        m_Bound = location;
        Render();
    }

    private void Render()
    {
        m_NameLabel.text = m_Bound.locationName;
        m_TypeLabel.text = m_Bound.locationType.ToString() +
                           (m_Bound.isWildcard ? "  [wildcard]" : "");

        foreach (Transform child in m_AvailabilityContainer)
            Destroy(child.gameObject);

        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            int abundance = m_Bound.GetAvailability(type);
            var line      = Instantiate(m_AvailabilityLinePrefab, m_AvailabilityContainer);
            line.text     = $"{type}: {AbundanceLabels[abundance]}";
            line.color    = abundance == 0 ? Color.gray : Color.white;
        }

        RefreshHighlight();
    }

    public void RefreshHighlight() =>
        m_Highlight.color = m_Bound.IsSelected() ? m_SelectedColor : m_NormalColor;

    // -------------------------------------------------------
    //  Interaction
    // -------------------------------------------------------

    private void OnClicked()
    {
        GameManager.Instance.OnLocationSelected(m_Bound);

        // Refresh all level cards so only this one shows as selected
        foreach (var card in FindObjectsByType<LevelCardView>(FindObjectsSortMode.None))
            card.RefreshHighlight();
    }
}
