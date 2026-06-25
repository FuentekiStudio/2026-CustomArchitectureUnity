using UnityEngine;

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

    public void Reset()
    {
        currentWaveIndex = 0;
        waveDelayTimer = 0f;
        waitingForNextWave = true;
        runtimeWave = null;
    }

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

    public bool IsFinalWave()
    {
        return currentWaveIndex >= TotalWaves - 1;
    }

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

    private void UpdateWaveDelay(float deltaTime)
    {
        WaveConfig wave = config.waves[currentWaveIndex];
        waveDelayTimer += deltaTime;
        if (waveDelayTimer >= Mathf.Max(0f, wave.delayBeforeWave))
        {
            StartNextWave();
        }
    }

    private sealed class RuntimeWave
    {
        private readonly WaveConfig config;
        private readonly SpawnCursor<EnemySpawnData> enemyCursor;
        private readonly SpawnCursor<BuffWallSpawnData> buffWallCursor;
        private float bossTimer;
        private bool bossSpawned;

        public RuntimeWave(WaveConfig config)
        {
            this.config = config;
            enemyCursor = new SpawnCursor<EnemySpawnData>(config.enemies);
            buffWallCursor = new SpawnCursor<BuffWallSpawnData>(config.buffWalls);
            bossTimer = 0f;
            bossSpawned = !config.boss.enabled;
        }

        public int TotalEnemies => enemyCursor.TotalCount + (config.boss.enabled ? 1 : 0);
        public bool IsCombatSpawnComplete => enemyCursor.IsComplete && bossSpawned;

        public void Update(float deltaTime, SpawnSystem spawnSystem)
        {
            enemyCursor.Update(deltaTime, data => spawnSystem.SpawnEnemy(data));
            buffWallCursor.Update(deltaTime, data => spawnSystem.SpawnBuffWall(data));

            if (!bossSpawned)
            {
                bossTimer += deltaTime;
                if (bossTimer >= Mathf.Max(0f, config.boss.delay))
                {
                    spawnSystem.SpawnEnemy(new EnemySpawnData
                    {
                        type = EnemyType.Boss,
                        count = 1,
                        interval = 0f,
                        health = config.boss.health,
                        speed = config.boss.speed
                    });
                    bossSpawned = true;
                }
            }
        }
    }

    private sealed class SpawnCursor<T>
    {
        private readonly T[] entries;
        private int entryIndex;
        private int spawnedFromEntry;
        private float timer;

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

        private void MoveNextEntry()
        {
            entryIndex++;
            spawnedFromEntry = 0;
            timer = float.MaxValue;
        }

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
