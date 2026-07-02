/// <summary>
/// Evento emitido por WaveSystem al comenzar una nueva wave.
/// </summary>
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

/// <summary>
/// Evento emitido por WaveSystem cuando una wave termina.
/// </summary>
public readonly struct WaveCompletedEvent
{
    public readonly int WaveIndex;

    public WaveCompletedEvent(int waveIndex)
    {
        WaveIndex = waveIndex;
    }
}

/// <summary>
/// Evento emitido por SpawnSystem al activar un enemigo desde el pool.
/// </summary>
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

/// <summary>
/// Evento emitido por CombatSystem al derrotar un enemigo.
/// </summary>
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

/// <summary>
/// Evento emitido al destruir una pared de buff para que PlayerSystem aplique la bonificación.
/// </summary>
public readonly struct BuffWallDestroyedEvent
{
    public readonly BuffData Buff;

    public BuffWallDestroyedEvent(BuffData buff)
    {
        Buff = buff;
    }
}

/// <summary>
/// Evento emitido por PlayerSystem cuando cambia el daño o la cantidad de proyectiles.
/// </summary>
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

/// <summary>
/// Evento emitido cuando el jugador pierde la partida.
/// </summary>
public readonly struct PlayerDefeatedEvent
{
}

/// <summary>
/// Evento emitido por GameStateSystem para mostrar victoria o derrota en UI.
/// </summary>
public readonly struct GameEndedEvent
{
    public readonly GameResult Result;

    public GameEndedEvent(GameResult result)
    {
        Result = result;
    }
}

/// <summary>
/// Evento emitido al iniciar o reiniciar la partida para limpiar estados visuales.
/// </summary>
public readonly struct GameRestartedEvent
{
}
