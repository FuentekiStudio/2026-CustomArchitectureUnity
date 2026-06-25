public readonly struct WaveStartedEvent
{
    public readonly int WaveIndex;
    public readonly int TotalWaves;
    public readonly int EnemiesRemaining;

    public WaveStartedEvent(int waveIndex, int totalWaves, int enemiesRemaining)
    {
        WaveIndex = waveIndex;
        TotalWaves = totalWaves;
        EnemiesRemaining = enemiesRemaining;
    }
}

public readonly struct WaveCompletedEvent
{
    public readonly int WaveIndex;

    public WaveCompletedEvent(int waveIndex)
    {
        WaveIndex = waveIndex;
    }
}

public readonly struct EnemySpawnedEvent
{
    public readonly int EnemyId;
    public readonly EnemyType EnemyType;

    public EnemySpawnedEvent(int enemyId, EnemyType enemyType)
    {
        EnemyId = enemyId;
        EnemyType = enemyType;
    }
}

public readonly struct EnemyDefeatedEvent
{
    public readonly int EnemyId;
    public readonly int EnemiesRemaining;

    public EnemyDefeatedEvent(int enemyId, int enemiesRemaining)
    {
        EnemyId = enemyId;
        EnemiesRemaining = enemiesRemaining;
    }
}

public readonly struct BuffWallDestroyedEvent
{
    public readonly BuffData Buff;

    public BuffWallDestroyedEvent(BuffData buff)
    {
        Buff = buff;
    }
}

public readonly struct BuffAppliedEvent
{
    public readonly int DamageBonus;
    public readonly int ProjectileCount;

    public BuffAppliedEvent(int damageBonus, int projectileCount)
    {
        DamageBonus = damageBonus;
        ProjectileCount = projectileCount;
    }
}

public readonly struct PlayerDefeatedEvent
{
}

public readonly struct GameEndedEvent
{
    public readonly GameResult Result;

    public GameEndedEvent(GameResult result)
    {
        Result = result;
    }
}

public readonly struct GameRestartedEvent
{
}
