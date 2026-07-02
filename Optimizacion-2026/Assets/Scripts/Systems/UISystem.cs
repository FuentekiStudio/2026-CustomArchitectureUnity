using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

/// <summary>
/// Sistema lógico de UI. Escucha eventos de gameplay, mantiene HudState y ordena a GameUIView qué mostrar.
/// </summary>
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


    private List<GameObject> activeButtons = new List<GameObject>();
    private List<GameObject> deactiveButtons = new List<GameObject>();

    private int buttonPoolSize = 5;

    /// <summary>
    /// Crea bindings de eventos y se suscribe al EventBus para reaccionar a gameplay.
    /// </summary>
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

    /// <summary>
    /// Precarga botones reutilizables para menús de pausa, victoria y derrota.
    /// </summary>
    public void InitButtons()
    {

        for (int i = 0; i < buttonPoolSize; i++)
        {
            GameObject button = Object.Instantiate(view.PrefabButton, view.transform);
            button.SetActive(false);
            deactiveButtons.Add(button);
        }
    }

    /// <summary>
    /// Obtiene un botón disponible del pool visual de UI.
    /// </summary>
    public Button GetButton()
    {
        if (deactiveButtons.Count > 0)
        {
            Button button = deactiveButtons[0].GetComponent<Button>();
            deactiveButtons.RemoveAt(0);
            activeButtons.Add(button.gameObject);
            button.gameObject.SetActive(true);
            return button;
        }
        else
        {
            CLogger.Log("No more buttons available in the pool. patatas");
            return null;
        }
    }

    /// <summary>
    /// Configura texto, acción y padre de un botón de menú.
    /// </summary>
    public Button SetUpButton(string buttonText, UnityAction onClickAction, Transform parentTransform)
    {
        Button button = GetButton();
        if (button != null)
        {
            TextMeshProUGUI buttonTextComponent = button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonTextComponent != null)
            {
                buttonTextComponent.text = buttonText;
            }
            else
            {
                CLogger.Log("Button prefab is missing a TextMeshProUGUI component.");
            }

            button.onClick.AddListener(onClickAction);
            button.transform.SetParent(parentTransform, false);
        }
        return button;
    }

    /// <summary>
    /// Limpia listeners y devuelve un botón al pool de UI.
    /// </summary>
    public void ReturnButton(Button button)
    {
        if (activeButtons.Contains(button.gameObject))
        {

            button.onClick.RemoveAllListeners();
            activeButtons.Remove(button.gameObject);
            deactiveButtons.Add(button.gameObject);
            button.gameObject.transform.SetParent(view.transform, false);
            button.gameObject.SetActive(false);
        }

    }

    /// <summary>
    /// Vincula la vista MonoBehaviour y muestra el menú inicial.
    /// </summary>
    public void BindView(GameUIView view)
    {
        this.view = view;
        if (view != null)
        {
            view.Bind(this);
            view.ShowMainMenu();
            InitButtons();

        }
    }

    /// <summary>
    /// Cancela suscripciones al EventBus al destruir la arquitectura.
    /// </summary>
    public void Dispose()
    {
        eventBus.Deregister(waveStartedBinding);
        eventBus.Deregister(enemyDefeatedBinding);
        eventBus.Deregister(buffAppliedBinding);
        eventBus.Deregister(gameEndedBinding);
        eventBus.Deregister(restartedBinding);
    }

    /// <summary>
    /// Aplica cambios pendientes al HUD solo cuando está marcado como dirty.
    /// </summary>
    public void Update(float deltaTime)
    {
        FlushIfDirty();
    }

    /// <summary>
    /// Atiende el botón Play y pasa el juego a HUD activo.
    /// </summary>
    public void OnPlayRequested()
    {
        gameState.StartGame();
        view?.ShowHUD();
    }

    /// <summary>
    /// Atiende el botón Restart y reinicia la partida.
    /// </summary>
    public void OnRestartRequested()
    {
        gameState.Restart();
        view?.ShowHUD();
    }

    /// <summary>
    /// Atiende el botón Resume y vuelve al HUD de gameplay.
    /// </summary>
    public void OnResumeRequested()
    {
        gameState.Resume();
        view?.ShowHUD();
    }

    /// <summary>
    /// Atiende el botón Quit y delega la salida en GameStateSystem.
    /// </summary>
    public void OnQuitRequested()
    {
        gameState.Quit();
    }

    /// <summary>
    /// Atiende una pausa solicitada desde UI y muestra el panel de pausa.
    /// </summary>
    public void OnPauseRequested()
    {
        gameState.Pause();
        view?.ShowPause();
    }

    /// <summary>
    /// Renderiza el HUD únicamente si algún evento marcó cambios pendientes.
    /// </summary>
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

    /// <summary>
    /// Actualiza datos de wave y enemigos restantes al iniciar una wave.
    /// </summary>
    private void OnWaveStarted(WaveStartedEvent eventData)
    {
        hudState.waveIndex = eventData.WaveIndex;
        hudState.totalWaves = eventData.TotalWaves;
        hudState.enemiesRemaining = eventData.EnemiesRemaining;
        dirty = true;
    }

    /// <summary>
    /// Actualiza el contador de enemigos restantes tras una muerte.
    /// </summary>
    private void OnEnemyDefeated(EnemyDefeatedEvent eventData)
    {
        hudState.enemiesRemaining = eventData.EnemiesRemaining;
        dirty = true;
    }

    /// <summary>
    /// Actualiza datos de buff visibles en HUD.
    /// </summary>
    private void OnBuffApplied(BuffAppliedEvent eventData)
    {
        hudState.damageBonus = eventData.DamageBonus;
        hudState.projectileCount = eventData.ProjectileCount;
        dirty = true;
    }

    /// <summary>
    /// Muestra pantalla de victoria o derrota según el resultado publicado.
    /// </summary>
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

    /// <summary>
    /// Reinicia los datos del HUD cuando comienza una partida nueva.
    /// </summary>
    private void OnGameRestarted(GameRestartedEvent eventData)
    {
        hudState = new HudState();
        dirty = true;
    }
}
