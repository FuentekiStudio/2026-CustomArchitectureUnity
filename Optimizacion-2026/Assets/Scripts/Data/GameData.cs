using System;
using UnityEngine;

public enum EnemyType
{
    Normal,
    Elite,
    Boss
}

public enum BuffType
{
    Damage,
    ProjectileCount
}

public enum GameResult
{
    Victory,
    Defeat
}

public enum GameState
{
    MainMenu,
    Playing,
    Paused,
    Victory,
    Defeat
}

public enum PoolId
{
    EnemyNormal,
    EnemyElite,
    EnemyBoss,
    BuffWallDamage,
    BuffWallProjectileCount,
    Projectile,
    ImpactVfx
}

public enum LaneAxis
{
    X,
    Z
}

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
public struct PlayerConfig
{
    public float moveSpeed;
    public float minHorizontal;
    public float maxHorizontal;
    public int baseDamage;
    public int baseProjectileCount;
    public float fireRate;

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
public struct SpawnConfig
{
    public float randomXRange;

    public static SpawnConfig Default => new SpawnConfig
    {
        randomXRange = 5f
    };
}

[Serializable]
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
public sealed class PoolConfig
{
    public PoolEntry[] entries = Array.Empty<PoolEntry>();
}

[Serializable]
public struct PoolEntry
{
    public PoolId id;
    public GameObject prefab;
    public int prewarmCount;
}

[Serializable]
public sealed class WaveConfig
{
    public EnemySpawnData[] enemies = Array.Empty<EnemySpawnData>();
    public BuffWallSpawnData[] buffWalls = Array.Empty<BuffWallSpawnData>();
    public BossSpawnData boss;
    public float delayBeforeWave = 1f;
}

[Serializable]
public struct EnemySpawnData
{
    public EnemyType type;
    public int count;
    public float interval;
    public int health;
    public float speed;
}

[Serializable]
public struct BossSpawnData
{
    public bool enabled;
    public int health;
    public float speed;
    public float delay;
}

[Serializable]
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
public struct BuffData
{
    public BuffType type;
    public int value;
}

public struct ProjectileSpawnData
{
    public Vector3 position;
    public Vector3 direction;
    public int damage;
    public object owner;
}

public struct HudState
{
    public int waveIndex;
    public int totalWaves;
    public int enemiesRemaining;
    public int damageBonus;
    public int projectileCount;
    public GameState gameState;
}
