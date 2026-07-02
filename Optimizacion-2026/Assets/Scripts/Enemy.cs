using Unity.VisualScripting;
using UnityEngine;




/// <summary>
/// Controlador de enemigo del prototipo inicial. El flujo principal actual usa EnemyEntity, EnemySystem y SpawnSystem.
/// </summary>
public class Enemy : IFixedUpdateable, IPoolable
{
    private CustomUpdateManager customUpdateManager;
    private GameObject selfObject;
    private EnemiesSpawner spawner;

    public float speed = 1.0f;

    /// <summary>
    /// Recibe el update manager, el GameObject controlado y el spawner que define su posición inicial.
    /// </summary>
    public Enemy(
                CustomUpdateManager customUpdateManager,
                GameObject selfObject,
                EnemiesSpawner spawner)
    {
        this.customUpdateManager = customUpdateManager;
        this.selfObject = selfObject;
        this.spawner = spawner;

    }
    /// <summary>
    /// Mueve este enemigo del prototipo inicial hacia atrás en el eje Z.
    /// </summary>
    public void FixedUpdate(float deltaTime)
    {
        float newZ = selfObject.transform.position.z - deltaTime * speed;
        selfObject.transform.position = new Vector3(
            selfObject.transform.position.x,
            selfObject.transform.position.y, 
            newZ
            );
    }

    /// <summary>
    /// Crea un controlador para una nueva instancia del pool usado por el prototipo inicial.
    /// </summary>
    public IPoolable getNewControllerInstance(GameObject newObject)
    {
        return new Enemy(customUpdateManager, newObject, spawner);// Devuelve un controlador nuevo para la instancia del prefab.
    }

    /// <summary>
    /// Devuelve el prefab o GameObject asociado a este controlador del prototipo inicial.
    /// </summary>
    public GameObject getPrefab()
    {
        return selfObject;// Devuelve el prefab u objeto base asociado.
    }
    /// <summary>
    /// Reposiciona, registra en FixedUpdate y activa este enemigo del prototipo inicial.
    /// </summary>
    public void Activate()
    {
        selfObject.transform.position = spawner.getRandomSpawnPoint();
        customUpdateManager.RegisterFixedUpdateable(this);
        selfObject.SetActive(true);
    }
    /// <summary>
    /// Desregistra del FixedUpdate y desactiva este enemigo del prototipo inicial.
    /// </summary>
    public void Deactivate()
    {
        customUpdateManager.UnregisterFixedUpdateable(this);
        selfObject.SetActive(false);
    }
}
