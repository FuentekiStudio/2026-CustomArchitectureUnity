using UnityEngine;
using UnityServiceLocator;

public sealed class GameBootstrap : MonoBehaviour
{
    [SerializeField] private CustomUpdateManager updateManager;
    [SerializeField] private GameUIView gameUIView;
    [SerializeField] private GameConfig gameConfig;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform enemySpawnPoint;
    [SerializeField] private Transform buffWallSpawnPoint;
    [SerializeField] private Transform playerLine;
    [SerializeField] private Transform poolRoot;
    [SerializeField] private bool startGameOnAwake = true;

    private ServiceLocator serviceLocator;
    private GameEventBus eventBus;
    private GameStateSystem gameStateSystem;
    private PlayerSystem playerSystem;
    private WaveSystem waveSystem;
    private EnemySystem enemySystem;
    private BuffWallSystem buffWallSystem;
    private ProjectileSystem projectileSystem;
    private CollisionSystem collisionSystem;
    private RenderingSyncSystem renderingSyncSystem;
    private UISystem uiSystem;
    private PhysicsRegistry physicsRegistry;
    private PoolService poolService;

    private void Awake()
    {
        if (!ResolveSceneReferences())
        {
            return;
        }

        BuildServices();
        BuildSystems();
        RegisterSystems();
        uiSystem.BindView(gameUIView);

        if (startGameOnAwake)
        {
            uiSystem.OnPlayRequested();
        }
    }

    private void OnDestroy()
    {
        playerSystem?.Dispose();
        uiSystem?.Dispose();
        eventBus?.Clear();
        serviceLocator?.Clear();
        updateManager?.Clear();
    }

    private bool ResolveSceneReferences()
    {
        bool valid = true;
        valid &= ValidateReference(updateManager, nameof(updateManager));
        valid &= ValidateReference(gameUIView, nameof(gameUIView));
        valid &= ValidateReference(gameConfig, nameof(gameConfig));
        valid &= ValidateReference(playerTransform, nameof(playerTransform));
        valid &= ValidateReference(enemySpawnPoint, nameof(enemySpawnPoint));
        valid &= ValidateReference(buffWallSpawnPoint, nameof(buffWallSpawnPoint));
        valid &= ValidateReference(playerLine, nameof(playerLine));
        valid &= ValidateReference(poolRoot, nameof(poolRoot));

        if (!valid)
        {
            enabled = false;
        }

        return valid;
    }

    private bool ValidateReference(Object reference, string fieldName)
    {
        if (reference != null)
        {
            return true;
        }

        Debug.LogError($"GameBootstrap missing required reference: {fieldName}", this);
        return false;
    }

    private void BuildServices()
    {
        serviceLocator = new ServiceLocator();
        eventBus = new GameEventBus();
        physicsRegistry = new PhysicsRegistry();
        poolService = new PoolService();
        TimeService timeService = new TimeService();
        InputSystemService inputService = new InputSystemService();

        poolService.Prewarm(gameConfig, poolRoot);

        serviceLocator
            .Register<IGameEventBus>(eventBus)
            .Register(physicsRegistry)
            .Register(poolService)
            .Register(timeService)
            .Register(inputService);
    }

    private void BuildSystems()
    {
        TimeService timeService = serviceLocator.Get<TimeService>();
        InputSystemService inputService = serviceLocator.Get<InputSystemService>();

        gameStateSystem = new GameStateSystem(eventBus, timeService, poolService, ResetGameplay);
        enemySystem = new EnemySystem(poolService, physicsRegistry, gameConfig, gameStateSystem);
        buffWallSystem = new BuffWallSystem(poolService, physicsRegistry, gameConfig, gameStateSystem);
        projectileSystem = new ProjectileSystem(poolService, physicsRegistry, gameConfig, gameStateSystem);

        SpawnSystem spawnSystem = new SpawnSystem(
            poolService,
            physicsRegistry,
            gameConfig,
            enemySpawnPoint,
            buffWallSpawnPoint,
            enemySystem,
            buffWallSystem,
            projectileSystem,
            eventBus);

        CombatSystem combatSystem = new CombatSystem(enemySystem, buffWallSystem, eventBus);
        playerSystem = new PlayerSystem(gameConfig, inputService, spawnSystem, eventBus, gameStateSystem, playerTransform);
        waveSystem = new WaveSystem(gameConfig, spawnSystem, enemySystem, gameStateSystem, eventBus);
        collisionSystem = new CollisionSystem(projectileSystem, enemySystem, buffWallSystem, combatSystem, gameStateSystem, physicsRegistry, gameConfig, playerLine);
        renderingSyncSystem = new RenderingSyncSystem(enemySystem, buffWallSystem, projectileSystem);
        uiSystem = new UISystem(gameStateSystem, eventBus);

        serviceLocator
            .Register(gameStateSystem)
            .Register(spawnSystem)
            .Register(combatSystem)
            .Register(playerSystem)
            .Register(waveSystem)
            .Register(enemySystem)
            .Register(buffWallSystem)
            .Register(projectileSystem)
            .Register(collisionSystem)
            .Register(renderingSyncSystem)
            .Register(uiSystem);
    }

    private void RegisterSystems()
    {
        updateManager.RegisterUpdateable(playerSystem);
        updateManager.RegisterUpdateable(waveSystem);
        updateManager.RegisterUpdateable(uiSystem);
        updateManager.RegisterFixedUpdateable(enemySystem);
        updateManager.RegisterFixedUpdateable(buffWallSystem);
        updateManager.RegisterFixedUpdateable(projectileSystem);
        updateManager.RegisterFixedUpdateable(collisionSystem);
        updateManager.RegisterLateUpdateable(renderingSyncSystem);
    }

    private void ResetGameplay()
    {
        physicsRegistry.Clear();
        projectileSystem?.Clear();
        enemySystem?.Clear();
        buffWallSystem?.Clear();
        playerSystem?.Reset();
        waveSystem?.Reset();
    }
}
