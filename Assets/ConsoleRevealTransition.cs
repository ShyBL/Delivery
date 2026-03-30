using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Easing presets that map to a baked AnimationCurve.
/// Selecting a preset overwrites m_Curve automatically.
/// Set to Custom to hand-author the curve directly in the Inspector.
/// </summary>
public enum EasingPreset
{
    Linear,
    EaseInOut,
    EaseOut,
    Custom
}

/// <summary>
/// Plays a reveal transition when Play() is called externally.
///
/// Two things animate in parallel over m_Duration seconds:
///   1. Console Camera local Z  : m_CameraZFrom  -> m_CameraZTo
///   2. RawImage localScale     : m_ScaleFrom    -> m_ScaleTo  (uniform XY, Z stays 1)
///
/// Easing is driven by a single AnimationCurve (m_Curve) applied to the
/// normalised time t. Choosing a preset from m_EasingPreset bakes a matching
/// curve automatically. Set to Custom to author the curve by hand.
///
/// Exposes m_OnComplete (UnityEvent) so GameManager or any other caller
/// can react when the transition finishes without polling.
///
/// Usage:
///   consoleRevealTransition.Play();
///
/// Mirrors the coroutine pattern used in GameManager.GameLoop() in Tanks.
/// </summary>
public class ConsoleRevealTransition : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("The Console Camera whose local Z will be animated.")]
    [SerializeField] private Camera m_ConsoleCamera;

    [Tooltip("The RawImage showing the full-screen game view. Its localScale will be animated.")]
    [SerializeField] private RawImage m_GameViewRawImage;

    [Header("Camera Z")]
    [Tooltip("Local Z position the console camera starts at (far, close-up on screen).")]
    [SerializeField] private float m_CameraZFrom = -16f;

    [Tooltip("Local Z position the console camera ends at (pulled back, hands visible).")]
    [SerializeField] private float m_CameraZTo = -10f;

    [Header("RawImage Scale")]
    [Tooltip("Uniform XY scale the RawImage starts at (fills screen, console not visible).")]
    [SerializeField] private float m_ScaleFrom = 2f;

    [Tooltip("Uniform XY scale the RawImage ends at (normal size, console frame revealed).")]
    [SerializeField] private float m_ScaleTo = 1f;

    [Header("Timing")]
    [Tooltip("Total duration of the transition in seconds.")]
    [SerializeField, Range(0.1f, 5f)] private float m_Duration = 1.5f;

    [Header("Easing")]
    [Tooltip("Choose a preset to auto-bake the curve, or pick Custom to author it by hand.")]
    [SerializeField] private EasingPreset m_EasingPreset = EasingPreset.EaseInOut;

    [Tooltip("Animation curve applied to normalised time t (0-1). " +
             "Overwritten when a non-Custom preset is selected.")]
    [SerializeField] private AnimationCurve m_Curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Events")]
    [Tooltip("Fired when the transition reaches t = 1.")]
    public UnityEvent m_OnComplete;

    // Whether a transition is currently running — prevents double-play
    private bool m_IsPlaying = false;

    #region UNITY LIFECYCLE

    private void Awake()
    {
        ValidateReferences();

        // Apply the preset once on Awake so the curve is correct from the first frame,
        // even if OnValidate hasn't fired yet (e.g. first play in a fresh build)
        if (m_EasingPreset != EasingPreset.Custom)
            BakeCurveFromPreset(m_EasingPreset);
    }

    /// <summary>
    /// Called by Unity in the Editor whenever an Inspector value changes.
    /// Keeps the curve in sync with the preset without needing to enter play mode.
    /// </summary>
    private void OnValidate()
    {
        if (m_EasingPreset != EasingPreset.Custom)
            BakeCurveFromPreset(m_EasingPreset);
    }

    #endregion

    #region PUBLIC API

    [ContextMenu("Play Transition")]
    private void ContextPlay()
    {
        Play();
    }

    [ContextMenu("Rewind and Play")]
    private void ContextRewindAndPlay()
    {
        StopAllCoroutines();
        m_IsPlaying = false;
        ApplyT(0f);
        StartCoroutine(RunTransition());
    }

    /// <summary>
    /// Starts the reveal transition. Safe to call from GameManager, a trigger,
    /// or any other external caller. Does nothing if already playing.
    /// </summary>
    public void Play()
    {
        if (m_IsPlaying)
        {
            Debug.LogWarning($"ConsoleRevealTransition '{gameObject.name}': " +
                             "Play() called while transition is already running. Ignored.", this);
            return;
        }

        StartCoroutine(RunTransition());
    }

    /// <summary>
    /// Immediately snaps both targets to their end state and cancels any running transition.
    /// Useful for skipping the intro or resetting in tests.
    /// </summary>
    public void SnapToEnd()
    {
        StopAllCoroutines();
        m_IsPlaying = false;
        ApplyT(1f);
        m_OnComplete.Invoke();
    }

    /// <summary>
    /// Immediately snaps both targets back to their start state.
    /// </summary>
    public void SnapToStart()
    {
        StopAllCoroutines();
        m_IsPlaying = false;
        ApplyT(0f);
    }

    #endregion

    #region TRANSITION COROUTINE

    /// <summary>
    /// Drives both animations in parallel from t=0 to t=1 over m_Duration seconds.
    /// The AnimationCurve is evaluated at each normalised t to produce the eased value.
    /// Mirrors the coroutine structure of MissionActive() in GameManager.
    /// </summary>
    private IEnumerator RunTransition()
    {
        m_IsPlaying = true;

        // Snap to start state before beginning so there's no first-frame pop
        ApplyT(0f);

        float elapsed = 0f;

        while (elapsed < m_Duration)
        {
            elapsed += Time.deltaTime;
            float rawT   = Mathf.Clamp01(elapsed / m_Duration);
            float easedT = m_Curve.Evaluate(rawT);

            ApplyT(easedT);

            yield return null;
        }

        // Guarantee we land exactly on the end values regardless of frame timing
        ApplyT(1f);

        m_IsPlaying = false;
        m_OnComplete.Invoke();
    }

    #endregion

    #region APPLY

    /// <summary>
    /// Applies both animated properties at the given eased t value (0-1).
    /// Keeping application separate from the coroutine lets SnapToEnd/SnapToStart
    /// reuse the same logic without duplicating it.
    /// </summary>
    private void ApplyT(float t)
    {
        // ── Console Camera Z ────────────────────────────────────────────────
        if (m_ConsoleCamera != null)
        {
            Vector3 camPos = m_ConsoleCamera.transform.localPosition;
            camPos.z = Mathf.LerpUnclamped(m_CameraZFrom, m_CameraZTo, t);
            m_ConsoleCamera.transform.localPosition = camPos;
        }

        // ── RawImage Scale ───────────────────────────────────────────────────
        if (m_GameViewRawImage != null)
        {
            float scale = Mathf.LerpUnclamped(m_ScaleFrom, m_ScaleTo, t);
            m_GameViewRawImage.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    #endregion

    #region CURVE BAKING

    /// <summary>
    /// Writes a matching AnimationCurve for the given preset.
    /// Uses Mathf.SmoothStep-equivalent tangents for EaseInOut,
    /// a steep-start flat-end shape for EaseOut, and a straight line for Linear.
    /// </summary>
    private void BakeCurveFromPreset(EasingPreset preset)
    {
        switch (preset)
        {
            case EasingPreset.Linear:
                m_Curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
                break;

            case EasingPreset.EaseInOut:
                m_Curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
                break;

            case EasingPreset.EaseOut:
                // Start fast (in-tangent steep), settle softly (out-tangent flat)
                m_Curve = new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 2.5f),   // steep departure
                    new Keyframe(1f, 1f, 0f, 0f)       // flat arrival
                );
                break;
        }
    }

    #endregion

    #region VALIDATION

    private void ValidateReferences()
    {
        if (m_ConsoleCamera == null)
            Debug.LogWarning($"ConsoleRevealTransition '{gameObject.name}': " +
                             "m_ConsoleCamera is not assigned. Camera Z will not animate.", this);

        if (m_GameViewRawImage == null)
            Debug.LogWarning($"ConsoleRevealTransition '{gameObject.name}': " +
                             "m_GameViewRawImage is not assigned. Scale will not animate.", this);
    }

    #endregion
}