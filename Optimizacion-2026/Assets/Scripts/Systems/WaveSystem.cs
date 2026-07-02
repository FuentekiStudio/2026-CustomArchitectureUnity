using UnityEngine;

/// <summary>
/// Sistema de waves. Decide cuándo iniciar waves, spawnear enemigos/paredes y declarar victoria.
/// Lo actualiza CustomUpdateManager en Update.
/// </summary>
public sealed class WaveSystem : IUpdateable
{
    private readonly GameConfig config;
    private readonly SpawnSystem spawnSystem;
    private readonly EnemySystem enemySystem;
    private readonly GameStateSystem gameState;
    private readonly IGameEventBus eventBus;

    private int currentWaveIndex;
    private float waveDelayTimer;
    private bool waitingForNextWave;
    private RuntimeWave runtimeWave;

    /// <summary>
    /// Recibe configuración, spawner, enemigos activos, estado global y bus de eventos.
    /// </summary>
    public WaveSystem(GameConfig config, SpawnSystem spawnSystem, EnemySystem enemySystem, GameStateSystem gameState, IGameEventBus eventBus)
    {
        this.config = config;
        this.spawnSystem = spawnSystem;
        this.enemySystem = enemySystem;
        this.gameState = gameState;
        this.eventBus = eventBus;
        Reset();
    }

    public int CurrentWaveIndex => currentWaveIndex;
    public int TotalWaves => config != null && config.waves != null ? config.waves.Length : 0;

    /// <summary>
    /// Vuelve el sistema a la primera wave y limpia el runtime actual.
    /// </summary>
    public void Reset()
    {
        currentWaveIndex = 0;
        waveDelayTimer = 0f;
        waitingForNextWave = true;
        runtimeWave = null;
    }

    /// <summary>
    /// Avanza timers de waves, ejecuta spawns y completa la wave cuando no quedan enemigos.
    /// </summary>
    public void Update(float deltaTime)
    {
        if (!gameState.IsGameplayRunning)
        {
            return;
        }

        if (TotalWaves == 0)
        {
            gameState.Win();
            return;
        }

        if (waitingForNextWave)
        {
            UpdateWaveDelay(deltaTime);
            return;
        }

        runtimeWave.Update(deltaTime, spawnSystem);

        if (runtimeWave.IsCombatSpawnComplete && enemySystem.ActiveCount == 0)
        {
            CompleteCurrentWave();
        }
    }

    /// <summary>
    /// Crea la wave runtime actual y publica el evento de inicio de wave.
    /// </summary>
    public void StartNextWave()
    {
        if (currentWaveIndex >= TotalWaves)
        {
            gameState.Win();
            return;
        }

        runtimeWave = new RuntimeWave(config.waves[currentWaveIndex]);
        waitingForNextWave = false;
        eventBus.Raise(new WaveStartedEvent(currentWaveIndex + 1, TotalWaves, runtimeWave.TotalEnemies));
    }

    /// <summary>
    /// Indica si el índice actual corresponde a la última wave configurada.
    /// </summary>
    public bool IsFinalWave()
    {
        return currentWaveIndex >= TotalWaves - 1;
    }

    /// <summary>
    /// Publica fin de wave y decide si avanzar a la siguiente o declarar victoria.
    /// </summary>
    private void CompleteCurrentWave()
    {
        eventBus.Raise(new WaveCompletedEvent(currentWaveIndex + 1));
        currentWaveIndex++;

        if (currentWaveIndex >= TotalWaves)
        {
            gameState.Win();
            return;
        }

        waitingForNextWave = true;
        waveDelayTimer = 0f;
        runtimeWave = null;
    }

    /// <summary>
    /// Espera la demora configurada antes de iniciar la siguiente wave.
    /// </summary>
    private void UpdateWaveDelay(float deltaTime)
    {
        WaveConfig wave = config.waves[currentWaveIndex];
        waveDelayTimer += deltaTime;
        if (waveDelayTimer >= Mathf.Max(0f, wave.delayBeforeWave))
        {
            StartNextWave();
        }
    }

    /// <summary>
    /// Estado temporal de una wave en ejecución. Administra cursores de spawn y jefe Megazord.
    /// </summary>
    private sealed class RuntimeWave
    {
        private readonly WaveConfig config;
        private readonly SpawnCursor<EnemySpawnData> enemyCursor;
        private readonly SpawnCursor<BuffWallSpawnData> buffWallCursor;
        private float bossTimer;
        private bool bossSpawned;

