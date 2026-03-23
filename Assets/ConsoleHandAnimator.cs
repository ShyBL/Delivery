using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Per-hand settings block. Shown as a foldout in the Inspector for each hand.
/// All motion parameters are independent so left and right can be tuned separately.
/// </summary>
[System.Serializable]
public class HandAnimSettings
{
    [Header("Tilt")]
    [Tooltip("Maximum tilt angle in degrees at full input.")]
    [Range(0f, 15f)] public float m_MaxTiltAngle = 1f;

    [Tooltip("How quickly the hand tracks toward its target rotation. Higher = snappier.")]
    [Range(1f, 30f)] public float m_SmoothSpeed = 6f;

    [Header("Twist")]
    [Tooltip("Secondary Y-axis rotation applied alongside the primary tilt. Adds a natural wrist roll.")]
    [Range(0f, 5f)] public float m_TwistAngle = 0.3f;

    [Header("Idle Sway")]
    [Tooltip("Angular amplitude of the procedural drift when no input is held.")]
    [Range(0f, 3f)] public float m_IdleSwayAmount = 0.2f;

    [Tooltip("How fast the idle sway oscillates. Lower = languid, higher = nervous.")]
    [Range(0f, 5f)] public float m_IdleSwaySpeed = 0.6f;

    [Tooltip("Phase offset in seconds so left and right hands don't sway in sync.")]
    public float m_IdleSwayPhase = 0f;

    [Header("Anticipation")]
    [Tooltip("Strength of the brief counter-lean that fires when input direction changes. " +
             "0 = disabled, 1 = full m_MaxTiltAngle in the opposite direction.")]
    [Range(0f, 1f)] public float m_AnticipationStrength = 0.2f;

    [Tooltip("How quickly the anticipation decays each frame (higher = shorter burst).")]
    [Range(1f, 30f)] public float m_AnticipationDecay = 12f;

    [Header("Return Overshoot")]
    [Tooltip("How much rotational velocity carries through when input is released. " +
             "Produces a natural swing-past-neutral then settle.")]
    [Range(0f, 1f)] public float m_OvershootStrength = 0.15f;

    [Tooltip("Damping applied to the overshoot velocity each frame. " +
             "Higher = quicker settle, lower = more bounce.")]
    [Range(1f, 30f)] public float m_OvershootDamping = 10f;

    [Header("Vertical Bob")]
    [Tooltip("How far (in local Y units) the hand root dips when input is held at full magnitude.")]
    [Range(0f, 0.02f)] public float m_BobAmount = 0.002f;

    [Tooltip("How quickly the bob position tracks toward its target.")]
    [Range(1f, 30f)] public float m_BobSmoothSpeed = 5f;

    [Header("Neutral Pose Offset")]
    [Tooltip("Local position offset added on top of the scene-placed root position. " +
             "Lets you shift the rest pose from the Inspector without moving the GameObject.")]
    public Vector3 m_NeutralPositionOffset = Vector3.zero;
}

/// <summary>
/// Runtime state tracked independently for each hand.
/// Keeps all mutable per-hand data out of the settings struct.
/// </summary>
internal class HandAnimState
{
    public Quaternion NeutralRotation;   // Captured in Awake from the root's localRotation
    public Vector3    NeutralPosition;   // Captured in Awake from the root's localPosition

    public float AnticipationValue;      // Current counter-lean angle (decays each frame)
    public float PreviousInput;          // Last frame's input — used to detect direction changes
    public float OvershootVelocity;      // Carries rotation momentum through input release
    public float CurrentTiltAngle;       // The actual tilt angle being applied this frame
    public float CurrentBobOffset;       // The actual Y bob offset being applied this frame
}

