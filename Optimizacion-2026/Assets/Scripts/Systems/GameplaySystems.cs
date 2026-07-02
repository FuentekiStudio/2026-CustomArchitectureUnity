using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerSystem : IUpdateable
{
    private readonly PlayerState player = new PlayerState();
    private readonly PlayerConfig config;
    private readonly ProjectileConfig projectileConfig;
    private readonly LaneConfig laneConfig;
    private readonly InputSystemService input;
    private readonly SpawnSystem spawnSystem;
    private readonly IGameEventBus eventBus;
    private readonly GameStateSystem gameState;
    private readonly Transform playerTransform;
    private readonly EventBinding<BuffWallDestroyedEvent> buffDestroyedBinding;

    public PlayerSystem(GameConfig gameConfig, InputSystemService input, SpawnSystem spawnSystem, IGameEventBus eventBus, GameStateSystem gameState, Transform playerTransform)
    {
        config = gameConfig != null ? gameConfig.player : PlayerConfig.Default;
        projectileConfig = gameConfig != null ? gameConfig.projectile : ProjectileConfig.Default;
        laneConfig = gameConfig != null ? gameConfig.lanes : LaneConfig.Default;
        this.input = input;
        this.spawnSystem = spawnSystem;
        this.eventBus = eventBus;
        this.gameState = gameState;
        this.playerTransform = playerTransform;
        buffDestroyedBinding = new EventBinding<BuffWallDestroyedEvent>(OnBuffWallDestroyed);
        eventBus.Register(buffDestroyedBinding);
        Reset();
    }

    public PlayerState State => player;

    public void Reset()
    {
        player.position = playerTransform != null ? playerTransform.position : Vector3.zero;
        player.damage = config.baseDamage;
        player.damageBonus = 0;
        player.projectileCount = Mathf.Max(1, config.baseProjectileCount);
        player.fireRate = Mathf.Max(0.01f, config.fireRate);
        player.shootCooldown = 0f;
        eventBus.Raise(new BuffAppliedEvent(player.damageBonus, player.projectileCount));
    }

    public void Dispose()
    {
        eventBus.Deregister(buffDestroyedBinding);
    }

    public void Update(float deltaTime)
    {
        if (input.PausePressed())
        {
            if (gameState.State == GameState.Paused)
            {
                gameState.Resume();
            }
            else
            {
                gameState.Pause();
            }
        }

        if (!gameState.IsGameplayRunning)
        {
            return;
        }

        Move(deltaTime);
        player.shootCooldown -= deltaTime;

        if (input.IsShooting())
        {
            Shoot();
        }
    }

    public void ApplyBuff(BuffData buff)
    {
        if (buff.type == BuffType.Damage)
        {
            player.damageBonus += buff.value;
            player.damage = config.baseDamage + player.damageBonus;
        }
        else if (buff.type == BuffType.ProjectileCount)
        {
            player.projectileCount = Mathf.Max(1, player.projectileCount + buff.value);
        }

        eventBus.Raise(new BuffAppliedEvent(player.damageBonus, player.projectileCount));
    }

    public void Shoot()
    {
        if (player.shootCooldown > 0f)
        {
            return;
        }

        player.shootCooldown = player.fireRate;
        int projectileCount = Mathf.Max(1, player.projectileCount);
        float center = (projectileCount - 1) * 0.5f;

        for (int i = 0; i < projectileCount; i++)
        {
            Vector3 spawnPosition = player.position + GetHorizontalOffset((i - center) * laneConfig.projectileSpread);
            ProjectileSpawnData spawnData = new ProjectileSpawnData
            {
                position = spawnPosition,
                direction = GetForwardDirection(),
                damage = player.damage,
                owner = player
            };
            spawnSystem.SpawnProjectile(spawnData);
        }
    }

    private void Move(float deltaTime)
    {
        float horizontal = input.GetHorizontal();
        player.position += GetHorizontalOffset(horizontal * config.moveSpeed * deltaTime);
        player.position = ClampHorizontal(player.position);

        if (playerTransform != null)
        {
            playerTransform.position = player.position;
        }
    }

    private Vector3 ClampHorizontal(Vector3 position)
    {
        if (laneConfig.movementAxis == LaneAxis.Z)
        {
            position.x = Mathf.Clamp(position.x, config.minHorizontal, config.maxHorizontal);
        }
        else
        {
            position.z = Mathf.Clamp(position.z, config.minHorizontal, config.maxHorizontal);
        }

        return position;
    }

    private Vector3 GetHorizontalOffset(float amount)
    {
        return laneConfig.movementAxis == LaneAxis.Z ? new Vector3(amount, 0f, 0f) : new Vector3(0f, 0f, amount);
    }

    private Vector3 GetForwardDirection()
    {
        if (laneConfig.movementAxis == LaneAxis.Z)
        {
            return new Vector3(0f, 0f, laneConfig.projectileMoveDirection).normalized;
        }

        return new Vector3(laneConfig.projectileMoveDirection, 0f, 0f).normalized;
    }

    private void OnBuffWallDestroyed(BuffWallDestroyedEvent eventData)
    {
        ApplyBuff(eventData.Buff);
    }
}

