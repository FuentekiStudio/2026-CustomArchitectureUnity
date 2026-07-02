using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Sistema encargado de activar objetos del pool y crear entidades runtime para enemigos, paredes, proyectiles y VFX.
/// Lo usan WaveSystem, PlayerSystem y CombatSystem.
/// </summary>
public sealed class SpawnSystem
{
    private readonly PoolService poolService;
    private readonly PhysicsRegistry physicsRegistry;
    private readonly GameConfig config;
    private readonly Transform enemySpawnPoint;
    private readonly Transform buffWallSpawnPoint;
    private readonly EnemySystem enemySystem;
    private readonly VfxSystem vfxSystem;
    private readonly BuffWallSystem buffWallSystem;
    private readonly ProjectileSystem projectileSystem;
    private readonly IGameEventBus eventBus;
    private int nextId = 1;

    /// <summary>
    /// Recibe pools, registro de física, configuración, puntos de spawn y sistemas donde registrar entidades.
    /// </summary>
    public SpawnSystem(PoolService poolService, PhysicsRegistry physicsRegistry, GameConfig config, Transform enemySpawnPoint, Transform buffWallSpawnPoint, EnemySystem enemySystem, VfxSystem vfxSystem, BuffWallSystem buffWallSystem, ProjectileSystem projectileSystem, IGameEventBus eventBus)
    {
        this.poolService = poolService;
        this.physicsRegistry = physicsRegistry;
        this.config = config;
        this.enemySpawnPoint = enemySpawnPoint;
        this.buffWallSpawnPoint = buffWallSpawnPoint;
        this.enemySystem = enemySystem;
        this.vfxSystem = vfxSystem;
        this.buffWallSystem = buffWallSystem;
        this.projectileSystem = projectileSystem;
        this.eventBus = eventBus;
    }

    /// <summary>
    /// Activa un enemigo desde su pool, crea su entidad y la registra en física y EnemySystem.
    /// </summary>
    public EnemyEntity SpawnEnemy(EnemySpawnData data)
    {
        PoolId poolId = GetEnemyPool(data.type);
        IPoolable poolable = poolService.Get(poolId);
        if (!TryPreparePoolable(poolable, enemySpawnPoint, out GameObject view, out Collider collider))
        {
            return null;
        }

        EnemyEntity enemy = new EnemyEntity
        {
            id = nextId++,
            type = data.type,
            health = Mathf.Max(1, data.health),
            speed = Mathf.Max(0f, data.speed),
            position = GetEnemySpawnPosition(),
            view = view,
            collider = collider,
            poolable = poolable,
            poolId = poolId,
            isActive = true
        };

        view.transform.position = enemy.position;
        physicsRegistry.Register(collider, new EntityRef(EntityKind.Enemy, enemy));
        enemySystem.Register(enemy);
        eventBus.Raise(new EnemySpawnedEvent(enemy.id, enemy.type));
        return enemy;
    }

    /// <summary>
    /// Activa una pared de buff desde su pool, crea su entidad y actualiza su texto visual.
    /// </summary>
    public BuffWallEntity SpawnBuffWall(BuffWallSpawnData data)
    {
        PoolId poolId = data.type == BuffType.Damage ? PoolId.BuffWallDamage : PoolId.BuffWallProjectileCount;
        IPoolable poolable = poolService.Get(poolId);
        if (!TryPreparePoolable(poolable, buffWallSpawnPoint, out GameObject view, out Collider collider))
        {
            return null;
        }

        BuffWallEntity wall = new BuffWallEntity
        {
            id = nextId++,
            health = Mathf.Max(1, data.health),
            speed = Mathf.Max(0f, data.speed),
            buff = new BuffData { type = data.type, value = data.value },
            position = GetSpawnPosition(buffWallSpawnPoint),
            view = view,
            collider = collider,
            poolable = poolable,
            poolId = poolId,
            isActive = true
        };

        view.transform.position = wall.position;
        UpdateBuffWallView(view, wall.buff);
        physicsRegistry.Register(collider, new EntityRef(EntityKind.BuffWall, wall));
        buffWallSystem.Register(wall);
        return wall;
    }

