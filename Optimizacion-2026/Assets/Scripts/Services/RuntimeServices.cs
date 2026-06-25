using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class TimeService
{
    public float DeltaTime => Time.deltaTime;
    public float FixedDeltaTime => Time.fixedDeltaTime;

    public void SetPaused(bool paused)
    {
        Time.timeScale = paused ? 0f : 1f;
    }
}

public sealed class InputSystemService
{
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

public sealed class PhysicsRegistry
{
    private readonly Dictionary<Collider, EntityRef> colliderToEntity = new Dictionary<Collider, EntityRef>();

    public void Register(Collider collider, EntityRef entity)
    {
        if (collider != null)
        {
            colliderToEntity[collider] = entity;
        }
    }

    public void Unregister(Collider collider)
    {
        if (collider != null)
        {
            colliderToEntity.Remove(collider);
        }
    }

    public bool Resolve(Collider collider, out EntityRef entity)
    {
        return colliderToEntity.TryGetValue(collider, out entity);
    }

    public void Clear()
    {
        colliderToEntity.Clear();
    }
}

public sealed class PoolService
{
    private readonly Dictionary<PoolId, GenericPooler> pools = new Dictionary<PoolId, GenericPooler>();

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

    public IPoolable Get(PoolId poolId)
    {
        if (!pools.TryGetValue(poolId, out GenericPooler pool))
        {
            Debug.LogWarning($"PoolService.Get: pool {poolId} is not registered.");
            return null;
        }

        return pool.getObj();
    }

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

    public void ReturnAll()
    {
        foreach (GenericPooler pool in pools.Values)
        {
            pool.ReturnAll();
        }
    }
}

public sealed class PoolableGameObject : IPoolable
{
    private readonly GameObject prefab;
    private readonly GameObject instance;

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

    public GameObject getPrefab()
    {
        return prefab;
    }

    public void Activate()
    {
        if (instance != null)
        {
            instance.SetActive(true);
        }
    }

    public void Deactivate()
    {
        if (instance != null)
        {
            instance.SetActive(false);
        }
    }

    public IPoolable getNewControllerInstance(GameObject newObject)
    {
        return new PoolableGameObject(prefab, newObject);
    }
}
