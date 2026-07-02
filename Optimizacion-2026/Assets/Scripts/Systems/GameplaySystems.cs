using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sistema del jugador. Lo actualiza CustomUpdateManager y coordina movimiento, disparo y aplicación de buffs.
/// </summary>
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

    /// <summary>
    /// Recibe configuración, input, spawner, eventos, estado global y transform visual del jugador.
    /// </summary>
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

    /// <summary>
    /// Restaura posición, daño, cantidad de proyectiles y cooldown del jugador.
    /// </summary>
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

    /// <summary>
    /// Cancela la suscripción a eventos de buff cuando se destruye el sistema.
    /// </summary>
    public void Dispose()
    {
        eventBus.Deregister(buffDestroyedBinding);
    }

    /// <summary>
    /// Procesa pausa, movimiento lateral, cooldown de disparo y disparo del jugador.
    /// </summary>
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

    /// <summary>
    /// Aplica una bonificación de daño o cantidad de proyectiles al estado del jugador.
    /// </summary>
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

    /// <summary>
    /// Solicita al SpawnSystem los proyectiles correspondientes al disparo actual.
    /// </summary>
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

    /// <summary>
    /// Mueve al jugador lateralmente y sincroniza su Transform de escena.
    /// </summary>
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

    /// <summary>
    /// Limita la posición del jugador dentro del rango lateral configurado.
    /// </summary>
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

    /// <summary>
    /// Convierte una distancia lateral al eje correcto según la orientación del carril.
    /// </summary>
    private Vector3 GetHorizontalOffset(float amount)
    {
        return laneConfig.movementAxis == LaneAxis.Z ? new Vector3(amount, 0f, 0f) : new Vector3(0f, 0f, amount);
    }

    /// <summary>
    /// Devuelve la dirección de avance de los proyectiles según la orientación del carril.
    /// </summary>
    private Vector3 GetForwardDirection()
    {
        if (laneConfig.movementAxis == LaneAxis.Z)
        {
            return new Vector3(0f, 0f, laneConfig.projectileMoveDirection).normalized;
        }

        return new Vector3(laneConfig.projectileMoveDirection, 0f, 0f).normalized;
    }

    /// <summary>
    /// Recibe el evento de pared destruida y aplica su buff al jugador.
    /// </summary>
    private void OnBuffWallDestroyed(BuffWallDestroyedEvent eventData)
    {
        ApplyBuff(eventData.Buff);
    }
}

/// <summary>
/// Sistema de enemigos activos. Los mueve en FixedUpdate y los devuelve al pool al morir o reiniciar.
/// </summary>
public sealed class EnemySystem : IFixedUpdateable
{
    private readonly List<EnemyEntity> activeEnemies = new List<EnemyEntity>();
    private readonly PoolService poolService;
    private readonly PhysicsRegistry physicsRegistry;
    private readonly LaneConfig laneConfig;
    private readonly GameStateSystem gameState;

    /// <summary>
    /// Recibe servicios de pool/física, configuración de carril y estado global de partida.
    /// </summary>
    public EnemySystem(PoolService poolService, PhysicsRegistry physicsRegistry, GameConfig config, GameStateSystem gameState)
    {
        this.poolService = poolService;
        this.physicsRegistry = physicsRegistry;
        laneConfig = config != null ? config.lanes : LaneConfig.Default;
        this.gameState = gameState;
    }

    public IReadOnlyList<EnemyEntity> ActiveEnemies => activeEnemies;
    public int ActiveCount => activeEnemies.Count;

    /// <summary>
    /// Agrega un enemigo recién spawneado a la lista activa.
    /// </summary>
    public void Register(EnemyEntity enemy)
    {
        if (enemy != null && !activeEnemies.Contains(enemy))
        {
            activeEnemies.Add(enemy);
        }
    }

    /// <summary>
    /// Actualiza la simulación fija de las entidades activas de este sistema.
    /// </summary>
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

    /// <summary>
    /// Quita un enemigo activo, desregistra su collider y lo devuelve al pool.
    /// </summary>
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

    /// <summary>
    /// Recicla todos los elementos activos del sistema al reiniciar gameplay.
    /// </summary>
    public void Clear()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            Recycle(activeEnemies[i]);
        }
    }

    /// <summary>
    /// Convierte el signo de avance al vector correcto del carril.
    /// </summary>
    private Vector3 GetMoveDirection(float sign)
    {
        return laneConfig.movementAxis == LaneAxis.Z ? new Vector3(0f, 0f, sign) : new Vector3(sign, 0f, 0f);
    }
}

/// <summary>
/// Sistema de efectos visuales activos. Controla cuándo devolver partículas terminadas al pool.
/// </summary>
public sealed class VfxSystem : IUpdateable
{
    private readonly List<Vfx> activeVfx = new List<Vfx>();
    private readonly PoolService poolService;
    private readonly GameStateSystem gameState;

    /// <summary>
    /// Recibe el pool y el estado global para actualizar efectos solo durante gameplay.
    /// </summary>
    public VfxSystem(PoolService poolService, GameStateSystem gameState)
    {
        this.poolService = poolService;
        this.gameState = gameState;
    }