public sealed class EnemySystem : IFixedUpdateable
{
    private readonly List<EnemyEntity> activeEnemies = new List<EnemyEntity>();
    private readonly PoolService poolService;
    private readonly PhysicsRegistry physicsRegistry;
    private readonly LaneConfig laneConfig;
    private readonly GameStateSystem gameState;

    public EnemySystem(PoolService poolService, PhysicsRegistry physicsRegistry, GameConfig config, GameStateSystem gameState)
    {
        this.poolService = poolService;
        this.physicsRegistry = physicsRegistry;
        laneConfig = config != null ? config.lanes : LaneConfig.Default;
        this.gameState = gameState;
    }

    public IReadOnlyList<EnemyEntity> ActiveEnemies => activeEnemies;
    public int ActiveCount => activeEnemies.Count;

    public void Register(EnemyEntity enemy)
    {
        if (enemy != null && !activeEnemies.Contains(enemy))
        {
            activeEnemies.Add(enemy);
        }
    }

    public void FixedUpdate(float deltaTime)
    {
        if (!gameState.IsGameplayRunning)
        {
            return;
        }

        for (int i = 0; i < activeEnemies.Count; i++)
        {
            EnemyEntity enemy = activeEnemies[i];
            enemy.position += GetMoveDirection(laneConfig.enemyMoveDirection) * enemy.speed * deltaTime;
        }
    }

    public void Recycle(EnemyEntity enemy)
    {
        if (enemy == null)
        {
            return;
        }

        activeEnemies.Remove(enemy);
        enemy.isActive = false;
        physicsRegistry.Unregister(enemy.collider);
        poolService.Return(enemy.poolId, enemy.poolable);
    }

    public void Clear()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            Recycle(activeEnemies[i]);
        }
    }

    private Vector3 GetMoveDirection(float sign)
    {
        return laneConfig.movementAxis == LaneAxis.Z ? new Vector3(0f, 0f, sign) : new Vector3(sign, 0f, 0f);
    }
}

public sealed class VfxSystem : IUpdateable
{
    private readonly List<Vfx> activeVfx = new List<Vfx>();
    private readonly PoolService poolService;
    private readonly GameStateSystem gameState;

    public VfxSystem(PoolService poolService, GameStateSystem gameState)
    {
        this.poolService = poolService;
        this.gameState = gameState;
    }

    public IReadOnlyList<Vfx> ActiveEnemies => activeVfx;
    public int ActiveCount => activeVfx.Count;

    public void Register(Vfx vfx)
    {
        if (vfx != null && !activeVfx.Contains(vfx))
        {
            activeVfx.Add(vfx);
        }
    }

    public void Update(float deltaTime)
    {
        if (!gameState.IsGameplayRunning)
        {
            return;
        }

        for (int i = 0; i < activeVfx.Count; i++)
        {
            Debug.Log(activeVfx[i].CheckIsStopped());
            if (activeVfx[i].CheckIsStopped())
            {
                Recycle(activeVfx[i]);
            }
        }
    }

    public void Recycle(Vfx vfx)
    {
        if (vfx == null)
        {
            return;
        }

        activeVfx.Remove(vfx);
        vfx.isActive = false;
        poolService.Return(vfx.id, vfx.poolable);
    }

