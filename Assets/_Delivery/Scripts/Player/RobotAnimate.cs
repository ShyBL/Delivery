using UnityEngine;

public class RobotAnimate : MonoBehaviour
{
    [Header("Animation Properties")]
    [SerializeField] private string m_PickupAnimationName;
    
    private Animator m_Animator;

    public void AnimatePickup()
    {
        m_Animator = GetComponent<Animator>();
        m_Animator.SetTrigger(m_PickupAnimationName);
    }
}