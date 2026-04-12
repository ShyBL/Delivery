using System;
using FMODUnity;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the player-controlled delivery bot.
/// Tracks collected packages, handles collection events, and manages player control state.
/// </summary>
public class Player : MonoBehaviour
{
    public RobotAnimate robotAnimate;
    public RobotMove robotMove;
    public ResourcesInventory robotInventory;

    private void Start()
    {
        // TODO: Temp way to spawn all resources
        foreach (var item in FindObjectsOfType<ResourceSpawner>())
        {
            item.TriggerSpawn();
        }
    }

    /// <summary>
    /// Enables or disables player movement control.
    /// </summary>
    /// <param name="value">True to enable control, false to disable.</param>
    public void ToggleControl(bool value)
    {
        if (robotMove != null)
            robotMove.enabled = value;
    }
    
   
}


