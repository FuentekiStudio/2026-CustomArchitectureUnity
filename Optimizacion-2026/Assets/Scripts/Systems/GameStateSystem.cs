using UnityEngine;

/// <summary>
/// Controla el estado global de la partida. Lo usan UI, WaveSystem, PlayerSystem y CollisionSystem.
/// </summary>
public sealed class GameStateSystem
{
    private readonly IGameEventBus eventBus;
    private readonly TimeService timeService;
    private readonly PoolService poolService;
    private readonly System.Action resetGameplay;

    /// <summary>
    /// Recibe servicios y callback de reseteo que ejecuta GameBootstrap.
    /// </summary>
    public GameStateSystem(IGameEventBus eventBus, TimeService timeService, PoolService poolService, System.Action resetGameplay)
    {
        this.eventBus = eventBus;
        this.timeService = timeService;
        this.poolService = poolService;
        this.resetGameplay = resetGameplay;
        State = GameState.MainMenu;
    }

    public GameState State { get; private set; }
    public bool IsGameplayRunning => State == GameState.Playing;

    /// <summary>
    /// Inicia una partida nueva, devuelve pools, resetea gameplay y avisa a la UI.
    /// </summary>
    public void StartGame()
    {
        poolService.ReturnAll();
        resetGameplay?.Invoke();
        timeService.SetPaused(false);
        State = GameState.Playing;
        eventBus.Raise(new GameRestartedEvent());
    }

    /// <summary>
    /// Pausa la partida si está en estado Playing.
    /// </summary>
    public void Pause()
    {
        if (State != GameState.Playing)
        {
            return;
        }

        State = GameState.Paused;
        timeService.SetPaused(true);
    }

    /// <summary>
    /// Reanuda la partida si estaba pausada.
    /// </summary>
    public void Resume()
    {
        if (State != GameState.Paused)
        {
            return;
        }

        State = GameState.Playing;
        timeService.SetPaused(false);
    }

    /// <summary>
    /// Reinicia el nivel reutilizando el mismo flujo de StartGame.
    /// </summary>
    public void Restart()
    {
        //CLogger.Log("GameStateSystem: Restart called");
        StartGame();
    }

    /// <summary>
    /// Marca victoria y publica el evento de fin de juego.
    /// </summary>
    public void Win()
    {
        if (State == GameState.Victory)
        {
            return;
        }

        State = GameState.Victory;
        timeService.SetPaused(false);
        eventBus.Raise(new GameEndedEvent(GameResult.Victory));
    }

    /// <summary>
    /// Marca derrota y publica los eventos de jugador derrotado y fin de juego.
    /// </summary>
    public void Lose()
    {
        if (State == GameState.Defeat)
        {
            return;
        }

        State = GameState.Defeat;
        timeService.SetPaused(false);
        eventBus.Raise(new PlayerDefeatedEvent());
        eventBus.Raise(new GameEndedEvent(GameResult.Defeat));
    }

    /// <summary>
    /// Sale del juego en build o detiene Play Mode dentro del editor.
    /// </summary>
    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
