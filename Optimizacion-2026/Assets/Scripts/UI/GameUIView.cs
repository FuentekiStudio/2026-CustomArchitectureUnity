using TMPro;
using UnityEngine;

public sealed class GameUIView : MonoBehaviour
{
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
    }

    public void ShowMainMenu()
    {
        SetPanels(mainMenu: true, hud: false, pause: false, victory: false, defeat: false);
    }

    public void ShowHUD()
    {
        SetPanels(mainMenu: false, hud: true, pause: false, victory: false, defeat: false);
    }

    public void ShowPause()
    {
        SetPanels(mainMenu: false, hud: true, pause: true, victory: false, defeat: false);
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
            waveText.text = $"Wave {state.waveIndex}/{state.totalWaves}";
        }

        if (enemiesText != null)
        {
            enemiesText.text = $"Enemies {state.enemiesRemaining}";
        }

        if (buffText != null)
        {
            buffText.text = $"Damage +{state.damageBonus}";
        }

        if (projectileText != null)
        {
            projectileText.text = $"Projectiles {state.projectileCount}";
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
}
