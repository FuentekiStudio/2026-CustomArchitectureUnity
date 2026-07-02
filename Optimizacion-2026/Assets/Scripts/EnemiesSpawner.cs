using UnityEngine;

/// <summary>
/// Spawner de enemigos del prototipo inicial. El flujo principal actual usa WaveSystem, SpawnSystem y EnemySystem.
/// </summary>
public class EnemiesSpawner : IUpdateable
{
    private CustomUpdateManager customUpdateManager;
    private Transform SpawnParent;
    private GameObject enemyPrefab;
    private Enemy baseEnemyController;

    private GenericPooler EnemiesPool;

    public float timerSpawn = 1.0f;
    private float lastSpawnTime = 0;


    private float rangeSpawnPoint = 5.0f;


    /// <summary>
    /// Configura un GenericPooler propio del prototipo inicial y se registra en el CustomUpdateManager.
    /// </summary>
    public EnemiesSpawner(
        CustomUpdateManager customUpdateManager,
        Transform parent, 
        GameObject prefab  
        )
    {
        this.SpawnParent = parent;
        this.enemyPrefab = prefab;
        this.customUpdateManager = customUpdateManager;
        this.lastSpawnTime = Time.time;

        baseEnemyController = new Enemy(customUpdateManager, prefab, this);

        EnemiesPool = new GenericPooler(this.SpawnParent, baseEnemyController);
        EnemiesPool.SetUp();// Precarga este pool del prototipo inicial antes de empezar a spawnear.

        customUpdateManager.RegisterUpdateable(this);
    
    }

    /// <summary>
    /// Spawnea un enemigo del prototipo inicial cada vez que vence el timer.
    /// </summary>
    public void Update(float deltatime)
    {
        if (Time.time - lastSpawnTime > timerSpawn)
        {
            // Log opcional para depurar cada spawn del prototipo inicial.
            lastSpawnTime = Time.time;
            IPoolable newEnemy =  EnemiesPool.getObj();
            newEnemy.Activate();

        }
    }

    /// <summary>
    /// Calcula una posición aleatoria alrededor del punto padre de spawn.
    /// </summary>
    public Vector3 getRandomSpawnPoint()
    {
        float spawnXoffset = Random.Range(-rangeSpawnPoint, rangeSpawnPoint);

        return new Vector3(
                SpawnParent.position.x + spawnXoffset,
                SpawnParent.position.y,
                SpawnParent.position.z
                );
    }
}
