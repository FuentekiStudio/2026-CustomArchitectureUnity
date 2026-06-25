using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class SpawnSystem
{
    private readonly PoolService poolService;
    private readonly PhysicsRegistry physicsRegistry;
    private readonly GameConfig config;
    private readonly Transform enemySpawnPoint;
    private readonly Transform buffWallSpawnPoint;
    private readonly EnemySystem enemySystem;
    private readonly BuffWallSystem buffWallSystem;
    private readonly ProjectileSystem projectileSystem;
    private readonly IGameEventBus eventBus;
    private int nextId = 1;

    public SpawnSystem(PoolService poolService, PhysicsRegistry physicsRegistry, GameConfig config, Transform enemySpawnPoint, Transform buffWallSpawnPoint, EnemySystem enemySystem, BuffWallSystem buffWallSystem, ProjectileSystem projectileSystem, IGameEventBus eventBus)
    {
        this.poolService = poolService;
        this.physicsRegistry = physicsRegistry;
        this.config = config;
        this.enemySpawnPoint = enemySpawnPoint;
        this.buffWallSpawnPoint = buffWallSpawnPoint;
        this.enemySystem = enemySystem;
        this.buffWallSystem = buffWallSystem;
        this.projectileSystem = projectileSystem;
        this.eventBus = eventBus;
    }

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

    public void SpawnImpactVfx(Vector3 position)
    {
        IPoolable poolable = poolService.Get(PoolId.ImpactVfx);
        if (poolable is PoolableGameObject pooled && pooled.Instance != null)
        {
            pooled.Instance.transform.position = position;
            poolable.Activate();
        }
    }


    private Vector3 GetEnemySpawnPosition()
    {
        Vector3 position = GetSpawnPosition(enemySpawnPoint);
        float randomXRange = config != null ? Mathf.Max(0f, config.spawn.randomXRange) : 5f;
        position.x += Random.Range(-randomXRange, randomXRange);
        return position;
    }

    private static Vector3 GetSpawnPosition(Transform spawnPoint)
    {
        return spawnPoint != null ? spawnPoint.position : Vector3.zero;
    }

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

public sealed class CombatSystem
{
    private readonly EnemySystem enemySystem;
    private readonly BuffWallSystem buffWallSystem;
    private readonly IGameEventBus eventBus;

    public CombatSystem(EnemySystem enemySystem, BuffWallSystem buffWallSystem, IGameEventBus eventBus)
    {
        this.enemySystem = enemySystem;
        this.buffWallSystem = buffWallSystem;
        this.eventBus = eventBus;
    }

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

    public void KillEnemy(EnemyEntity enemy)
    {
        enemySystem.Recycle(enemy);
        eventBus.Raise(new EnemyDefeatedEvent(enemy.id, enemySystem.ActiveCount));
    }

    public void DestroyBuffWall(BuffWallEntity wall)
    {
        BuffData buff = wall.buff;
        buffWallSystem.Recycle(wall);
        eventBus.Raise(new BuffWallDestroyedEvent(buff));
    }
}

public sealed class CollisionSystem : IFixedUpdateable
{
    private readonly ProjectileSystem projectileSystem;
    private readonly EnemySystem enemySystem;
    private readonly BuffWallSystem buffWallSystem;
    private readonly CombatSystem combatSystem;
    private readonly GameStateSystem gameState;
    private readonly PhysicsRegistry physicsRegistry;
    private readonly Transform playerLine;
    private readonly LaneConfig laneConfig;
    private readonly Collider[] hitBuffer = new Collider[16];

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

    private void CheckProjectileHits()
    {
        IReadOnlyList<ProjectileEntity> projectiles = projectileSystem.ActiveProjectiles;
        for (int i = projectiles.Count - 1; i >= 0; i--)
        {
            ProjectileEntity projectile = projectiles[i];
            int count = Physics.OverlapSphereNonAlloc(projectile.position, projectileSystem.HitRadius, hitBuffer, Physics.AllLayers, QueryTriggerInteraction.Collide);

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

    private float GetLineValue()
    {
        Vector3 position = playerLine != null ? playerLine.position : Vector3.zero;
        return laneConfig.movementAxis == LaneAxis.Z ? position.z : position.x;
    }

    private bool HasReachedLine(Vector3 position, float line, float direction)
    {
        float value = laneConfig.movementAxis == LaneAxis.Z ? position.z : position.x;
        return direction < 0f ? value <= line : value >= line;
    }
}

public sealed class RenderingSyncSystem : ILateUpdateable
{
    private readonly EnemySystem enemySystem;
    private readonly BuffWallSystem buffWallSystem;
    private readonly ProjectileSystem projectileSystem;

    public RenderingSyncSystem(EnemySystem enemySystem, BuffWallSystem buffWallSystem, ProjectileSystem projectileSystem)
    {
        this.enemySystem = enemySystem;
        this.buffWallSystem = buffWallSystem;
        this.projectileSystem = projectileSystem;
    }

    public void LateUpdate(float deltaTime)
    {
        SyncEnemyTransforms();
        SyncBuffWallTransforms();
        SyncProjectileTransforms();
    }

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
