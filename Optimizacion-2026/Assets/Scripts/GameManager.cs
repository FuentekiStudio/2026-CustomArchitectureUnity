using UnityEngine;

public class GameManager : MonoBehaviour
{

    [SerializeField] private CustomUpdateManager customUpdateManager;

    [SerializeField] private Transform SpawnerPoint;
    [SerializeField] private GameObject EnemyPrefab;

    private EnemiesSpawner EnemiesSpawner;


    private void Awake()
    {
        EnemiesSpawner = new EnemiesSpawner(
            customUpdateManager
            ,SpawnerPoint
            ,EnemyPrefab 
            );

    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
