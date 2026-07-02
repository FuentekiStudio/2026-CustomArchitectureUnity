using System;
using UnityEngine;

/// <summary>
/// Tipos de enemigos que el WaveSystem y SpawnSystem usan para seleccionar datos y pools.
/// </summary>
public enum EnemyType
{
    Normal,
    Elite,
    Boss,
    Megazord
}

/// <summary>
/// Tipos de bonificación que puede entregar una pared al ser destruida.
/// </summary>
public enum BuffType
{
    Damage,
    ProjectileCount
}

/// <summary>
/// Resultado final informado por GameStateSystem a la UI mediante eventos.
/// </summary>
public enum GameResult
{
    Victory,
    Defeat
}

/// <summary>
/// Estados globales de la partida controlados por GameStateSystem.
/// </summary>
public enum GameState
{
    MainMenu,
    Playing,
    Paused,
    Victory,
    Defeat
}

/// <summary>
/// Identificadores de pools usados por PoolService y SpawnSystem.
/// </summary>
public enum PoolId
{
    EnemyNormal,
    EnemyElite,
    EnemyBoss,
    MegazordBoss,
    BuffWallDamage,
    BuffWallProjectileCount,
    Projectile,
    ImpactVfx
}

/// <summary>
/// Eje principal de avance del juego; permite orientar la lógica sobre X o Z.
/// </summary>
public enum LaneAxis
{
    X,
    Z
}

/// <summary>
/// Configuración editable del juego. GameBootstrap la inyecta en servicios y sistemas durante la inicialización.
/// </summary>
[CreateAssetMenu(menuName = "Optimization Final/Game Config", fileName = "GameConfig")]
public sealed class GameConfig : ScriptableObject
{
    public PlayerConfig player = PlayerConfig.Default;
    public ProjectileConfig projectile = ProjectileConfig.Default;
    public SpawnConfig spawn = SpawnConfig.Default;
    public LaneConfig lanes = LaneConfig.Default;
    public PoolConfig pools = new PoolConfig();
    public WaveConfig[] waves = Array.Empty<WaveConfig>();
}

[Serializable]
/// <summary>
/// Datos base del jugador: movimiento, daño inicial, disparo y límites laterales.
/// </summary>
public struct PlayerConfig
{
    public float moveSpeed;
    public float minHorizontal;
    public float maxHorizontal;
    public int baseDamage;
    public int baseProjectileCount;
    public float fireRate;

    /// <summary>
    /// Valores de respaldo usados si no hay GameConfig asignado.
    /// </summary>
    public static PlayerConfig Default => new PlayerConfig
    {
        moveSpeed = 8f,
        minHorizontal = -6f,
        maxHorizontal = 6f,
        baseDamage = 1,
        baseProjectileCount = 1,
        fireRate = 0.2f
    };
}

[Serializable]
/// <summary>
/// Configuración general de spawn, incluyendo aleatoriedad horizontal para enemigos.
/// </summary>
public struct SpawnConfig
{
    public float randomXRange;

    public static SpawnConfig Default => new SpawnConfig
    {
        randomXRange = 5f
    };
}

[Serializable]
/// <summary>
/// Parámetros de proyectiles usados por PlayerSystem, ProjectileSystem y CollisionSystem.
/// </summary>
public struct ProjectileConfig
{
    public float speed;
    public float lifetime;
    public float hitRadius;

    public static ProjectileConfig Default => new ProjectileConfig
    {
        speed = 16f,
        lifetime = 3f,
        hitRadius = 0.35f
    };
}

[Serializable]
/// <summary>
/// Define direcciones de movimiento y separación de proyectiles según la orientación del carril.
/// </summary>
public struct LaneConfig
{
    public LaneAxis movementAxis;
    public float enemyMoveDirection;
    public float buffWallMoveDirection;
    public float projectileMoveDirection;
    public float projectileSpread;

    public static LaneConfig Default => new LaneConfig
    {
        movementAxis = LaneAxis.Z,
        enemyMoveDirection = -1f,
        buffWallMoveDirection = -1f,
        projectileMoveDirection = 1f,
        projectileSpread = 0.35f
    };
}

[Serializable]
/// <summary>
/// Lista de prefabs y cantidades iniciales que PoolService debe preinstanciar.
/// </summary>
public sealed class PoolConfig
{
    public PoolEntry[] entries = Array.Empty<PoolEntry>();
}

[Serializable]
/// <summary>
/// Entrada individual de pool: identificador, prefab y cantidad a precargar.
/// </summary>
public struct PoolEntry
{
    public PoolId id;
    public GameObject prefab;
    public int prewarmCount;
}

[Serializable]
/// <summary>
/// Configuración de una wave: enemigos, paredes de buff, jefe especial y demora inicial.
/// </summary>
public sealed class WaveConfig
{
    public EnemySpawnData[] enemies = Array.Empty<EnemySpawnData>();
    public BuffWallSpawnData[] buffWalls = Array.Empty<BuffWallSpawnData>();
    public BossSpawnData boss;
    public MegazordSpawnData megazord;
    public float delayBeforeWave = 1f;
}

[Serializable]
/// <summary>
/// Datos que WaveSystem entrega a SpawnSystem para crear enemigos.
/// </summary>
public struct EnemySpawnData
{
    public EnemyType type;
    public int count;
    public float interval;
    public int health;
    public float speed;
}

[Serializable]
/// <summary>
/// Datos de jefe tradicional conservados para configuración y compatibilidad.
/// </summary>
public struct BossSpawnData
{
    public bool enabled;
    public int health;
    public float speed;
    public float delay;
}
[Serializable]
/// <summary>
/// Datos del jefe Megazord que se spawnea con demora dentro de una wave.
/// </summary>
public struct MegazordSpawnData
{
    public bool enabled;
    public int health;
    public float speed;
    public float delay;
}

[Serializable]
/// <summary>
/// Datos que WaveSystem entrega a SpawnSystem para crear paredes de bonificación.
/// </summary>
public struct BuffWallSpawnData
{
    public BuffType type;
    public int count;
    public float interval;
    public int health;
    public float speed;
    public int value;
}

[Serializable]
/// <summary>
/// Bonificación concreta aplicada al jugador cuando destruye una pared.
/// </summary>
public struct BuffData
{
    public BuffType type;
    public int value;
}

/// <summary>
/// Datos temporales que PlayerSystem usa para pedirle a SpawnSystem un proyectil.
/// </summary>
public struct ProjectileSpawnData
{
    public Vector3 position;
    public Vector3 direction;
    public int damage;
    public object owner;
}

/// <summary>
/// Estado resumido que UISystem le pasa a GameUIView para renderizar el HUD.
/// </summary>
public struct HudState
{
    public int waveIndex;
    public int totalWaves;
    public int enemiesRemaining;
    public int damageBonus;
    public int projectileCount;
    public GameState gameState;
}
