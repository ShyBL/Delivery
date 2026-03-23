using UnityEngine;

/// <summary>
/// Attach to any world-space GameObject that should appear as a navigational
/// marker on the HUD (packages, drop-zones, points-of-interest, etc.).
///
/// Mirrors the Tanks pattern of keeping data/state on the subject itself
/// (e.g. TankHealth lives on the tank) so the UI controller only reads,
/// never drives, game state.
/// </summary>
public class WorldMarker : MonoBehaviour
{
    #region Inspector

    [Header("Identity")]
    [Tooltip("Human-readable label shown in the marker tooltip (optional).")]
    public string markerLabel = "Point of Interest";

    [Tooltip("Icon shown once the player has discovered this object " +
             "(i.e. it has entered the camera frustum at least once).")]
    public Sprite discoveredIcon;

    [Tooltip("Icon shown while the object has never been seen " +
             "(question-mark style).")]
    public Sprite unknownIcon;
    #endregion
    
    #region State (read by MarkerHUDController)

    /// <summary>True after the object has been inside the camera frustum at least once.</summary>
    public bool IsDiscovered { get; private set; } = false;

    /// <summary>True while the object is currently inside the camera frustum.</summary>
    public bool IsVisible { get; private set; } = false;
    #endregion
    
    #region Internal

    // Raised so the HUD controller can react without polling.
    public event System.Action<WorldMarker> OnDiscovered;

    /// <summary>
    /// Called by MarkerHUDController each frame after it has tested visibility.
    /// Keeping the frustum check centralised avoids N redundant Camera calls.
    /// </summary>
    internal void SetVisibility(bool visible)
    {
        IsVisible = visible;

        if (visible && !IsDiscovered)
        {
            IsDiscovered = true;
            OnDiscovered?.Invoke(this);
        }
    }

    /// <summary>Returns the correct icon for the current discovery state.</summary>
    public Sprite CurrentIcon => IsDiscovered ? discoveredIcon : unknownIcon;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (discoveredIcon == null)
            Debug.LogWarning($"[WorldMarker] '{name}' has no discoveredIcon assigned.", this);
        if (unknownIcon == null)
            Debug.LogWarning($"[WorldMarker] '{name}' has no unknownIcon assigned.", this);
    }
#endif
    
    #endregion
}