using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders a single navigational marker on the HUD canvas.
///
/// Responsibilities (single responsibility, matching Tanks' UIDirectionControl):
///  • Display the correct icon (unknown vs discovered).
///  • In edge-mode: position itself at the clamped screen edge and rotate
///    an arrow child to point toward the target.
///  • In on-screen mode: hide itself (the world object is directly visible).
///
/// Never touches game state — it is a pure view.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MarkerWidget : MonoBehaviour
{
    // ── Inspector (set up in the prefab) ─────────────────────────────────

    [Header("Icon")]
    [Tooltip("Displays the unknown (?) or discovered icon.")]
    public Image iconImage;

    [Header("Arrow")]
    [Tooltip("Child RectTransform rotated to point toward the off-screen target. " +
             "Disable the arrow GameObject when the target is on-screen.")]
    public RectTransform arrowTransform;

    [Tooltip("Optional separate Image for the arrow so it can be styled " +
             "independently from the icon.")]
    public Image arrowImage;

    // ── Internal ─────────────────────────────────────────────────────────

    private RectTransform m_Rect;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    private void Awake()
    {
        m_Rect = GetComponent<RectTransform>();
    }

    // ── Public API (called by MarkerHUDController) ────────────────────────

    /// <summary>Sets the anchored size of this widget.</summary>
    public void Initialise(Vector2 size)
    {
        m_Rect ??= GetComponent<RectTransform>();
        m_Rect.sizeDelta = size;

        // Anchor to center so we can position freely in canvas-space.
        m_Rect.anchorMin = new Vector2(0.5f, 0.5f);
        m_Rect.anchorMax = new Vector2(0.5f, 0.5f);
        m_Rect.pivot     = new Vector2(0.5f, 0.5f);
    }

    /// <summary>Swaps the icon sprite (unknown ↔ discovered).</summary>
    public void SetIcon(Sprite sprite)
    {
        if (iconImage == null) return;
        iconImage.sprite  = sprite;
        iconImage.enabled = sprite != null;
    }

    /// <summary>
    /// Switches between edge-mode (off-screen) and hidden-mode (on-screen).
    /// </summary>
    /// <param name="edgeMode">True → show edge indicator; False → hide.</param>
    /// <param name="canvasPosition">Anchored position within the canvas (edge-mode only).</param>
    /// <param name="angleDeg">Arrow rotation in degrees (edge-mode only).</param>
    public void SetEdgeMode(bool edgeMode, Vector2 canvasPosition, float angleDeg)
    {
        if (edgeMode)
        {
            m_Rect.anchoredPosition = canvasPosition;

            if (arrowTransform != null)
            {
                arrowTransform.gameObject.SetActive(true);
                // Z-rotation so 0° = right; adjust offset if your arrow asset points up.
                arrowTransform.localRotation =
                    Quaternion.Euler(0f, 0f, angleDeg - 90f);
            }

            if (iconImage != null)
                iconImage.gameObject.SetActive(true);
        }
        else
        {
            // On-screen: hide the entire widget.
            if (arrowTransform != null)
                arrowTransform.gameObject.SetActive(false);

            if (iconImage != null)
                iconImage.gameObject.SetActive(false);
        }
    }
}