        /// <summary>
        /// Inicializa los cursores de enemigos y paredes a partir de la configuración de wave.
        /// </summary>
        public RuntimeWave(WaveConfig config)
        {
            this.config = config;
            enemyCursor = new SpawnCursor<EnemySpawnData>(config.enemies);
            buffWallCursor = new SpawnCursor<BuffWallSpawnData>(config.buffWalls);
            bossTimer = 0f;
            bossSpawned = !config.megazord.enabled;
        }

        public int TotalEnemies => enemyCursor.TotalCount + (config.megazord.enabled ? 1 : 0);
        public bool IsCombatSpawnComplete => enemyCursor.IsComplete && bossSpawned;

        /// <summary>
        /// Actualiza spawns regulares y dispara el jefe especial cuando vence su demora.
        /// </summary>
        public void Update(float deltaTime, SpawnSystem spawnSystem)
        {
            enemyCursor.Update(deltaTime, data => spawnSystem.SpawnEnemy(data));
            buffWallCursor.Update(deltaTime, data => spawnSystem.SpawnBuffWall(data));

            if (!bossSpawned)
            {
                bossTimer += deltaTime;
                if (bossTimer >= Mathf.Max(0f, config.megazord.delay))
                {
                    spawnSystem.SpawnEnemy(new EnemySpawnData
                    {
                        type = EnemyType.Megazord,
                        count = 1,
                        interval = 0f,
                        health = config.megazord.health,
                        speed = config.megazord.speed
                    });
                    bossSpawned = true;
                }
            }
        }
    }

    /// <summary>
    /// Cursor genérico que recorre entradas de spawn y respeta cantidad e intervalo configurados.
    /// </summary>
    private sealed class SpawnCursor<T>
    {
        private readonly T[] entries;
        private int entryIndex;
        private int spawnedFromEntry;
        private float timer;

        /// <summary>
        /// Recibe la lista de entradas de spawn que debe procesar en orden.
        /// </summary>
        public SpawnCursor(T[] entries)
        {
            this.entries = entries ?? System.Array.Empty<T>();
            timer = float.MaxValue;
        }

        public bool IsComplete => entryIndex >= entries.Length;
        public int TotalCount
        {
            get
            {
                int total = 0;
                for (int i = 0; i < entries.Length; i++)
                {
                    if (entries[i] is EnemySpawnData enemy)
                    {
                        total += Mathf.Max(0, enemy.count);
                    }
                    else if (entries[i] is BuffWallSpawnData wall)
                    {
                        total += Mathf.Max(0, wall.count);
                    }
                }

                return total;
            }
        }

        /// <summary>
        /// Avanza el timer y ejecuta el callback de spawn cuando corresponde.
        /// </summary>
        public void Update(float deltaTime, System.Action<T> spawn)
        {
            if (IsComplete)
            {
                return;
            }

            timer += deltaTime;
            T entry = entries[entryIndex];
            int count = GetCount(entry);
            float interval = GetInterval(entry);

            if (count <= 0)
            {
                MoveNextEntry();
                return;
            }

            if (timer >= Mathf.Max(0f, interval))
            {
                timer = 0f;
                spawn(entry);
                spawnedFromEntry++;

                if (spawnedFromEntry >= count)
                {
                    MoveNextEntry();
                }
            }
        }

        /// <summary>
        /// Avanza a la siguiente entrada y reinicia contadores internos.
        /// </summary>
        private void MoveNextEntry()
        {
            entryIndex++;
            spawnedFromEntry = 0;
            timer = float.MaxValue;
        }

        /// <summary>
        /// Obtiene la cantidad de spawns definida por la entrada actual.
        /// </summary>
        private static int GetCount(T entry)
        {
            if (entry is EnemySpawnData enemy)
            {
                return enemy.count;
            }

            if (entry is BuffWallSpawnData wall)
            {
                return wall.count;
            }

            return 0;
        }

        /// <summary>
        /// Obtiene el intervalo entre spawns definido por la entrada actual.
        /// </summary>
        private static float GetInterval(T entry)
        {
            if (entry is EnemySpawnData enemy)
            {
                return enemy.interval;
            }

            if (entry is BuffWallSpawnData wall)
            {
                return wall.interval;
            }

            return 0f;
        }
    }
}