    /// <summary>
    /// Activa un proyectil desde el pool y lo registra para movimiento y colisiones.
    /// </summary>
    public ProjectileEntity SpawnProjectile(ProjectileSpawnData data)
    {
        IPoolable poolable = poolService.Get(PoolId.Projectile);
        if (!TryPreparePoolable(poolable, null, out GameObject view, out Collider collider))
        {
            return null;
        }

        ProjectileEntity projectile = new ProjectileEntity
        {
            id = nextId++,
            damage = Mathf.Max(1, data.damage),
            direction = data.direction == Vector3.zero ? Vector3.forward : data.direction.normalized,
            lifetime = config != null ? config.projectile.lifetime : ProjectileConfig.Default.lifetime,
            owner = data.owner,
            position = data.position,
            view = view,
            collider = collider,
            poolable = poolable,
            poolId = PoolId.Projectile,
            isActive = true
        };

        view.transform.position = projectile.position;
        physicsRegistry.Register(collider, new EntityRef(EntityKind.Projectile, projectile));
        projectileSystem.Register(projectile);
        return projectile;
    }

    /// <summary>
    /// Activa un efecto visual en una posición específica y lo registra en VfxSystem.
    /// </summary>
    public Vfx SpawnVfx(Vector3 position, PoolId poolId)
    {
        Vfx vfx = null;
        IPoolable poolable = poolService.Get(poolId);
        if (poolable is PoolableGameObject pooled && pooled.Instance != null)
        {
            pooled.Instance.transform.position = position;
            vfx = new Vfx(pooled.Instance, poolId, poolable);
            vfxSystem.Register(vfx);
            poolable.Activate();
            vfx.Activate();
        }
        
        return vfx;
    }


    /// <summary>
    /// Calcula la posición de spawn de enemigos con aleatoriedad lateral configurada.
    /// </summary>
    private Vector3 GetEnemySpawnPosition()
    {
        Vector3 position = GetSpawnPosition(enemySpawnPoint);
        float randomXRange = config != null ? Mathf.Max(0f, config.spawn.randomXRange) : 5f;
        position.x += Random.Range(-randomXRange, randomXRange);
        return position;
    }

    /// <summary>
    /// Devuelve la posición de un punto de spawn o Vector3.zero si no existe.
    /// </summary>
    private static Vector3 GetSpawnPosition(Transform spawnPoint)
    {
        return spawnPoint != null ? spawnPoint.position : Vector3.zero;
    }

    /// <summary>
    /// Actualiza el texto visible de una pared según el tipo y valor del buff.
    /// </summary>
    private static void UpdateBuffWallView(GameObject view, BuffData buff)
    {
        if (view == null)
        {
            return;
        }

        TMP_Text text = FindBuffWallText(view);
        if (text != null)
        {
            string prefix = buff.type == BuffType.Damage ? "DMG" : "x";
            text.text = $"{prefix} {buff.value}";
        }
    }


    /// <summary>
    /// Busca el TMP_Text dentro del prefab de pared para mostrar el buff.
    /// </summary>
    private static TMP_Text FindBuffWallText(GameObject view)
    {
        TMP_Text[] texts = view.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].gameObject.name == "Text (TMP)")
            {
                return texts[i];
            }
        }

        return texts.Length > 0 ? texts[0] : null;
    }

    /// <summary>
    /// Prepara un objeto del pool, lo posiciona, obtiene su collider y lo activa.
    /// </summary>
    private static bool TryPreparePoolable(IPoolable poolable, Transform fallbackTransform, out GameObject view, out Collider collider)
    {
        view = null;
        collider = null;

        if (poolable is not PoolableGameObject pooled || pooled.Instance == null)
        {
            return false;
        }

        view = pooled.Instance;
        if (fallbackTransform != null)
        {
            view.transform.position = fallbackTransform.position;
        }

        collider = view.GetComponentInChildren<Collider>(true);
        poolable.Activate();
        return true;
    }

    /// <summary>
    /// Traduce el tipo de enemigo al identificador de pool correspondiente.
    /// </summary>
    private static PoolId GetEnemyPool(EnemyType type)
    {
        return type switch
        {
            EnemyType.Elite => PoolId.EnemyElite,
            EnemyType.Boss => PoolId.EnemyBoss,
            EnemyType.Megazord => PoolId.MegazordBoss,
            _ => PoolId.EnemyNormal
        };
    }
}

/// <summary>
/// Sistema de resolución de daño. Lo usa CollisionSystem cuando detecta impactos válidos.
/// </summary>
public sealed class CombatSystem
{
    private readonly EnemySystem enemySystem;
    private readonly PoolService poolService;
    private readonly SpawnSystem spawnSystem;
    private readonly BuffWallSystem buffWallSystem;
    private readonly IGameEventBus eventBus;