/// <summary>
/// Animates two hand root Transforms on a handheld console model to visually
/// represent player input — no rigging required.
///
/// Left hand  → driven by Vertical axis  (movement: forward/back tilt + Y twist)
/// Right hand → driven by Horizontal axis (turning:  left/right roll  + Y twist)
///
/// Features per hand (all tunable independently in the Inspector):
///   • Analog tilt    — scales with input magnitude, not binary on/off
///   • Twist          — secondary Y lean layered on top of the primary tilt
///   • Idle sway      — gentle procedural drift that fades out when input arrives
///   • Anticipation   — brief counter-lean when direction changes (squash and stretch feel)
///   • Overshoot      — spring-style momentum carry when input is released
///   • Vertical bob   — hand dips slightly when input is held
///   • Neutral offset — shift rest pose per hand from the Inspector
///
/// Setup:
///   ConsoleObject                 <- add this component here
///   |- LeftHandRoot               <- assign to m_LeftHandRoot  (script moves and rotates this)
///   |  +- LeftHandMesh            <- your mesh, localRotation (-90, 0, 0), pivot at thumb base
///   +- RightHandRoot              <- assign to m_RightHandRoot
///      +- RightHandMesh
///
/// The script never touches the mesh child's transform.
/// </summary>
public class ConsoleHandAnimator : MonoBehaviour
{
    [Header("Hand Roots")]
    [Tooltip("Root Transform wrapping the left hand mesh. Script moves and rotates this.")]
    [SerializeField] private Transform m_LeftHandRoot;

    [Tooltip("Root Transform wrapping the right hand mesh. Script moves and rotates this.")]
    [SerializeField] private Transform m_RightHandRoot;

    [Header("Left Hand Settings")]
    [SerializeField] private HandAnimSettings m_LeftHand = new HandAnimSettings
    {
        m_MaxTiltAngle          = 1f,     // base unit — scale everything else relative to this
        m_SmoothSpeed           = 6f,     // moderate tracking, not too snappy at small angles
        m_TwistAngle            = 0.3f,   // ~30% of max tilt, barely perceptible wrist roll
        m_IdleSwayAmount        = 0.2f,   // very gentle drift at rest
        m_IdleSwaySpeed         = 0.6f,   // slow, languid oscillation
        m_IdleSwayPhase         = 0f,
        m_AnticipationStrength  = 0.2f,   // subtle counter-lean, not distracting
        m_AnticipationDecay     = 12f,    // decays quickly so it doesn't linger
        m_OvershootStrength     = 0.15f,  // light momentum carry on release
        m_OvershootDamping      = 10f,    // settles fast at this scale
        m_BobAmount             = 0.002f, // tiny dip — 2mm equivalent in close-up space
        m_BobSmoothSpeed        = 5f,
        m_NeutralPositionOffset = Vector3.zero
    };

    [Header("Right Hand Settings")]
    [SerializeField] private HandAnimSettings m_RightHand = new HandAnimSettings
    {
        m_MaxTiltAngle          = 1f,
        m_SmoothSpeed           = 6f,
        m_TwistAngle            = 0.3f,
        m_IdleSwayAmount        = 0.2f,
        m_IdleSwaySpeed         = 0.6f,
        m_IdleSwayPhase         = 1.3f,   // offset so hands don't sway in lockstep
        m_AnticipationStrength  = 0.2f,
        m_AnticipationDecay     = 12f,
        m_OvershootStrength     = 0.15f,
        m_OvershootDamping      = 10f,
        m_BobAmount             = 0.002f,
        m_BobSmoothSpeed        = 5f,
        m_NeutralPositionOffset = Vector3.zero
    };

    // Input actions — same axis names as RobotMove, no remapping needed
    private InputAction m_MoveAction;
    private InputAction m_TurnAction;

    // Per-hand runtime state — mutable data lives here, not in the settings struct
    private HandAnimState m_LeftState  = new HandAnimState();
    private HandAnimState m_RightState = new HandAnimState();

    #region UNITY LIFECYCLE

    private void Awake()
    {
        ValidateReferences();
        CaptureNeutralPose(m_LeftHandRoot,  m_LeftHand,  m_LeftState);
        CaptureNeutralPose(m_RightHandRoot, m_RightHand, m_RightState);
    }

