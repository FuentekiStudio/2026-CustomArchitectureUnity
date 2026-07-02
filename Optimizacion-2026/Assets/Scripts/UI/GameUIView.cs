using TMPro;
using UnityEngine.UI;

using System.Collections.Generic;
using UnityEngine;

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

    public void Bind(UISystem uiSystem)
    {
        this.uiSystem = uiSystem;
        EnsureAssignedLayout();
    }

    public void ShowMainMenu()
    {
        SetPanels(mainMenu: true, hud: false, pause: false, victory: false, defeat: false);
    }

    public void ShowHUD()
    {
        ClearActiveButtons(activeButtons, uiSystem);


        SetPanels(mainMenu: false, hud: true, pause: false, victory: false, defeat: false);
        EnsureAssignedLayout();
    }



    public void ShowPause()
    {
        ClearActiveButtons(activeButtons, uiSystem);

        activeButtons.Add(uiSystem.SetUpButton("Resume", OnResumePressed, pausePanel.transform));
        activeButtons.Add(uiSystem.SetUpButton("Restart", OnRestartPressed, pausePanel.transform));
        activeButtons.Add(uiSystem.SetUpButton("Quit", OnQuitPressed, pausePanel.transform));

        SetPanels(mainMenu: false, hud: true, pause: true, victory: false, defeat: false);
        EnsureAssignedLayout();
    }

    public void ShowVictory()
    {
        ClearActiveButtons(activeButtons, uiSystem);

        activeButtons.Add(uiSystem.SetUpButton("Restart Game", OnRestartPressed, victoryPanel.transform));
        activeButtons.Add(uiSystem.SetUpButton("Quit Game", OnQuitPressed, victoryPanel.transform));

        SetPanels(mainMenu: false, hud: false, pause: false, victory: true, defeat: false);
        EnsureAssignedLayout();
    }

    public void ShowDefeat()
    {
        CLogger.Log("GameUIView: ShowDefeat called");
        ClearActiveButtons(activeButtons, uiSystem);

        activeButtons.Add(uiSystem.SetUpButton("Restart Game", OnRestartPressed, defeatPanel.transform));
        activeButtons.Add(uiSystem.SetUpButton("Quit Game", OnQuitPressed, defeatPanel.transform));

        SetPanels(mainMenu: false, hud: false, pause: false, victory: false, defeat: true);
        EnsureAssignedLayout();
    }

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

    public void OnPlayPressed()
    {
        uiSystem?.OnPlayRequested();
    }

    public void OnRestartPressed()
    {
        uiSystem?.OnRestartRequested();
    }

    public void OnResumePressed()
    {
        uiSystem?.OnResumeRequested();
    }

    public void OnPausePressed()
    {
        uiSystem?.OnPauseRequested();
    }

    public void OnQuitPressed()
    {
        uiSystem?.OnQuitRequested();
    }

    private void SetPanels(bool mainMenu, bool hud, bool pause, bool victory, bool defeat)
    {
        SetActive(mainMenuPanel, mainMenu);
        SetActive(hudPanel, hud);
        SetActive(pausePanel, pause);
        SetActive(victoryPanel, victory);
        SetActive(defeatPanel, defeat);
    }

    private static void SetActive(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }

    private void EnsureAssignedLayout()
    {
        EnsureCanvasOverlay();
        EnsureFullScreenPanel(hudPanel);
        EnsureFullScreenPanel(mainMenuPanel);
        EnsureFullScreenPanel(pausePanel);
        EnsureFullScreenPanel(victoryPanel);
        EnsureFullScreenPanel(defeatPanel);
    }

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
