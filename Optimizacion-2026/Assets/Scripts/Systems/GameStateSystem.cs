using UnityEngine;

public sealed class GameStateSystem
{
    private readonly IGameEventBus eventBus;
    private readonly TimeService timeService;
    private readonly PoolService poolService;
    private readonly System.Action resetGameplay;

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

    public void StartGame()
    {
        poolService.ReturnAll();
        resetGameplay?.Invoke();
        timeService.SetPaused(false);
        State = GameState.Playing;
        eventBus.Raise(new GameRestartedEvent());
    }

    public void Pause()
    {
        if (State != GameState.Playing)
        {
            return;
        }

        State = GameState.Paused;
        timeService.SetPaused(true);
    }

    public void Resume()
    {
        if (State != GameState.Paused)
        {
            return;
        }

        State = GameState.Playing;
        timeService.SetPaused(false);
    }

    public void Restart()
    {
        //CLogger.Log("GameStateSystem: Restart called");
        StartGame();
    }

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

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

