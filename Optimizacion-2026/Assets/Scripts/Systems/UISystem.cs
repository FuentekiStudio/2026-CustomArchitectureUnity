using UnityEngine;

public sealed class UISystem : IUpdateable
{
    private readonly GameStateSystem gameState;
    private readonly IGameEventBus eventBus;
    private readonly EventBinding<WaveStartedEvent> waveStartedBinding;
    private readonly EventBinding<EnemyDefeatedEvent> enemyDefeatedBinding;
    private readonly EventBinding<BuffAppliedEvent> buffAppliedBinding;
    private readonly EventBinding<GameEndedEvent> gameEndedBinding;
    private readonly EventBinding<GameRestartedEvent> restartedBinding;
    private HudState hudState;
    private GameUIView view;
    private bool dirty;

    public UISystem(GameStateSystem gameState, IGameEventBus eventBus)
    {
        this.gameState = gameState;
        this.eventBus = eventBus;
        waveStartedBinding = new EventBinding<WaveStartedEvent>(OnWaveStarted);
        enemyDefeatedBinding = new EventBinding<EnemyDefeatedEvent>(OnEnemyDefeated);
        buffAppliedBinding = new EventBinding<BuffAppliedEvent>(OnBuffApplied);
        gameEndedBinding = new EventBinding<GameEndedEvent>(OnGameEnded);
        restartedBinding = new EventBinding<GameRestartedEvent>(OnGameRestarted);
        eventBus.Register(waveStartedBinding);
        eventBus.Register(enemyDefeatedBinding);
        eventBus.Register(buffAppliedBinding);
        eventBus.Register(gameEndedBinding);
        eventBus.Register(restartedBinding);
    }

    public void BindView(GameUIView view)
    {
        this.view = view;
        if (view != null)
        {
            view.Bind(this);
            view.ShowMainMenu();
        }
    }

    public void Dispose()
    {
        eventBus.Deregister(waveStartedBinding);
        eventBus.Deregister(enemyDefeatedBinding);
        eventBus.Deregister(buffAppliedBinding);
        eventBus.Deregister(gameEndedBinding);
        eventBus.Deregister(restartedBinding);
    }

    public void Update(float deltaTime)
    {
        FlushIfDirty();
    }

    public void OnPlayRequested()
    {
        gameState.StartGame();
        view?.ShowHUD();
    }

    public void OnRestartRequested()
    {
        gameState.Restart();
        view?.ShowHUD();
    }

    public void OnResumeRequested()
    {
        gameState.Resume();
        view?.ShowHUD();
    }

    public void OnQuitRequested()
    {
        gameState.Quit();
    }

    public void OnPauseRequested()
    {
        gameState.Pause();
        view?.ShowPause();
    }

    public void FlushIfDirty()
    {
        if (!dirty || view == null)
        {
            return;
        }

        hudState.gameState = gameState.State;
        view.RenderHud(hudState);
        dirty = false;
    }

    private void OnWaveStarted(WaveStartedEvent eventData)
    {
        hudState.waveIndex = eventData.WaveIndex;
        hudState.totalWaves = eventData.TotalWaves;
        hudState.enemiesRemaining = eventData.EnemiesRemaining;
        dirty = true;
    }

    private void OnEnemyDefeated(EnemyDefeatedEvent eventData)
    {
        hudState.enemiesRemaining = eventData.EnemiesRemaining;
        dirty = true;
    }

    private void OnBuffApplied(BuffAppliedEvent eventData)
    {
        hudState.damageBonus = eventData.DamageBonus;
        hudState.projectileCount = eventData.ProjectileCount;
        dirty = true;
    }

    private void OnGameEnded(GameEndedEvent eventData)
    {
        if (eventData.Result == GameResult.Victory)
        {
            view?.ShowVictory();
        }
        else
        {
            view?.ShowDefeat();
        }

        dirty = true;
    }

    private void OnGameRestarted(GameRestartedEvent eventData)
    {
        hudState = new HudState();
        dirty = true;
    }
}
