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
        private readonly EnemySpawnCursor enemyCursor;
        private readonly BuffWallSpawnCursor buffWallCursor;
        private readonly int totalEnemies;
        private float bossTimer;
        private bool bossSpawned;

        public RuntimeWave(WaveConfig config)
        {
            this.config = config;
            enemyCursor = new EnemySpawnCursor(config.enemies);
            buffWallCursor = new BuffWallSpawnCursor(config.buffWalls);
            totalEnemies = enemyCursor.TotalCount + (config.megazord.enabled ? 1 : 0);
            bossTimer = 0f;
            bossSpawned = !config.megazord.enabled;
        }

        public int TotalEnemies => totalEnemies;
        public bool IsCombatSpawnComplete => enemyCursor.IsComplete && bossSpawned;

        public void Update(float deltaTime, SpawnSystem spawnSystem)
        {
            if (enemyCursor.TryUpdate(deltaTime, out EnemySpawnData enemySpawnData))
            {
                spawnSystem.SpawnEnemy(enemySpawnData);
            }

            if (buffWallCursor.TryUpdate(deltaTime, out BuffWallSpawnData buffWallSpawnData))
            {
                spawnSystem.SpawnBuffWall(buffWallSpawnData);
            }

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

    private sealed class EnemySpawnCursor
    {
        private readonly EnemySpawnData[] entries;
        private readonly int totalCount;
        private int entryIndex;
        private int spawnedFromEntry;
        private float timer;

        public EnemySpawnCursor(EnemySpawnData[] entries)
        {
            this.entries = entries ?? System.Array.Empty<EnemySpawnData>();
            totalCount = CalculateTotalCount(this.entries);
            timer = float.MaxValue;
        }

        public bool IsComplete => entryIndex >= entries.Length;
        public int TotalCount => totalCount;

        public bool TryUpdate(float deltaTime, out EnemySpawnData spawnData)
        {
            spawnData = default;

            if (IsComplete)
            {
                return false;
            }

            timer += deltaTime;
            EnemySpawnData entry = entries[entryIndex];
            int count = entry.count;
            float interval = entry.interval;

            if (count <= 0)
            {
                MoveNextEntry();
                return false;
            }

            if (timer < Mathf.Max(0f, interval))
            {
                return false;
            }

            timer = 0f;
            spawnData = entry;
            spawnedFromEntry++;

            if (spawnedFromEntry >= count)
            {
                MoveNextEntry();
            }

            return true;
        }

        private void MoveNextEntry()
        {
            entryIndex++;
            spawnedFromEntry = 0;
            timer = float.MaxValue;
        }

        private static int CalculateTotalCount(EnemySpawnData[] entries)
        {
            int total = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                total += Mathf.Max(0, entries[i].count);
            }

            return total;
        }
    }

    private sealed class BuffWallSpawnCursor
    {
        private readonly BuffWallSpawnData[] entries;
        private int entryIndex;
        private int spawnedFromEntry;
        private float timer;

        public BuffWallSpawnCursor(BuffWallSpawnData[] entries)
        {
            this.entries = entries ?? System.Array.Empty<BuffWallSpawnData>();
            timer = float.MaxValue;
        }

        public bool IsComplete => entryIndex >= entries.Length;

        public bool TryUpdate(float deltaTime, out BuffWallSpawnData spawnData)
        {
            spawnData = default;

            if (IsComplete)
            {
                return false;
            }

            timer += deltaTime;
            BuffWallSpawnData entry = entries[entryIndex];
            int count = entry.count;
            float interval = entry.interval;

            if (count <= 0)
            {
                MoveNextEntry();
                return false;
            }

            if (timer < Mathf.Max(0f, interval))
            {
                return false;
            }

            timer = 0f;
            spawnData = entry;
            spawnedFromEntry++;

            if (spawnedFromEntry >= count)
            {
                MoveNextEntry();
            }

            return true;
        }

        private void MoveNextEntry()
        {
            entryIndex++;
            spawnedFromEntry = 0;
            timer = float.MaxValue;
        }
    }
}