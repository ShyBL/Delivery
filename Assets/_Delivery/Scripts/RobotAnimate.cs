using UnityEngine;

public class RobotAnimate : MonoBehaviour
{
    private Animator m_Animator;

    public void AnimatePickup()
    {
        m_Animator = GetComponent<Animator>();
        m_Animator.SetTrigger("Pickup");
    }
}