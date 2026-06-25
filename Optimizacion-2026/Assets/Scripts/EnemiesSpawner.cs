using UnityEngine;

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
        EnemiesPool.SetUp();//come muchos frames?? veremos

        customUpdateManager.RegisterUpdateable(this);
    
    }

    public void Update(float deltatime)
    {
        if (Time.time - lastSpawnTime > timerSpawn)
        {
            //CLogger.Log("patata");
            lastSpawnTime = Time.time;
            IPoolable newEnemy =  EnemiesPool.getObj();
            newEnemy.Activate();

        }
    }

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
