using UnityEngine;

/// <summary>
/// Controlador pooleable de un ParticleSystem. SpawnSystem lo activa y VfxSystem lo recicla cuando termina.
/// </summary>
public class Vfx : IPoolable
{
    private ParticleSystem particleSystemRef;
    private GameObject selfGO;
    public bool isActive;
    public PoolId id;
    public IPoolable poolable;

    /// <summary>
    /// Guarda referencias necesarias para controlar y devolver el efecto al pool correcto.
    /// </summary>
    public Vfx(GameObject selfGO, PoolId id, IPoolable poolable)
    {
        this.selfGO = selfGO;
        particleSystemRef = this.selfGO.GetComponent<ParticleSystem>();
        isActive = true;
        this.id = id;
        this.poolable = poolable;
    }

    /// <summary>
    /// Indica si el ParticleSystem terminó su reproducción.
    /// </summary>
    public bool CheckIsStopped()
    {
        return particleSystemRef.isStopped;
    }

    /// <summary>
    /// Devuelve el GameObject asociado para cumplir IPoolable.
    /// </summary>
    public GameObject getPrefab()
    {
        return selfGO;
    }

    /// <summary>
    /// Activa el GameObject y reproduce la partícula.
    /// </summary>
    public void Activate()
    {
        selfGO.SetActive(true);
        particleSystemRef.Play();
    }

    /// <summary>
    /// Detiene la partícula y desactiva el GameObject.
    /// </summary>
    public void Deactivate()
    {
        particleSystemRef.Stop();
        selfGO.SetActive(false);
    }

    /// <summary>
    /// Crea el controlador para una nueva instancia generada por GenericPooler.
    /// </summary>
    public IPoolable getNewControllerInstance(GameObject newObject)
    {
        return new Vfx(newObject, id, poolable);
    }
}