    /// <summary>
    /// Recibe sistemas necesarios para aplicar consecuencias de combate y emitir eventos.
    /// </summary>
    public CombatSystem(EnemySystem enemySystem, BuffWallSystem buffWallSystem, IGameEventBus eventBus, PoolService poolService, SpawnSystem spawnSystem)
    {
        this.enemySystem = enemySystem;
        this.buffWallSystem = buffWallSystem;
        this.eventBus = eventBus;
        this.poolService = poolService;
        this.spawnSystem = spawnSystem;
    }

    /// <summary>
    /// Aplica daño a un objetivo y ejecuta muerte o destrucción si llega a cero vida.
    /// </summary>
    public void ApplyDamage(IDamageable target, int amount)
    {
        if (target == null || target.IsDead)
        {
            return;
        }

        target.TakeDamage(Mathf.Max(1, amount));

        if (!target.IsDead)
        {
            return;
        }

        if (target is EnemyEntity enemy)
        {
            KillEnemy(enemy);
        }
        else if (target is BuffWallEntity wall)
        {
            DestroyBuffWall(wall);
        }
    }

    /// <summary>
    /// Recicla un enemigo derrotado, dispara VFX y notifica el evento de muerte.
    /// </summary>
    public void KillEnemy(EnemyEntity enemy)
    {
        spawnSystem.SpawnVfx(enemy.Position, PoolId.ImpactVfx);
        enemySystem.Recycle(enemy);
        eventBus.Raise(new EnemyDefeatedEvent(enemy.id, enemySystem.ActiveCount));
    }

    /// <summary>
    /// Recicla una pared destruida y publica el buff que debe recibir el jugador.
    /// </summary>
    public void DestroyBuffWall(BuffWallEntity wall)
    {
        BuffData buff = wall.buff;
        buffWallSystem.Recycle(wall);
        eventBus.Raise(new BuffWallDestroyedEvent(buff));
    }
}

/// <summary>
/// Sistema centralizado de colisiones. Consulta física para proyectiles y verifica líneas de derrota/despawn.
/// </summary>
public sealed class CollisionSystem : IFixedUpdateable
{
    private const int EnemyLayer = 6;
    private const int BarrierLayer = 7;
    private const int ProjectileTargetLayerMask = (1 << EnemyLayer) | (1 << BarrierLayer);

    private readonly ProjectileSystem projectileSystem;
    private readonly EnemySystem enemySystem;
    private readonly BuffWallSystem buffWallSystem;
    private readonly CombatSystem combatSystem;
    private readonly GameStateSystem gameState;
    private readonly PhysicsRegistry physicsRegistry;
    private readonly Transform playerLine;
    private readonly LaneConfig laneConfig;
    private readonly Collider[] hitBuffer = new Collider[16];

    /// <summary>
    /// Recibe listas activas, combate, estado global, registro de colliders y línea del jugador.
    /// </summary>
    public CollisionSystem(ProjectileSystem projectileSystem, EnemySystem enemySystem, BuffWallSystem buffWallSystem, CombatSystem combatSystem, GameStateSystem gameState, PhysicsRegistry physicsRegistry, GameConfig config, Transform playerLine)
    {
        this.projectileSystem = projectileSystem;
        this.enemySystem = enemySystem;
        this.buffWallSystem = buffWallSystem;
        this.combatSystem = combatSystem;
        this.gameState = gameState;
        this.physicsRegistry = physicsRegistry;
        this.playerLine = playerLine;
        laneConfig = config != null ? config.lanes : LaneConfig.Default;
    }

    /// <summary>
    /// Ejecuta chequeos de impactos, llegada de enemigos y despawn de paredes en simulación fija.
    /// </summary>
    public void FixedUpdate(float deltaTime)
    {
        if (!gameState.IsGameplayRunning)
        {
            return;
        }

        CheckProjectileHits();
        CheckEnemyPlayerLine();
        CheckBuffWallDespawnLine();
    }

