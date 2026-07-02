using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contrato que debe cumplir cualquier objeto administrado por GenericPooler.
/// </summary>
public interface IPoolable
{
    public GameObject getPrefab();
    public void Activate();
    public void Deactivate();
    public IPoolable getNewControllerInstance(GameObject newObject);
}

/// <summary>
/// Pool genérico de objetos IPoolable. Lo usa PoolService para reciclar prefabs durante gameplay.
/// </summary>
public class GenericPooler
{
    private readonly IPoolable objectToPool;
    private readonly Transform poolParent;

    public List<IPoolable> activeObjects;
    public List<IPoolable> deactiveObjects;

    public int startAmount = 10;

    /// <summary>
    /// Recibe la plantilla a clonar, el padre de escena y la cantidad inicial del pool.
    /// </summary>
    public GenericPooler(Transform parent, IPoolable objectToPool, int startAmount = 10)
    {
        this.objectToPool = objectToPool;
        poolParent = parent;
        activeObjects = new List<IPoolable>();
        deactiveObjects = new List<IPoolable>();
        this.startAmount = startAmount;
    }

    /// <summary>
    /// Instancia y desactiva todos los objetos iniciales del pool.
    /// </summary>
    public void SetUp()
    {
        for (int i = 0; i < startAmount; i++)
        {
            GameObject obj = Object.Instantiate(objectToPool.getPrefab(), poolParent);
            IPoolable objController = objectToPool.getNewControllerInstance(obj);
            objController.Deactivate();
            deactiveObjects.Add(objController);
        }
    }

    /// <summary>
    /// Entrega un objeto inactivo y lo mueve a la lista de activos.
    /// </summary>
    public IPoolable getObj()
    {
        IPoolable poolObj;

        if (deactiveObjects.Count > 0)
        {
            poolObj = deactiveObjects[0];
            deactiveObjects.RemoveAt(0);
        }
        else
        {
            Debug.LogWarning("GenericPooler.getObj: pool exhausted. Increase prewarmCount to avoid runtime allocations.");
            return null;
        }

        if (!activeObjects.Contains(poolObj))
        {
            activeObjects.Add(poolObj);
        }

        return poolObj;
    }

    /// <summary>
    /// Desactiva un objeto y lo devuelve a la lista de disponibles.
    /// </summary>
    public void returnToPool(IPoolable poolObj)
    {
        if (poolObj == null)
        {
            return;
        }

        poolObj.Deactivate();
        activeObjects.Remove(poolObj);

        if (!deactiveObjects.Contains(poolObj))
        {
            deactiveObjects.Add(poolObj);
        }
    }

    /// <summary>
    /// Devuelve todos los objetos activos al pool.
    /// </summary>
    public void ReturnAll()
    {
        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            returnToPool(activeObjects[i]);
        }
    }
}
