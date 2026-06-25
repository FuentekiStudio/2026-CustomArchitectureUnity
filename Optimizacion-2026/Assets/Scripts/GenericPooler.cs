using System.Collections.Generic;
using UnityEditor;
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
    private IPoolable objectToPool;
    private Transform poolParent;


    public List<IPoolable> activeObjects;
    public List<IPoolable> deactiveObjects;



    public int startAmount = 10;

    public GenericPooler(Transform parent, IPoolable objectToPool, int startAmount = 10){
        this.objectToPool = objectToPool;
        this.poolParent = parent;

        activeObjects = new List<IPoolable>();
        deactiveObjects = new List<IPoolable>();
        this.startAmount = startAmount;
    }

    public void SetUp()
    {
        for (int i=0;i<startAmount ; i++)
        {
            GameObject obj = Object.Instantiate(objectToPool.getPrefab(), poolParent);
            IPoolable objController = objectToPool.getNewControllerInstance(obj);

            deactiveObjects.Add(
                    objController
                );
        }

    }

    public IPoolable getObj()
    {
        IPoolable poolObj = null;

        if (deactiveObjects.Count > 0)
        {
            poolObj = deactiveObjects[0];

        }
        else
        {
            GameObject obj = Object.Instantiate(objectToPool.getPrefab(), poolParent);
            IPoolable objController = objectToPool.getNewControllerInstance(obj);

            poolObj = objController;
        }

        activeObjects.Add( poolObj );

        return poolObj;
    }


    public void returnToPool(IPoolable poolObj)
    {
        activeObjects.Remove(poolObj);
        deactiveObjects.Add(poolObj );
    }
}