    public void Clear()
    {
        for (int i = activeVfx.Count - 1; i >= 0; i--)
        {
            Recycle(activeVfx[i]);
        }
    }
}

public sealed class BuffWallSystem : IFixedUpdateable
{
    private readonly List<BuffWallEntity> activeWalls = new List<BuffWallEntity>();
    private readonly PoolService poolService;
    private readonly PhysicsRegistry physicsRegistry;
    private readonly LaneConfig laneConfig;
    private readonly GameStateSystem gameState;

    public BuffWallSystem(PoolService poolService, PhysicsRegistry physicsRegistry, GameConfig config, GameStateSystem gameState)
    {
        this.poolService = poolService;
        this.physicsRegistry = physicsRegistry;
        laneConfig = config != null ? config.lanes : LaneConfig.Default;
        this.gameState = gameState;
    }

    public IReadOnlyList<BuffWallEntity> ActiveWalls => activeWalls;

    public void Register(BuffWallEntity wall)
    {
        if (wall != null && !activeWalls.Contains(wall))
        {
            activeWalls.Add(wall);
        }
    }

    public void FixedUpdate(float deltaTime)
    {
        if (!gameState.IsGameplayRunning)
        {
            return;
        }

        for (int i = 0; i < activeWalls.Count; i++)
        {
            BuffWallEntity wall = activeWalls[i];
            wall.position += GetMoveDirection(laneConfig.buffWallMoveDirection) * wall.speed * deltaTime;
        }
    }

    public void Recycle(BuffWallEntity wall)
    {
        if (wall == null)
        {
            return;
        }

        activeWalls.Remove(wall);
        wall.isActive = false;
        physicsRegistry.Unregister(wall.collider);
        poolService.Return(wall.poolId, wall.poolable);
    }

    public void Clear()
    {
        for (int i = activeWalls.Count - 1; i >= 0; i--)
        {
            Recycle(activeWalls[i]);
        }
    }

    private Vector3 GetMoveDirection(float sign)
    {
        return laneConfig.movementAxis == LaneAxis.Z ? new Vector3(0f, 0f, sign) : new Vector3(sign, 0f, 0f);
    }
}

public sealed class ProjectileSystem : IFixedUpdateable
{
    private readonly List<ProjectileEntity> activeProjectiles = new List<ProjectileEntity>();
    private readonly PoolService poolService;
    private readonly PhysicsRegistry physicsRegistry;
    private readonly ProjectileConfig config;
    private readonly GameStateSystem gameState;

    public ProjectileSystem(PoolService poolService, PhysicsRegistry physicsRegistry, GameConfig gameConfig, GameStateSystem gameState)
    {
        this.poolService = poolService;
        this.physicsRegistry = physicsRegistry;
        config = gameConfig != null ? gameConfig.projectile : ProjectileConfig.Default;
        this.gameState = gameState;
    }

    public IReadOnlyList<ProjectileEntity> ActiveProjectiles => activeProjectiles;
    public float HitRadius => config.hitRadius;

    public void Register(ProjectileEntity projectile)
    {
        if (projectile != null && !activeProjectiles.Contains(projectile))
        {
            activeProjectiles.Add(projectile);
        }
    }

    public void FixedUpdate(float deltaTime)
    {
        if (!gameState.IsGameplayRunning)
        {
            return;
        }

        for (int i = activeProjectiles.Count - 1; i >= 0; i--)
        {
            ProjectileEntity projectile = activeProjectiles[i];
            projectile.position += projectile.direction * config.speed * deltaTime;
            projectile.lifetime -= deltaTime;

            if (projectile.lifetime <= 0f)
            {
                Recycle(projectile);
            }
        }
    }

    public void Recycle(ProjectileEntity projectile)
    {
        if (projectile == null)
        {
            return;
        }

        activeProjectiles.Remove(projectile);
        projectile.isActive = false;
        physicsRegistry.Unregister(projectile.collider);
        poolService.Return(projectile.poolId, projectile.poolable);
    }

    public void Clear()
    {
        for (int i = activeProjectiles.Count - 1; i >= 0; i--)
        {
            Recycle(activeProjectiles[i]);
        }
    }
}