    private void Start()
    {
        m_MoveAction = InputSystem.actions.FindAction("Vertical");
        m_TurnAction = InputSystem.actions.FindAction("Horizontal");

        if (m_MoveAction == null)
            Debug.LogError($"ConsoleHandAnimator '{gameObject.name}': " +
                           "InputAction 'Vertical' not found. Check your Input Action Asset.", this);

        if (m_TurnAction == null)
            Debug.LogError($"ConsoleHandAnimator '{gameObject.name}': " +
                           "InputAction 'Horizontal' not found. Check your Input Action Asset.", this);
    }

    private void Update()
    {
        float moveInput = m_MoveAction != null ? m_MoveAction.ReadValue<float>() : 0f;
        float turnInput = m_TurnAction != null ? m_TurnAction.ReadValue<float>() : 0f;

        // Left hand: primary axis is Vertical (forward/back tilt on X), twist on Y
        UpdateHand(
            root:        m_LeftHandRoot,
            settings:    m_LeftHand,
            state:       m_LeftState,
            input:       moveInput,
            primaryAxis: Vector3.right,   // X tilt = forward/back lean
            twistAxis:   Vector3.up       // Y twist = wrist roll
        );

        // Right hand: primary axis is Horizontal (left/right roll on Z), twist on Y
        UpdateHand(
            root:        m_RightHandRoot,
            settings:    m_RightHand,
            state:       m_RightState,
            input:       turnInput,
            primaryAxis: Vector3.forward, // Z tilt = left/right roll
            twistAxis:   Vector3.up
        );
    }

    #endregion

    #region HAND UPDATE

    /// <summary>
    /// Full per-hand update. Computes all motion contributions independently
    /// then composites them into a final localRotation and localPosition.
    ///
    /// Layer order: neutral -> anticipation -> primary tilt -> overshoot -> sway -> twist -> bob
    /// </summary>
    private void UpdateHand(
        Transform        root,
        HandAnimSettings settings,
        HandAnimState    state,
        float            input,
        Vector3          primaryAxis,
        Vector3          twistAxis)
    {
        if (root == null) return;

        float dt             = Time.deltaTime;
        float inputMagnitude = Mathf.Abs(input);

        // ── 1. ANTICIPATION ──────────────────────────────────────────────────────
        // Fire a counter-lean when input direction reverses or starts from rest.
        // Detected by comparing sign of current vs previous input.
        float inputSign    = Mathf.Sign(input);
        float prevSign     = Mathf.Sign(state.PreviousInput);
        bool  dirChanged   = input != 0f
                             && state.PreviousInput != 0f
                             && inputSign != prevSign;
        bool  inputStarted = Mathf.Abs(state.PreviousInput) < 0.05f
                             && inputMagnitude > 0.05f;

        if (dirChanged || inputStarted)
        {
            // Kick counter-lean in the OPPOSITE direction to the incoming input
            state.AnticipationValue = -inputSign
                                      * settings.m_MaxTiltAngle
                                      * settings.m_AnticipationStrength;
        }

        // Decay anticipation toward zero
        state.AnticipationValue = Mathf.Lerp(
            state.AnticipationValue, 0f, dt * settings.m_AnticipationDecay);

        state.PreviousInput = input;

        // ── 2. TARGET TILT (analog-scaled) ───────────────────────────────────────
        // Negative sign so positive input = forward/right lean (feels natural).
        // Anticipation is additive so the counter-lean sits on top of the main tilt.
        float targetTilt = -input * settings.m_MaxTiltAngle + state.AnticipationValue;

        // ── 3. OVERSHOOT ─────────────────────────────────────────────────────────
        // When input releases, inject the tilt direction as a velocity that decays.
        // This carries the hand past neutral then settles — a spring without physics.
        float tiltDelta = targetTilt - state.CurrentTiltAngle;

        if (inputMagnitude < 0.05f)
        {
            // Input released: bleed velocity toward the remaining tilt delta,
            // scaled by overshoot strength
            state.OvershootVelocity = Mathf.Lerp(
                state.OvershootVelocity,
                tiltDelta * settings.m_OvershootStrength,
                dt * settings.m_OvershootDamping
            );
        }
        else
        {
            // Input held: bleed off any leftover overshoot quickly so it doesn't fight the tilt
            state.OvershootVelocity = Mathf.Lerp(
                state.OvershootVelocity, 0f, dt * settings.m_OvershootDamping * 2f);
        }

        float effectiveTarget = targetTilt + state.OvershootVelocity;

        // Smooth current tilt toward effective target
        state.CurrentTiltAngle = Mathf.Lerp(
            state.CurrentTiltAngle, effectiveTarget, dt * settings.m_SmoothSpeed);

        // ── 4. IDLE SWAY ─────────────────────────────────────────────────────────
        // Gentle sine-wave drift on the primary axis.
        // Fades proportionally to zero as inputMagnitude rises so it never fights active input.
        float swayAngle = Mathf.Sin(
                              (Time.time + settings.m_IdleSwayPhase) * settings.m_IdleSwaySpeed)
                          * settings.m_IdleSwayAmount
                          * (1f - inputMagnitude);

        // ── 5. COMPOSITE ROTATION ────────────────────────────────────────────────
        // Stack: neutral base -> primary tilt + sway -> secondary twist
        float totalTilt  = state.CurrentTiltAngle + swayAngle;
        float twistAngle = -input * settings.m_TwistAngle;

        Quaternion tiltRot  = Quaternion.AngleAxis(totalTilt,  primaryAxis);
        Quaternion twistRot = Quaternion.AngleAxis(twistAngle, twistAxis);

        root.localRotation = state.NeutralRotation * tiltRot * twistRot;

        // ── 6. VERTICAL BOB ──────────────────────────────────────────────────────
        // Dips the hand root in local Y proportional to how hard input is held.
        float targetBob = -inputMagnitude * settings.m_BobAmount;

        state.CurrentBobOffset = Mathf.Lerp(
            state.CurrentBobOffset, targetBob, dt * settings.m_BobSmoothSpeed);

        // Final position = scene-placed neutral + inspector offset + bob
        root.localPosition = state.NeutralPosition
                             + settings.m_NeutralPositionOffset
                             + new Vector3(0f, state.CurrentBobOffset, 0f);
    }

