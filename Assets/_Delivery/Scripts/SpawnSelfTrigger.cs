using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnSelfTrigger : MonoBehaviour
{
    private PackageSpawner m_PackageSpawner;
    public bool onStart = true; 
    
    private void Awake()
    {
        m_PackageSpawner = GetComponent<PackageSpawner>();
    }

    private void Start()
    {
        if (onStart)
        {
            m_PackageSpawner.TriggerSpawn();
        }
    }
}
