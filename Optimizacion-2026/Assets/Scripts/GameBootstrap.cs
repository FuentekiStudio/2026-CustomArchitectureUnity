using UnityEngine;
using UnityServiceLocator;

/// <summary>
/// Punto de entrada de la escena. Inicializa servicios, sistemas y pools desde las referencias asignadas en el Inspector.
/// Lo usa Unity mediante Awake y no contiene lógica de gameplay por frame.
/// </summary>
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
    private VfxSystem vfxSystem;
    private BuffWallSystem buffWallSystem;
    private ProjectileSystem projectileSystem;
    private CollisionSystem collisionSystem;
    private RenderingSyncSystem renderingSyncSystem;
    private UISystem uiSystem;
    private PhysicsRegistry physicsRegistry;
    private PoolService poolService;

    private TimeService timeService;
    private InputSystemService inputService;

    /// <summary>
    /// Valida referencias de escena, construye servicios y registra los sistemas en el CustomUpdateManager.
    /// </summary>
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

    /// <summary>
    /// Libera suscripciones, servicios y sistemas cuando se destruye la escena o el objeto bootstrap.
    /// </summary>
    private void OnDestroy()
    {
        playerSystem?.Dispose();
        uiSystem?.Dispose();
        eventBus?.Clear();
        serviceLocator?.Clear();
        updateManager?.Clear();
    }

    /// <summary>
    /// Comprueba que todas las referencias requeridas estén asignadas antes de iniciar la arquitectura.
    /// </summary>
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

    /// <summary>
    /// Valida una referencia individual y reporta el campo faltante en consola.
    /// </summary>
    private bool ValidateReference(Object reference, string fieldName)
    {
        if (reference != null)
        {
            return true;
        }

        Debug.LogError($"GameBootstrap missing required reference: {fieldName}", this);
        return false;
    }

    /// <summary>
    /// Crea servicios transversales como EventBus, pools, input, tiempo y registro de física.
    /// </summary>
    private void BuildServices()
    {
        serviceLocator = new ServiceLocator();
        eventBus = new GameEventBus();
        physicsRegistry = new PhysicsRegistry();
        poolService = new PoolService();
        timeService = new TimeService();
        inputService = new InputSystemService();

        poolService.Prewarm(gameConfig, poolRoot);

        serviceLocator
            .Register<IGameEventBus>(eventBus)
            .Register(physicsRegistry)
            .Register(poolService)
            .Register(timeService)
            .Register(inputService);
    }

    /// <summary>
    /// Construye los sistemas C# puros y les pasa las dependencias que necesitan por constructor.
    /// </summary>
    private void BuildSystems()
    {
        gameStateSystem = new GameStateSystem(eventBus, timeService, poolService, ResetGameplay);
        enemySystem = new EnemySystem(poolService, physicsRegistry, gameConfig, gameStateSystem);
        vfxSystem = new VfxSystem(poolService, gameStateSystem);
        buffWallSystem = new BuffWallSystem(poolService, physicsRegistry, gameConfig, gameStateSystem);
        projectileSystem = new ProjectileSystem(poolService, physicsRegistry, gameConfig, gameStateSystem);

        SpawnSystem spawnSystem = new SpawnSystem(
            poolService,
            physicsRegistry,
            gameConfig,
            enemySpawnPoint,
            buffWallSpawnPoint,
            enemySystem,
            vfxSystem,
            buffWallSystem,
            projectileSystem,
            eventBus);

        CombatSystem combatSystem = new CombatSystem(enemySystem, buffWallSystem, eventBus, poolService, spawnSystem);
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
            .Register(vfxSystem)
            .Register(buffWallSystem)
            .Register(projectileSystem)
            .Register(collisionSystem)
            .Register(renderingSyncSystem)
            .Register(uiSystem);
    }

    /// <summary>
    /// Registra cada sistema en la etapa correcta del loop centralizado: Update, FixedUpdate o LateUpdate.
    /// </summary>
    private void RegisterSystems()
    {
        updateManager.RegisterUpdateable(playerSystem);
        updateManager.RegisterUpdateable(waveSystem);
        updateManager.RegisterUpdateable(uiSystem);
        updateManager.RegisterUpdateable(vfxSystem);
        updateManager.RegisterFixedUpdateable(enemySystem);
        updateManager.RegisterFixedUpdateable(buffWallSystem);
        updateManager.RegisterFixedUpdateable(projectileSystem);
        updateManager.RegisterFixedUpdateable(collisionSystem);
        updateManager.RegisterLateUpdateable(renderingSyncSystem);
    }

    /// <summary>
    /// Restaura el estado runtime del gameplay al iniciar o reiniciar una partida.
    /// </summary>
    private void ResetGameplay()
    {
        physicsRegistry.Clear();
        projectileSystem?.Clear();
        enemySystem?.Clear();
        vfxSystem?.Clear();
        buffWallSystem?.Clear();
        playerSystem?.Reset();
        waveSystem?.Reset();
    }
}