    public IReadOnlyList<Vfx> ActiveEnemies => activeVfx;
    public int ActiveCount => activeVfx.Count;

    /// <summary>
    /// Registra un efecto visual activado por SpawnSystem.
    /// </summary>
    public void Register(Vfx vfx)
    {
        if (vfx != null && !activeVfx.Contains(vfx))
        {
            activeVfx.Add(vfx);
        }
    }

    /// <summary>
    /// Revisa efectos activos y recicla los que ya terminaron.
    /// </summary>
    public void Update(float deltaTime)
    {
        if (!gameState.IsGameplayRunning)
        {
            return;
        }

        for (int i = 0; i < activeVfx.Count; i++)
        {
            if (activeVfx[i].CheckIsStopped())
            {
                Recycle(activeVfx[i]);
            }
        }
    }

    /// <summary>
    /// Devuelve un efecto visual terminado a su pool.
    /// </summary>
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

    /// <summary>
    /// Recicla todos los elementos activos del sistema al reiniciar gameplay.
    /// </summary>
    public void Clear()
    {
        for (int i = activeVfx.Count - 1; i >= 0; i--)
        {
            Recycle(activeVfx[i]);
        }
    }
}

/// <summary>
/// Sistema de paredes de bonificación. Las mueve, registra y recicla durante la wave.
/// </summary>
public sealed class BuffWallSystem : IFixedUpdateable
{
    private readonly List<BuffWallEntity> activeWalls = new List<BuffWallEntity>();
    private readonly PoolService poolService;
    private readonly PhysicsRegistry physicsRegistry;
    private readonly LaneConfig laneConfig;
    private readonly GameStateSystem gameState;

    /// <summary>
    /// Recibe servicios de pool/física, configuración de carril y estado global de partida.
    /// </summary>
    public BuffWallSystem(PoolService poolService, PhysicsRegistry physicsRegistry, GameConfig config, GameStateSystem gameState)
    {
        this.poolService = poolService;
        this.physicsRegistry = physicsRegistry;
        laneConfig = config != null ? config.lanes : LaneConfig.Default;
        this.gameState = gameState;
    }

    public IReadOnlyList<BuffWallEntity> ActiveWalls => activeWalls;
    public int ActiveCount => activeWalls.Count;

    /// <summary>
    /// Agrega una pared recién spawneada a la lista activa.
    /// </summary>
    public void Register(BuffWallEntity wall)
    {
        if (wall != null && !activeWalls.Contains(wall))
        {
            activeWalls.Add(wall);
        }
    }

    /// <summary>
    /// Actualiza la simulación fija de las entidades activas de este sistema.
    /// </summary>
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

    /// <summary>
    /// Quita una pared activa, desregistra su collider y la devuelve al pool.
    /// </summary>
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

    /// <summary>
    /// Recicla todos los elementos activos del sistema al reiniciar gameplay.
    /// </summary>
    public void Clear()
    {
        for (int i = activeWalls.Count - 1; i >= 0; i--)
        {
            Recycle(activeWalls[i]);
        }
    }

    /// <summary>
    /// Convierte el signo de avance al vector correcto del carril.
    /// </summary>
    private Vector3 GetMoveDirection(float sign)
    {
        return laneConfig.movementAxis == LaneAxis.Z ? new Vector3(0f, 0f, sign) : new Vector3(sign, 0f, 0f);
    }
}

/// <summary>
/// Sistema de proyectiles activos. Los mueve, reduce su lifetime y los recicla al expirar.
/// </summary>
public sealed class ProjectileSystem : IFixedUpdateable
{
    private readonly List<ProjectileEntity> activeProjectiles = new List<ProjectileEntity>();
    private readonly PoolService poolService;
    private readonly PhysicsRegistry physicsRegistry;
    private readonly ProjectileConfig config;
    private readonly GameStateSystem gameState;

    /// <summary>
    /// Recibe pool, registro de física, configuración de proyectiles y estado global.
    /// </summary>
    public ProjectileSystem(PoolService poolService, PhysicsRegistry physicsRegistry, GameConfig gameConfig, GameStateSystem gameState)
    {
        this.poolService = poolService;
        this.physicsRegistry = physicsRegistry;
        config = gameConfig != null ? gameConfig.projectile : ProjectileConfig.Default;
        this.gameState = gameState;
    }

    public IReadOnlyList<ProjectileEntity> ActiveProjectiles => activeProjectiles;
    public float HitRadius => config.hitRadius;

    /// <summary>
    /// Agrega un proyectil recién spawneado a la lista activa.
    /// </summary>
    public void Register(ProjectileEntity projectile)
    {
        if (projectile != null && !activeProjectiles.Contains(projectile))
        {
            activeProjectiles.Add(projectile);
        }
    }

    /// <summary>
    /// Actualiza la simulación fija de las entidades activas de este sistema.
    /// </summary>
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

    /// <summary>
    /// Quita un proyectil activo, desregistra su collider y lo devuelve al pool.
    /// </summary>
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

    /// <summary>
    /// Recicla todos los elementos activos del sistema al reiniciar gameplay.
    /// </summary>
    public void Clear()
    {
        for (int i = activeProjectiles.Count - 1; i >= 0; i--)
        {
            Recycle(activeProjectiles[i]);
        }
    }
}
