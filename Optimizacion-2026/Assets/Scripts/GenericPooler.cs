using System.Collections.Generic;
using UnityEngine;

public interface IPoolable
{
    public GameObject getPrefab();
    public void Activate();
    public void Deactivate();
    public IPoolable getNewControllerInstance(GameObject newObject);
}

public class GenericPooler
{
    private readonly IPoolable objectToPool;
    private readonly Transform poolParent;

    public List<IPoolable> activeObjects;
    public List<IPoolable> deactiveObjects;

    public int startAmount = 10;

    public GenericPooler(Transform parent, IPoolable objectToPool, int startAmount = 10)
    {
        this.objectToPool = objectToPool;
        poolParent = parent;
        activeObjects = new List<IPoolable>();
        deactiveObjects = new List<IPoolable>();
        this.startAmount = startAmount;
    }

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

    public void ReturnAll()
    {
        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            returnToPool(activeObjects[i]);
        }
    }
}
