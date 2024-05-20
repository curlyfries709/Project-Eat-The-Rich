using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyNetworkManager : MonoBehaviour
{
    private static EnemyNetworkManager instance;
    public static EnemyNetworkManager Instance
    {
        get
        {
            if (instance == null)
            {
                Debug.LogError("EnemyNetworkManager is NULL");
            }

            return instance;
        }
    }

    List<EnemyStateMachine> activeEnemies = new List<EnemyStateMachine>();


    private void Awake()
    {
        instance = this;
    }


    public void OnEnemySpawned(EnemyStateMachine enemyStateMachine)
    {
        activeEnemies.Add(enemyStateMachine);
    }

    public void OnEnemyDeath(EnemyStateMachine enemyStateMachine)
    {
        activeEnemies.Remove(enemyStateMachine);
    }
}
