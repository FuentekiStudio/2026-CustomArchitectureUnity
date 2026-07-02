using UnityEngine;

public class Vfx : IPoolable
{
    private ParticleSystem particleSystemRef;
    private GameObject selfGO;
    public bool isActive;
    public PoolId id;
    public IPoolable poolable;

    public Vfx()
    {
    }

    public Vfx(GameObject selfGO, PoolId id, IPoolable poolable)
    {
        Initialize(selfGO, id, poolable);
    }

    public void Initialize(GameObject selfGO, PoolId id, IPoolable poolable)
    {
        this.selfGO = selfGO;
        particleSystemRef = this.selfGO.GetComponent<ParticleSystem>();
        isActive = true;
        this.id = id;
        this.poolable = poolable;
    }

    public bool CheckIsStopped()
    {
        return particleSystemRef.isStopped;
    }

    public GameObject getPrefab()
    {
        return selfGO;
    }

    public void Activate()
    {
        selfGO.SetActive(true);
        particleSystemRef.Play();
    }

    public void Deactivate()
    {
        particleSystemRef.Stop();
        selfGO.SetActive(false);
    }

    public IPoolable getNewControllerInstance(GameObject newObject)
    {
        return new Vfx(newObject, id, poolable);
    }
}