    /// <summary>
    /// Consulta solo capas de enemigos/barreras cerca de cada proyectil y delega daño en CombatSystem.
    /// </summary>
    private void CheckProjectileHits()
    {
        if (enemySystem.ActiveCount == 0 && buffWallSystem.ActiveCount == 0)
        {
            return;
        }

        IReadOnlyList<ProjectileEntity> projectiles = projectileSystem.ActiveProjectiles;
        for (int i = projectiles.Count - 1; i >= 0; i--)
        {
            ProjectileEntity projectile = projectiles[i];
            int count = Physics.OverlapSphereNonAlloc(projectile.position, projectileSystem.HitRadius, hitBuffer, ProjectileTargetLayerMask, QueryTriggerInteraction.Collide);

            for (int hitIndex = 0; hitIndex < count; hitIndex++)
            {
                if (!physicsRegistry.Resolve(hitBuffer[hitIndex], out EntityRef entityRef))
                {
                    continue;
                }

                if (entityRef.Kind == EntityKind.Projectile || entityRef.Entity == projectile)
                {
                    continue;
                }

                if (entityRef.Entity is IDamageable damageable)
                {
                    combatSystem.ApplyDamage(damageable, projectile.damage);
                    projectileSystem.Recycle(projectile);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Verifica si algún enemigo llegó a la línea del jugador para declarar derrota.
    /// </summary>
    private void CheckEnemyPlayerLine()
    {
        float line = GetLineValue();
        IReadOnlyList<EnemyEntity> enemies = enemySystem.ActiveEnemies;
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            if (HasReachedLine(enemies[i].position, line, laneConfig.enemyMoveDirection))
            {
                gameState.Lose();
                return;
            }
        }
    }

    /// <summary>
    /// Recicla paredes de buff que llegaron a la línea de despawn sin ser destruidas.
    /// </summary>
    private void CheckBuffWallDespawnLine()
    {
        float line = GetLineValue();
        IReadOnlyList<BuffWallEntity> walls = buffWallSystem.ActiveWalls;
        for (int i = walls.Count - 1; i >= 0; i--)
        {
            if (HasReachedLine(walls[i].position, line, laneConfig.buffWallMoveDirection))
            {
                buffWallSystem.Recycle(walls[i]);
            }
        }
    }

    /// <summary>
    /// Obtiene el valor de la línea del jugador sobre el eje de movimiento configurado.
    /// </summary>
    private float GetLineValue()
    {
        Vector3 position = playerLine != null ? playerLine.position : Vector3.zero;
        return laneConfig.movementAxis == LaneAxis.Z ? position.z : position.x;
    }

    /// <summary>
    /// Determina si una posición ya cruzó la línea según la dirección de avance.
    /// </summary>
    private bool HasReachedLine(Vector3 position, float line, float direction)
    {
        float value = laneConfig.movementAxis == LaneAxis.Z ? position.z : position.x;
        return direction < 0f ? value <= line : value >= line;
    }
}

/// <summary>
/// Sistema de sincronización visual. Copia posiciones de entidades de datos a sus GameObjects en LateUpdate.
/// </summary>
public sealed class RenderingSyncSystem : ILateUpdateable
{
    private readonly EnemySystem enemySystem;
    private readonly BuffWallSystem buffWallSystem;
    private readonly ProjectileSystem projectileSystem;

    /// <summary>
    /// Recibe sistemas con listas activas para sincronizar sus vistas visuales.
    /// </summary>
    public RenderingSyncSystem(EnemySystem enemySystem, BuffWallSystem buffWallSystem, ProjectileSystem projectileSystem)
    {
        this.enemySystem = enemySystem;
        this.buffWallSystem = buffWallSystem;
        this.projectileSystem = projectileSystem;
    }

    /// <summary>
    /// Actualiza posiciones de GameObjects luego de que termina la simulación del frame.
    /// </summary>
    public void LateUpdate(float deltaTime)
    {
        SyncEnemyTransforms();
        SyncBuffWallTransforms();
        SyncProjectileTransforms();
    }

    /// <summary>
    /// Copia la posición de cada EnemyEntity a su vista de Unity.
    /// </summary>
    private void SyncEnemyTransforms()
    {
        IReadOnlyList<EnemyEntity> enemies = enemySystem.ActiveEnemies;
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i].view != null)
            {
                enemies[i].view.transform.position = enemies[i].position;
            }
        }
    }

    /// <summary>
    /// Copia la posición de cada BuffWallEntity a su vista de Unity.
    /// </summary>
    private void SyncBuffWallTransforms()
    {
        IReadOnlyList<BuffWallEntity> walls = buffWallSystem.ActiveWalls;
        for (int i = 0; i < walls.Count; i++)
        {
            if (walls[i].view != null)
            {
                walls[i].view.transform.position = walls[i].position;
            }
        }
    }

    /// <summary>
    /// Copia la posición de cada ProjectileEntity a su vista de Unity.
    /// </summary>
    private void SyncProjectileTransforms()
    {
        IReadOnlyList<ProjectileEntity> projectiles = projectileSystem.ActiveProjectiles;
        for (int i = 0; i < projectiles.Count; i++)
        {
            if (projectiles[i].view != null)
            {
                projectiles[i].view.transform.position = projectiles[i].position;
            }
        }
    }
}
