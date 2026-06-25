using Unity.VisualScripting;
using UnityEngine;




public class Enemy : IFixedUpdateable, IPoolable
{
    private CustomUpdateManager customUpdateManager;
    private GameObject selfObject;
    private EnemiesSpawner spawner;

    public float speed = 1.0f;

    public Enemy(
                CustomUpdateManager customUpdateManager,
                GameObject selfObject,
                EnemiesSpawner spawner)
    {
        this.customUpdateManager = customUpdateManager;
        this.selfObject = selfObject;
        this.spawner = spawner;

    }
    public void FixedUpdate(float deltaTime)
    {
        float newZ = selfObject.transform.position.z - deltaTime * speed;
        selfObject.transform.position = new Vector3(
            selfObject.transform.position.x,
            selfObject.transform.position.y, 
            newZ
            );
    }

    public IPoolable getNewControllerInstance(GameObject newObject)
    {
        return new Enemy(customUpdateManager, newObject, spawner);// devuelvo un nuevo controllador para un nuevo prefab
    }

    public GameObject getPrefab()
    {
        return selfObject;//devuelvo el prefab del objeto
    }
    public void Activate()
    {
        selfObject.transform.position = spawner.getRandomSpawnPoint();
        customUpdateManager.RegisterFixedUpdateable(this);
        selfObject.SetActive(true);
    }
    public void Deactivate()
    {
        customUpdateManager.UnregisterFixedUpdateable(this);
        selfObject.SetActive(false);
    }
}
