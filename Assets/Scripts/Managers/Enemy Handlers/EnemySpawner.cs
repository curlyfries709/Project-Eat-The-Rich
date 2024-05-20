using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] CombatWave[] combatWaves;

    int numOfEnemies = 0;
    int currentWaveIndex = 0;

    /* 
     * 
     * o	Has Data about each wave.
o	Number Of Waves.
o	Number Of Enemies per waves. 
o	Events triggered. 
o	When each enemy spawned must pass them to EnemyNetworkManager

     * 
     * */

    private void Start()
    {
        EnemyStateMachine.EnemyDead += OnEnemyDeath;
        SpawnEnemies();
    }

    public void SpawnEnemies()
    {
        CombatWave currentWave = combatWaves[currentWaveIndex];

        foreach(EnemyWaveData waveData in currentWave.waveEnemies)
        {
            for (int i = 0;  i < waveData.numOfEnemiesToSpawn; i++)
            {
                GameObject spawnedEnemy = Instantiate(waveData.enemyPrefab, waveData.spawnPoint.position, Quaternion.identity);
                EnemyNetworkManager.Instance.OnEnemySpawned(spawnedEnemy.GetComponent<EnemyStateMachine>());

                numOfEnemies++;
            }
        }
    }

    private void OnEnemyDeath(EnemyStateMachine enemyStateMachine)
    {
        numOfEnemies = numOfEnemies - 1;

        CombatWave currentWave = combatWaves[currentWaveIndex];

        Debug.Log("Num Of Enemies: " + numOfEnemies);

        if (numOfEnemies <= currentWave.numOfEnemiesRemainingToSpawnNextWave)
        {
            if (currentWave.OnWaveCompleteEvents.GetPersistentEventCount() == 0)
            {
                BeginNextWave();
            }
            else
            {
                currentWave.OnWaveCompleteEvents.Invoke();
            }
        }
    }

    public void BeginNextWave()
    {
        if (currentWaveIndex + 1 < combatWaves.Length)
        {
            currentWaveIndex++;
            SpawnEnemies();
        }
        else
        {
            //Call Combat Ended Event
        }
    }
}

[System.Serializable]
public class CombatWave
{
    public int numOfEnemiesRemainingToSpawnNextWave;
    [Space(10)]
    public EnemyWaveData[] waveEnemies;
    public UnityEvent OnWaveCompleteEvents = null;
}

[System.Serializable]
public class EnemyWaveData
{
    public int numOfEnemiesToSpawn;
    public GameObject enemyPrefab;
    public Transform spawnPoint;
}
