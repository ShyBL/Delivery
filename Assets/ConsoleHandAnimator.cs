using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Animates two hand root Transforms on a handheld console model to visually
/// represent player input — no rigging required.
///
/// Left hand tilts forward/back on its local X axis → Vertical input (movement).
/// Right hand tilts left/right on its local Z axis → Horizontal input (turning).
///
/// Setup:
///   Each hand mesh should be placed inside a root Transform:
///
///     ConsoleObject
///     ├── LeftHandRoot          ← assign to m_LeftHandRoot  (script rotates this)
///     │   └── LeftHandMesh     ← your mesh, already at localRotation -90 X, pivot at thumb base
///     └── RightHandRoot         ← assign to m_RightHandRoot (script rotates this)
///         └── RightHandMesh    ← same setup
///
///   The script never touches the mesh child's transform — only the root.
///   This keeps your mesh's -90 X offset and thumb-pivot positioning intact.
///
/// Mirrors the single-responsibility pattern of RobotMove: reads the same
/// Input System actions but owns only the visual response, not game movement.
/// </summary>
public class ConsoleHandAnimator : MonoBehaviour
{
    [Header("Hand Roots")]
    [Tooltip("Root Transform wrapping the left hand mesh. Script rotates this.")]
    [SerializeField] private Transform m_LeftHandRoot;

    [Tooltip("Root Transform wrapping the right hand mesh. Script rotates this.")]
    [SerializeField] private Transform m_RightHandRoot;

    [Header("Tilt Settings")]
    [Tooltip("Maximum tilt angle in degrees applied to each hand at full input.")]
    [SerializeField, Range(0f, 45f)] private float m_MaxTiltAngle = 15f;

    [Tooltip("How quickly the hands rotate toward the target angle. Higher = snappier.")]
    [SerializeField, Range(1f, 20f)] private float m_SmoothSpeed = 8f;

    // Input actions — same axis names as RobotMove so no remapping is needed
    private InputAction m_MoveAction;
    private InputAction m_TurnAction;

    // Cached neutral local rotations of each root, captured in Awake
    // so we always tilt relative to whatever the designer placed the root at
    private Quaternion m_LeftHandNeutral;
    private Quaternion m_RightHandNeutral;

    #region UNITY LIFECYCLE

    private void Awake()
    {
        ValidateReferences();

        // Cache the designer-placed neutral rotations of the roots.
        // Tilt is applied as an offset from these, not from Quaternion.identity,
        // so the roots can be freely positioned/rotated in the scene.
        if (m_LeftHandRoot != null)
            m_LeftHandNeutral = m_LeftHandRoot.localRotation;

        if (m_RightHandRoot != null)
            m_RightHandNeutral = m_RightHandRoot.localRotation;
    }

    private void Start()
    {
        // Reuse the same axis names RobotMove binds to — no duplication needed
        m_MoveAction = InputSystem.actions.FindAction("Vertical");
        m_TurnAction = InputSystem.actions.FindAction("Horizontal");

        if (m_MoveAction == null)
            Debug.LogError($"ConsoleHandAnimator on '{gameObject.name}': " +
                           "Could not find InputAction 'Vertical'. " +
                           "Check your Input Action Asset action names.", this);

        if (m_TurnAction == null)
            Debug.LogError($"ConsoleHandAnimator on '{gameObject.name}': " +
                           "Could not find InputAction 'Horizontal'. " +
                           "Check your Input Action Asset action names.", this);
    }

    private void Update()
    {
        float moveInput = m_MoveAction != null ? m_MoveAction.ReadValue<float>() : 0f;
        float turnInput = m_TurnAction != null ? m_TurnAction.ReadValue<float>() : 0f;

        UpdateLeftHand(moveInput);
        UpdateRightHand(turnInput);
    }

    #endregion

    #region HAND UPDATE

    /// <summary>
    /// Tilts the left hand root forward (positive input) or back (negative input)
    /// around its local X axis to represent forward/back movement.
    ///
    /// Local X tilt on the root = nodding motion from the hand's perspective.
    /// The mesh child's -90 X offset means this maps cleanly to a forward lean
    /// relative to the camera without any additional correction.
    /// </summary>
    private void UpdateLeftHand(float moveInput)
    {
        if (m_LeftHandRoot == null) return;

        // Positive move input  → tilt forward  (negative X angle = forward lean in Unity)
        // Negative move input  → tilt backward
        float targetAngle = -moveInput * m_MaxTiltAngle;
        Quaternion tilt = Quaternion.AngleAxis(targetAngle, Vector3.right);
        Quaternion targetRotation = m_LeftHandNeutral * tilt;

        m_LeftHandRoot.localRotation = Quaternion.Slerp(
            m_LeftHandRoot.localRotation,
            targetRotation,
            Time.deltaTime * m_SmoothSpeed
        );
    }

    /// <summary>
    /// Tilts the right hand root left or right around its local Z axis
    /// to represent turning input.
    ///
    /// Local Z tilt on the root = rolling/banking motion from the hand's perspective,
    /// which reads as a natural thumb-press lean when viewed from the front camera.
    /// </summary>
    private void UpdateRightHand(float turnInput)
    {
        if (m_RightHandRoot == null) return;

        // Positive turn input → tilt right (negative Z angle in Unity local space)
        // Negative turn input → tilt left
        float targetAngle = -turnInput * m_MaxTiltAngle;
        Quaternion tilt = Quaternion.AngleAxis(targetAngle, Vector3.forward);
        Quaternion targetRotation = m_RightHandNeutral * tilt;

        m_RightHandRoot.localRotation = Quaternion.Slerp(
            m_RightHandRoot.localRotation,
            targetRotation,
            Time.deltaTime * m_SmoothSpeed
        );
    }

    #endregion

    #region VALIDATION

    private void ValidateReferences()
    {
        if (m_LeftHandRoot == null)
            Debug.LogWarning($"ConsoleHandAnimator on '{gameObject.name}': " +
                             "m_LeftHandRoot is not assigned. Left hand will not animate.", this);

        if (m_RightHandRoot == null)
            Debug.LogWarning($"ConsoleHandAnimator on '{gameObject.name}': " +
                             "m_RightHandRoot is not animate. Right hand will not animate.", this);
    }

    #endregion

    #region GIZMOS

    private void OnDrawGizmosSelected()
    {
        // Draw a small axis cross at each hand root so you can see their pivot in the scene view
        DrawRootGizmo(m_LeftHandRoot, Color.green, "L");
        DrawRootGizmo(m_RightHandRoot, Color.red, "R");
    }

    private void DrawRootGizmo(Transform root, Color color, string label)
    {
        if (root == null) return;

        Gizmos.color = color;
        Vector3 pos = root.position;
        float size = 0.05f;

        Gizmos.DrawLine(pos - root.right * size, pos + root.right * size);
        Gizmos.DrawLine(pos - root.up * size, pos + root.up * size);
        Gizmos.DrawLine(pos - root.forward * size, pos + root.forward * size);
        Gizmos.DrawWireSphere(pos, 0.02f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(pos + Vector3.up * 0.06f, $"{label} Root");
#endif
    }

    #endregion
}