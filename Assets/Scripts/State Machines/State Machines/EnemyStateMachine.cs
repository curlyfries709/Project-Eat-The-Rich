using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class EnemyStateMachine : StateMachine
{
    public static Action<EnemyStateMachine> EnemyDead;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDisable()
    {
        //TEST CODE
        EnemyDead(this);
    }
}
