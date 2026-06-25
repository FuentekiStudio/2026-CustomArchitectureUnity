using TMPro;
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

    [Header("HUD")]
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text enemiesText;
    [SerializeField] private TMP_Text buffText;
    [SerializeField] private TMP_Text projectileText;

    private UISystem uiSystem;

    public void Bind(UISystem uiSystem)
    {
        this.uiSystem = uiSystem;
        EnsureHudIsVisible();
    }

    public void ShowMainMenu()
    {
        SetPanels(mainMenu: true, hud: false, pause: false, victory: false, defeat: false);
    }

    public void ShowHUD()
    {
        SetPanels(mainMenu: false, hud: true, pause: false, victory: false, defeat: false);
        EnsureHudIsVisible();
    }

    public void ShowPause()
    {
        SetPanels(mainMenu: false, hud: true, pause: true, victory: false, defeat: false);
        EnsureHudIsVisible();
    }

    public void ShowVictory()
    {
        SetPanels(mainMenu: false, hud: false, pause: false, victory: true, defeat: false);
    }

    public void ShowDefeat()
    {
        SetPanels(mainMenu: false, hud: false, pause: false, victory: false, defeat: true);
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

    private void EnsureHudIsVisible()
    {
        EnsureCanvasOverlay();
        EnsureFullScreenPanel(hudPanel);
        ConfigureHudText(waveText, new Vector2(24f, -24f), "0/0");
        ConfigureHudText(enemiesText, new Vector2(24f, -64f), "0");
        ConfigureHudText(buffText, new Vector2(24f, -104f), "+0");
        ConfigureHudText(projectileText, new Vector2(24f, -144f), "1");
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

    private static void ConfigureHudText(TMP_Text text, Vector2 anchoredPosition, string fallbackValue)
    {
        if (text == null)
        {
            return;
        }

        text.gameObject.SetActive(true);
        text.text = string.IsNullOrEmpty(text.text) ? fallbackValue : text.text;
        text.color = Color.white;
        text.fontSize = 32f;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.raycastTarget = false;

        RectTransform rectTransform = text.rectTransform;
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(260f, 36f);
        rectTransform.localScale = Vector3.one;
    }
}