    #endregion

    #region SETUP

    /// <summary>
    /// Captures the root's scene-placed localRotation and localPosition as the neutral pose.
    /// All motion is applied as offsets from this, so the designer's scene placement is always respected.
    /// </summary>
    private void CaptureNeutralPose(Transform root, HandAnimSettings settings, HandAnimState state)
    {
        if (root == null) return;

        state.NeutralRotation = root.localRotation;
        state.NeutralPosition = root.localPosition;
    }

    #endregion

    #region VALIDATION

    private void ValidateReferences()
    {
        if (m_LeftHandRoot == null)
            Debug.LogWarning($"ConsoleHandAnimator '{gameObject.name}': " +
                             "m_LeftHandRoot is not assigned. Left hand will not animate.", this);

        if (m_RightHandRoot == null)
            Debug.LogWarning($"ConsoleHandAnimator '{gameObject.name}': " +
                             "m_RightHandRoot is not assigned. Right hand will not animate.", this);
    }

    #endregion

    #region GIZMOS

    private void OnDrawGizmosSelected()
    {
        DrawRootGizmo(m_LeftHandRoot,  Color.green, "L");
        DrawRootGizmo(m_RightHandRoot, Color.red,   "R");
    }

    private void DrawRootGizmo(Transform root, Color color, string label)
    {
        if (root == null) return;

        Gizmos.color = color;
        Vector3 pos  = root.position;
        float   size = 0.05f;

        Gizmos.DrawLine(pos - root.right   * size, pos + root.right   * size);
        Gizmos.DrawLine(pos - root.up      * size, pos + root.up      * size);
        Gizmos.DrawLine(pos - root.forward * size, pos + root.forward * size);
        Gizmos.DrawWireSphere(pos, 0.02f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(pos + Vector3.up * 0.06f, $"{label} Root");
#endif
    }

    #endregion
}