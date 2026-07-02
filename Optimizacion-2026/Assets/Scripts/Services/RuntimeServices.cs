using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Servicio de tiempo usado por GameStateSystem para pausar y consultar tiempos de Unity desde clases puras.
/// </summary>
public sealed class TimeService
{
    public float DeltaTime => Time.deltaTime;
    public float FixedDeltaTime => Time.fixedDeltaTime;

    /// <summary>
    /// Cambia Time.timeScale para pausar o reanudar la simulación de Unity.
    /// </summary>
    public void SetPaused(bool paused)
    {
        Time.timeScale = paused ? 0f : 1f;
    }
}

/// <summary>
/// Servicio de entrada que abstrae teclado y mouse para PlayerSystem.
/// Soporta Input System nuevo y Legacy Input si están habilitados.
/// </summary>
public sealed class InputSystemService
{
    /// <summary>
    /// Devuelve el eje lateral del jugador usando A/D o flechas.
    /// </summary>
    public float GetHorizontal()
    {
        float keyboard = 0f;

#if ENABLE_INPUT_SYSTEM
        Keyboard currentKeyboard = Keyboard.current;
        if (currentKeyboard != null)
        {
            if (currentKeyboard.aKey.isPressed || currentKeyboard.leftArrowKey.isPressed)
            {
                keyboard -= 1f;
            }

            if (currentKeyboard.dKey.isPressed || currentKeyboard.rightArrowKey.isPressed)
            {
                keyboard += 1f;
            }
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            keyboard -= 1f;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            keyboard += 1f;
        }
#endif

        return Mathf.Clamp(keyboard, -1f, 1f);
    }

    /// <summary>
    /// Indica si el jugador está disparando con click izquierdo o barra espaciadora.
    /// </summary>
    public bool IsShooting()
    {
        bool shooting = false;

#if ENABLE_INPUT_SYSTEM
        Mouse currentMouse = Mouse.current;
        Keyboard currentKeyboard = Keyboard.current;
        shooting |= currentMouse != null && currentMouse.leftButton.isPressed;
        shooting |= currentKeyboard != null && currentKeyboard.spaceKey.isPressed;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        shooting |= Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0);
#endif

        return shooting;
    }

    /// <summary>
    /// Indica si se presionó Escape durante este frame.
    /// </summary>
    public bool PausePressed()
    {
        bool pressed = false;

#if ENABLE_INPUT_SYSTEM
        Keyboard currentKeyboard = Keyboard.current;
        pressed |= currentKeyboard != null && currentKeyboard.escapeKey.wasPressedThisFrame;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        pressed |= Input.GetKeyDown(KeyCode.Escape);
#endif

        return pressed;
    }
}

/// <summary>
/// Tabla que relaciona Colliders de Unity con entidades runtime. La usa CollisionSystem para resolver impactos.
/// </summary>
public sealed class PhysicsRegistry
{
    private readonly Dictionary<Collider, EntityRef> colliderToEntity = new Dictionary<Collider, EntityRef>();

    /// <summary>
    /// Asocia un collider activo con una entidad de gameplay.
    /// </summary>
    public void Register(Collider collider, EntityRef entity)
    {
        if (collider != null)
        {
            colliderToEntity[collider] = entity;
        }
    }

    /// <summary>
    /// Quita la asociación de un collider cuando la entidad se recicla.
    /// </summary>
    public void Unregister(Collider collider)
    {
        if (collider != null)
        {
            colliderToEntity.Remove(collider);
        }
    }

    /// <summary>
    /// Busca qué entidad runtime pertenece al collider detectado por física.
    /// </summary>
    public bool Resolve(Collider collider, out EntityRef entity)
    {
        return colliderToEntity.TryGetValue(collider, out entity);
    }

    /// <summary>
    /// Limpia todas las asociaciones de colliders al reiniciar gameplay.
    /// </summary>
    public void Clear()
    {
        colliderToEntity.Clear();
    }
}

/// <summary>
/// Servicio de pools usado por SpawnSystem y sistemas de reciclado para evitar Instantiate/Destroy durante gameplay.
/// </summary>
public sealed class PoolService
{
    private readonly Dictionary<PoolId, GenericPooler> pools = new Dictionary<PoolId, GenericPooler>();

    /// <summary>
    /// Crea los pools declarados en GameConfig y precarga sus objetos bajo poolRoot.
    /// </summary>
    public void Prewarm(GameConfig config, Transform poolRoot)
    {
        if (config == null || config.pools == null || config.pools.entries == null)
        {
            return;
        }

        for (int i = 0; i < config.pools.entries.Length; i++)
        {
            PoolEntry entry = config.pools.entries[i];
            if (entry.prefab == null)
            {
                continue;
            }

            int prewarmCount = Mathf.Max(0, entry.prewarmCount);
            PoolableGameObject template = new PoolableGameObject(entry.prefab);
            GenericPooler pool = new GenericPooler(poolRoot, template, prewarmCount);
            pool.SetUp();
            pools[entry.id] = pool;
        }
    }

    /// <summary>
    /// Obtiene un objeto inactivo del pool indicado.
    /// </summary>
    public IPoolable Get(PoolId poolId)
    {
        if (!pools.TryGetValue(poolId, out GenericPooler pool))
        {
            Debug.LogWarning($"PoolService.Get: pool {poolId} is not registered.");
            return null;
        }

        return pool.getObj();
    }

    /// <summary>
    /// Devuelve un objeto activo a su pool o lo desactiva si el pool no existe.
    /// </summary>
    public void Return(PoolId poolId, IPoolable poolable)
    {
        if (poolable == null)
        {
            return;
        }

        if (pools.TryGetValue(poolId, out GenericPooler pool))
        {
            pool.returnToPool(poolable);
        }
        else
        {
            poolable.Deactivate();
        }
    }

    /// <summary>
    /// Devuelve todos los objetos activos de todos los pools.
    /// </summary>
    public void ReturnAll()
    {
        foreach (GenericPooler pool in pools.Values)
        {
            pool.ReturnAll();
        }
    }
}

/// <summary>
/// Adaptador que permite tratar un GameObject de Unity como objeto pooleable.
/// </summary>
public sealed class PoolableGameObject : IPoolable
{
    private readonly GameObject prefab;
    private readonly GameObject instance;

    /// <summary>
    /// Crea el wrapper usado como plantilla para instanciar objetos del pool.
    /// </summary>
    public PoolableGameObject(GameObject prefab)
    {
        this.prefab = prefab;
        instance = prefab;
    }

    private PoolableGameObject(GameObject prefab, GameObject instance)
    {
        this.prefab = prefab;
        this.instance = instance;
    }

    public GameObject Instance => instance;

    /// <summary>
    /// Devuelve el prefab usado para crear instancias del pool.
    /// </summary>
    public GameObject getPrefab()
    {
        return prefab;
    }

    /// <summary>
    /// Activa la instancia asociada en escena.
    /// </summary>
    public void Activate()
    {
        if (instance != null)
        {
            instance.SetActive(true);
        }
    }

    /// <summary>
    /// Desactiva la instancia asociada en escena.
    /// </summary>
    public void Deactivate()
    {
        if (instance != null)
        {
            instance.SetActive(false);
        }
    }

    /// <summary>
    /// Crea el wrapper correspondiente para una nueva instancia generada por GenericPooler.
    /// </summary>
    public IPoolable getNewControllerInstance(GameObject newObject)
    {
        return new PoolableGameObject(prefab, newObject);
    }
}
