using TMPro;
using UnityEngine.UI;

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Vista MonoBehaviour conectada al Canvas. Expone paneles, textos y botones para que UISystem los controle.
/// </summary>
public sealed class GameUIView : MonoBehaviour
{
    private const int HudCanvasSortingOrder = 100;

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;

    [Header("HUD Dynamic Values")]
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text enemiesText;
    [SerializeField] private TMP_Text buffText;
    [SerializeField] private TMP_Text projectileText;

    [Header("The Button")]
    [SerializeField] private GameObject prefabButton;

    public GameObject PrefabButton => prefabButton;
    private List<Button> activeButtons = new List<Button>();

    private UISystem uiSystem;

    /// <summary>
    /// Recibe el UISystem que responderá a los callbacks de botones.
    /// </summary>
    public void Bind(UISystem uiSystem)
    {
        this.uiSystem = uiSystem;
        EnsureAssignedLayout();
    }

    /// <summary>
    /// Muestra el menú inicial y oculta los demás paneles.
    /// </summary>
    public void ShowMainMenu()
    {
        SetPanels(mainMenu: true, hud: false, pause: false, victory: false, defeat: false);
    }

    /// <summary>
    /// Muestra el HUD de gameplay y limpia botones dinámicos activos.
    /// </summary>
    public void ShowHUD()
    {
        ClearActiveButtons(activeButtons, uiSystem);


        SetPanels(mainMenu: false, hud: true, pause: false, victory: false, defeat: false);
        EnsureAssignedLayout();
    }



    /// <summary>
    /// Muestra pausa y crea botones de Resume, Restart y Quit desde el pool de UI.
    /// </summary>
    public void ShowPause()
    {
        ClearActiveButtons(activeButtons, uiSystem);

        activeButtons.Add(uiSystem.SetUpButton("Resume", OnResumePressed, pausePanel.transform));
        activeButtons.Add(uiSystem.SetUpButton("Restart", OnRestartPressed, pausePanel.transform));
        activeButtons.Add(uiSystem.SetUpButton("Quit", OnQuitPressed, pausePanel.transform));

        SetPanels(mainMenu: false, hud: true, pause: true, victory: false, defeat: false);
        EnsureAssignedLayout();
    }

    /// <summary>
    /// Muestra pantalla de victoria con botones de reinicio y salida.
    /// </summary>
    public void ShowVictory()
    {
        ClearActiveButtons(activeButtons, uiSystem);

        activeButtons.Add(uiSystem.SetUpButton("Restart Game", OnRestartPressed, victoryPanel.transform));
        activeButtons.Add(uiSystem.SetUpButton("Quit Game", OnQuitPressed, victoryPanel.transform));

        SetPanels(mainMenu: false, hud: false, pause: false, victory: true, defeat: false);
        EnsureAssignedLayout();
    }

    /// <summary>
    /// Muestra pantalla de derrota con botones de reinicio y salida.
    /// </summary>
    public void ShowDefeat()
    {
        ClearActiveButtons(activeButtons, uiSystem);

        activeButtons.Add(uiSystem.SetUpButton("Restart Game", OnRestartPressed, defeatPanel.transform));
        activeButtons.Add(uiSystem.SetUpButton("Quit Game", OnQuitPressed, defeatPanel.transform));

        SetPanels(mainMenu: false, hud: false, pause: false, victory: false, defeat: true);
        EnsureAssignedLayout();
    }

    /// <summary>
    /// Escribe los valores dinámicos del HUD a partir del estado calculado por UISystem.
    /// </summary>
    public void RenderHud(HudState state)
    {
        if (waveText != null)
        {
            waveText.text = $"{state.waveIndex}/{state.totalWaves}";
        }

        if (enemiesText != null)
        {
            enemiesText.text = state.enemiesRemaining.ToString();
        }

        if (buffText != null)
        {
            buffText.text = $"+{state.damageBonus}";
        }

        if (projectileText != null)
        {
            projectileText.text = state.projectileCount.ToString();
        }
    }

    /// <summary>
    /// Callback del botón Play configurado en la escena.
    /// </summary>
    public void OnPlayPressed()
    {
        uiSystem?.OnPlayRequested();
    }

    /// <summary>
    /// Callback de botones de reinicio.
    /// </summary>
    public void OnRestartPressed()
    {
        uiSystem?.OnRestartRequested();
    }

    /// <summary>
    /// Callback del botón Resume.
    /// </summary>
    public void OnResumePressed()
    {
        uiSystem?.OnResumeRequested();
    }

    /// <summary>
    /// Callback del botón Pause si existe en la escena.
    /// </summary>
    public void OnPausePressed()
    {
        uiSystem?.OnPauseRequested();
    }

    /// <summary>
    /// Callback de botones de salida.
    /// </summary>
    public void OnQuitPressed()
    {
        uiSystem?.OnQuitRequested();
    }

    /// <summary>
    /// Activa exactamente los paneles solicitados para cada estado de UI.
    /// </summary>
    private void SetPanels(bool mainMenu, bool hud, bool pause, bool victory, bool defeat)
    {
        SetActive(mainMenuPanel, mainMenu);
        SetActive(hudPanel, hud);
        SetActive(pausePanel, pause);
        SetActive(victoryPanel, victory);
        SetActive(defeatPanel, defeat);
    }

    /// <summary>
    /// Activa o desactiva un panel si la referencia existe.
    /// </summary>
    private static void SetActive(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }

    /// <summary>
    /// Normaliza Canvas y paneles asignados para que ocupen pantalla completa.
    /// </summary>
    private void EnsureAssignedLayout()
    {
        EnsureCanvasOverlay();
        EnsureFullScreenPanel(hudPanel);
        EnsureFullScreenPanel(mainMenuPanel);
        EnsureFullScreenPanel(pausePanel);
        EnsureFullScreenPanel(victoryPanel);
        EnsureFullScreenPanel(defeatPanel);
    }

    /// <summary>
    /// Asegura que el Canvas principal se renderice en Screen Space Overlay.
    /// </summary>
    private void EnsureCanvasOverlay()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = HudCanvasSortingOrder;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        if (canvasRect != null)
        {
            canvasRect.localScale = Vector3.one;
        }
    }

    /// <summary>
    /// Ajusta anclas y offsets de un panel para cubrir todo el Canvas.
    /// </summary>
    private static void EnsureFullScreenPanel(GameObject panel)
    {
        if (panel == null || !panel.TryGetComponent(out RectTransform rectTransform))
        {
            return;
        }

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }

    /// <summary>
    /// Devuelve al pool los botones dinámicos que estaban visibles.
    /// </summary>
    private static void ClearActiveButtons(List<Button> activeButtons, UISystem uiSystem)
    {
        for (int i = 0; i < activeButtons.Count; i++)
        {
            Button button = activeButtons[i];
            uiSystem.ReturnButton(button);
        }

        activeButtons.Clear();
    }
}
