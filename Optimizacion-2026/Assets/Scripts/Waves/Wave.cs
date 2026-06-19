using System.Collections.Generic;
using UnityEngine;

public class Wave
{
    /*
    private int maxEnemies;
    private int enemiesLeft;
    private int enemiesSpawned;
    private List<Enemy> enemiesRef;          //lista de enemigos
    private List<EnemyTypes> enemyTypes;     //lista de tipos de enemigo (para ver cual spawnear)

    private EnemySpawner enemySpawner;       //factory/pool de enemigos

    private float currentTime;

    private float spawnTime;
    private float randomTime;

    private bool isWaveCompleted;

    public int EnemiesLeft => enemiesLeft;
    public bool IsWaveCompleted => isWaveCompleted;

    public Wave(int maxEnemies, float enemySpawnTime, List<EnemyTypes> enemyTypes)
    {
        this.maxEnemies = maxEnemies;

        spawnTime = enemySpawnTime;

        this.enemyTypes = enemyTypes;
    }

    public void Initialize()
    {
        enemiesRef = Level.ObjectsController.Enemies;
        enemySpawner = new EnemySpawner(enemyTypes, Level.ObjectsController.Garden, enemiesRef);

        isWaveCompleted = false;
        randomTime = 4;
    }

    public void Update()
    {
        if (enemiesSpawned < maxEnemies)
        {
            FillUpEnemyList();
        }
        else
        {
            CheckEnemiesLeft();
        }
    }

    private void FillUpEnemyList()
    {
        currentTime += Time.Deltatime;

        if (currentTime >= randomTime)
        {
            enemySpawner.SpawnEnemies();

            enemiesSpawned++;

            //randomTime = (float)Engine.GetRandomDouble() * spawnTime + 1;
            currentTime = 0;
        }

    }

    private void CheckEnemiesLeft()
    {
        enemiesLeft = enemiesRef.Count;

        if (enemiesLeft == 0)
        {
            isWaveCompleted = true;
        }
    }
    */
}
