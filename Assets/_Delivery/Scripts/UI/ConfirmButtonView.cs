// ============================================================
//  ConfirmButtonView.cs
//  Place in: Assets/_Delivery/Scripts/
//  Layer   : View — MonoBehaviour
//
//  The "Deploy" confirm button on the Board.
//  Disabled until both a contract and a location are selected.
//  Fires OnClicked up to BoardView, which relays it to GameManager.
//
//  Attach to: Confirm Button GameObject (Canvas child)
//  Inspector : assign Button component and TMP label
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConfirmButtonView : MonoBehaviour
{
    [SerializeField] private Button          m_Button;
    [SerializeField] private TextMeshProUGUI m_Label;

    [Header("Label Text")]
    [SerializeField] private string m_EnabledText  = "DEPLOY";
    [SerializeField] private string m_DisabledText = "Select a contract and location";

    [Header("Colors")]
    [SerializeField] private Color m_EnabledColor  = Color.white;
    [SerializeField] private Color m_DisabledColor = new Color(1f, 1f, 1f, 0.3f);

    // BoardView subscribes to this
    public event System.Action OnClicked;

    // -------------------------------------------------------
    //  Unity lifecycle
    // -------------------------------------------------------

    private void Awake()
    {
        m_Button.onClick.AddListener(() => OnClicked?.Invoke());
        SetEnabled(false);
    }

    // -------------------------------------------------------
    //  Public API  (called by BoardView)
    // -------------------------------------------------------

    public void SetEnabled(bool enabled)
    {
        m_Button.interactable = enabled;
        m_Label.text          = enabled ? m_EnabledText  : m_DisabledText;
        m_Label.color         = enabled ? m_EnabledColor : m_DisabledColor;
    }

    public void SetLabel(string text) => m_Label.text = text;
}
