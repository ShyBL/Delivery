using UnityEngine;
using UnityEngine.InputSystem;

public class RobotMove : MonoBehaviour
{
    [SerializeField] private float m_Speed = 12f; 
    [SerializeField] private float m_TurnSpeed = 180f;
    [SerializeField] private GameObject model;
        
    private InputActionAsset m_LocalActionAsset;
    private InputAction m_MoveAction;        
    private InputAction m_TurnAction;
    private float m_MovementInputValue; 
    private float m_TurnInputValue; 
    private string m_MovementAxisName;
    private string m_TurnAxisName;   
    
    private Rigidbody m_Rigidbody;  
    private Vector3 m_ExplosionForceValue;   
    private void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        m_MovementAxisName = "Vertical";
        m_TurnAxisName = "Horizontal";
        m_MoveAction = InputSystem.actions.FindAction(m_MovementAxisName);
        m_TurnAction = InputSystem.actions.FindAction(m_TurnAxisName);
    }

    private void Update()
    {
        m_MovementInputValue = m_MoveAction.ReadValue<float>();
        m_TurnInputValue = m_TurnAction.ReadValue<float>();
    }
    
    private void FixedUpdate()
    {
        Move();
        Turn();
        AlignToGround();
    }

    private void AlignToGround()
    {
        Ray ray = new Ray(model.transform.position + Vector3.up * 0.1f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 2f))
        {
            // The normal of the ground
            Vector3 groundNormal = hit.normal;

            // Create a rotation that aligns the robot's up direction with the ground normal
            Quaternion targetRotation = Quaternion.FromToRotation(model.transform.up, groundNormal) * model.transform.rotation;

            // Smoothly rotate to match the slope
            model.transform.rotation = Quaternion.Slerp(model.transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }

    
    private void Move()
    {
        float speedInput = 0.0f;
        speedInput = m_MovementInputValue;
        Vector3 movement = transform.forward * speedInput * m_Speed;

        m_Rigidbody.linearVelocity = movement + m_ExplosionForceValue;
        m_ExplosionForceValue = Vector3.Lerp(m_ExplosionForceValue, Vector3.zero, Time.deltaTime * 3f); // 3f = braking speed
    }
    
    private void Turn()
    {
        Quaternion turnRotation;
        float turn = m_TurnInputValue * m_TurnSpeed * Time.deltaTime;

        turnRotation = Quaternion.Euler(0f, turn, 0f);
        m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
    }


}