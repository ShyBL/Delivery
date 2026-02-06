using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    public RobotAnimate robotAnimate;
    public RobotMove robotMove;
    private int m_Packages;
    
    public void OnPackageCollected()
    {
        RegisterPickup();
        robotAnimate.AnimatePickup();
    }
    
    public void ToggleControl(bool value)
    {
        if (robotMove != null)
            robotMove.enabled = value;
    }
    
    private void RegisterPickup()
    {
        m_Packages++;
    }
    
    public int GetPackagesStoredCount()
    {
        return m_Packages;
    }
}