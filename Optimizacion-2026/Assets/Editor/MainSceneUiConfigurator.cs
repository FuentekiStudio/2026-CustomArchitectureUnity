using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class MainSceneUiConfigurator
{
    private const string ScenePath = "Assets/Scenes/MainScene.unity";

    [MenuItem("Tools/Optimization/Configure Main Scene UI")]
    public static void Configure()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameUIView view = Object.FindFirstObjectByType<GameUIView>();
        Canvas rootCanvas = view != null ? view.GetComponent<Canvas>() : Object.FindFirstObjectByType<Canvas>();
        if (rootCanvas == null)
        {
            GameObject canvasObject = new GameObject("StaticUICanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            rootCanvas = canvasObject.GetComponent<Canvas>();
        }

        rootCanvas.gameObject.name = "StaticUICanvas";
        rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        rootCanvas.pixelPerfect = false;
        rootCanvas.sortingOrder = 100;
        EnsureFullScreen(rootCanvas.GetComponent<RectTransform>());

        CanvasScaler scaler = rootCanvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = rootCanvas.gameObject.AddComponent<CanvasScaler>();
        }
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (rootCanvas.GetComponent<GraphicRaycaster>() == null)
        {
            rootCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        view = rootCanvas.GetComponent<GameUIView>();
        if (view == null)
        {
            view = rootCanvas.gameObject.AddComponent<GameUIView>();
        }

        GameObject hudPanel = EnsurePanel(rootCanvas.transform, "HUDPanel", true);
        GameObject mainMenuPanel = EnsurePanel(rootCanvas.transform, "MainMenuPanel", false);
        GameObject pausePanel = EnsurePanel(rootCanvas.transform, "PausePanel", false);
        GameObject victoryPanel = EnsurePanel(rootCanvas.transform, "VictoryPanel", false);
        GameObject defeatPanel = EnsurePanel(rootCanvas.transform, "DefeatPanel", false);

        EnsureStaticHudLabel(hudPanel.transform, "WaveLabel", new Vector2(24f, -24f), "Wave");
        EnsureStaticHudLabel(hudPanel.transform, "EnemiesLabel", new Vector2(24f, -64f), "Enemies");
        EnsureStaticHudLabel(hudPanel.transform, "BuffLabel", new Vector2(24f, -104f), "Damage");
        EnsureStaticHudLabel(hudPanel.transform, "ProjectileLabel", new Vector2(24f, -144f), "Projectiles");

        GameObject dynamicHudCanvasObject = EnsurePanel(hudPanel.transform, "DynamicHUDCanvas", true);
        Canvas dynamicCanvas = dynamicHudCanvasObject.GetComponent<Canvas>();
        if (dynamicCanvas == null)
        {
            dynamicCanvas = dynamicHudCanvasObject.AddComponent<Canvas>();
        }
        dynamicCanvas.overrideSorting = true;
        dynamicCanvas.sortingOrder = 101;
        if (dynamicHudCanvasObject.GetComponent<GraphicRaycaster>() != null)
        {
            Object.DestroyImmediate(dynamicHudCanvasObject.GetComponent<GraphicRaycaster>(), true);
        }

        TMP_Text waveText = EnsureDynamicHudValue(dynamicHudCanvasObject.transform, "WaveText", new Vector2(170f, -24f), "0/0");
        TMP_Text enemiesText = EnsureDynamicHudValue(dynamicHudCanvasObject.transform, "EnemiesText", new Vector2(170f, -64f), "0");
        TMP_Text buffText = EnsureDynamicHudValue(dynamicHudCanvasObject.transform, "BuffText", new Vector2(170f, -104f), "+0");
        TMP_Text projectileText = EnsureDynamicHudValue(dynamicHudCanvasObject.transform, "ProjectileText", new Vector2(170f, -144f), "1");
        Button hudPauseButton = EnsureButton(hudPanel.transform, "PauseButton", "Pause", new Vector2(-110f, -42f));
        RectTransform hudPauseRect = hudPauseButton.GetComponent<RectTransform>();
        hudPauseRect.anchorMin = new Vector2(1f, 1f);
        hudPauseRect.anchorMax = new Vector2(1f, 1f);
        hudPauseRect.pivot = new Vector2(1f, 1f);
        hudPauseRect.sizeDelta = new Vector2(180f, 50f);

        EnsureCenteredTitle(mainMenuPanel.transform, "TitleText", "Optimization Shooter", new Vector2(0f, 110f));
        Button playButton = EnsureButton(mainMenuPanel.transform, "PlayButton", "Play", new Vector2(0f, 30f));
        Button quitMenuButton = EnsureButton(mainMenuPanel.transform, "QuitButton", "Quit", new Vector2(0f, -50f));

        EnsureCenteredTitle(pausePanel.transform, "PauseTitle", "Paused", new Vector2(0f, 120f));
        Button resumeButton = EnsureButton(pausePanel.transform, "ResumeButton", "Resume", new Vector2(0f, 40f));
        Button restartPauseButton = EnsureButton(pausePanel.transform, "RestartButton", "Restart", new Vector2(0f, -40f));
        Button quitPauseButton = EnsureButton(pausePanel.transform, "QuitButton", "Quit", new Vector2(0f, -120f));

        EnsureCenteredTitle(victoryPanel.transform, "VictoryTitle", "Victory", new Vector2(0f, 80f));
        Button restartVictoryButton = EnsureButton(victoryPanel.transform, "RestartButton", "Restart", new Vector2(0f, 0f));
        Button quitVictoryButton = EnsureButton(victoryPanel.transform, "QuitButton", "Quit", new Vector2(0f, -80f));

        EnsureCenteredTitle(defeatPanel.transform, "DefeatTitle", "Defeat", new Vector2(0f, 80f));
        Button restartDefeatButton = EnsureButton(defeatPanel.transform, "RestartButton", "Restart", new Vector2(0f, 0f));
        Button quitDefeatButton = EnsureButton(defeatPanel.transform, "QuitButton", "Quit", new Vector2(0f, -80f));

        ReplacePersistentListener(hudPauseButton.onClick, view.OnPausePressed);
        ReplacePersistentListener(playButton.onClick, view.OnPlayPressed);
        ReplacePersistentListener(quitMenuButton.onClick, view.OnQuitPressed);
        ReplacePersistentListener(resumeButton.onClick, view.OnResumePressed);
        ReplacePersistentListener(restartPauseButton.onClick, view.OnRestartPressed);
        ReplacePersistentListener(quitPauseButton.onClick, view.OnQuitPressed);
        ReplacePersistentListener(restartVictoryButton.onClick, view.OnRestartPressed);
        ReplacePersistentListener(quitVictoryButton.onClick, view.OnQuitPressed);
        ReplacePersistentListener(restartDefeatButton.onClick, view.OnRestartPressed);
        ReplacePersistentListener(quitDefeatButton.onClick, view.OnQuitPressed);

        SerializedObject serializedView = new SerializedObject(view);
        SetObject(serializedView, "mainMenuPanel", mainMenuPanel);
        SetObject(serializedView, "hudPanel", hudPanel);
        SetObject(serializedView, "pausePanel", pausePanel);
        SetObject(serializedView, "victoryPanel", victoryPanel);
        SetObject(serializedView, "defeatPanel", defeatPanel);
        SetObject(serializedView, "waveText", waveText);
        SetObject(serializedView, "enemiesText", enemiesText);
        SetObject(serializedView, "buffText", buffText);
        SetObject(serializedView, "projectileText", projectileText);
        serializedView.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(view);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static GameObject EnsurePanel(Transform parent, string name, bool active)
    {
        Transform found = parent.Find(name);
        GameObject panel = found != null ? found.gameObject : new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        panel.SetActive(active);
        EnsureFullScreen(panel.GetComponent<RectTransform>());
        return panel;
    }

    private static void EnsureFullScreen(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
    }

    private static TMP_Text EnsureStaticHudLabel(Transform parent, string name, Vector2 anchoredPosition, string text)
    {
        TMP_Text label = EnsureText(parent, name, anchoredPosition, text, 28f, TextAlignmentOptions.TopLeft);
        label.raycastTarget = false;
        return label;
    }

    private static TMP_Text EnsureDynamicHudValue(Transform parent, string name, Vector2 anchoredPosition, string text)
    {
        TMP_Text value = EnsureText(parent, name, anchoredPosition, text, 28f, TextAlignmentOptions.TopLeft);
        value.raycastTarget = false;
        return value;
    }

    private static TMP_Text EnsureCenteredTitle(Transform parent, string name, string text, Vector2 anchoredPosition)
    {
        TMP_Text title = EnsureText(parent, name, anchoredPosition, text, 44f, TextAlignmentOptions.Center);
        RectTransform rect = title.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(520f, 64f);
        title.raycastTarget = false;
        return title;
    }

    private static TMP_Text EnsureText(Transform parent, string name, Vector2 anchoredPosition, string text, float fontSize, TextAlignmentOptions alignment)
    {
        Transform found = parent.Find(name);
        GameObject textObject = found != null ? found.gameObject : new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TMP_Text tmp = textObject.GetComponent<TMP_Text>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(260f, 38f);
        return tmp;
    }

    private static Button EnsureButton(Transform parent, string name, string label, Vector2 anchoredPosition)
    {
        Transform found = parent.Find(name);
        GameObject buttonObject = found != null ? found.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(220f, 56f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.08f, 0.12f, 0.18f, 0.86f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        TMP_Text text = EnsureText(buttonObject.transform, "Label", Vector2.zero, label, 26f, TextAlignmentOptions.Center);
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        text.color = Color.white;
        text.raycastTarget = false;
        return button;
    }

    private static void ReplacePersistentListener(UnityEvent unityEvent, UnityAction action)
    {
        unityEvent.RemoveAllListeners();
        UnityEventTools.AddPersistentListener(unityEvent, action);
    }

    private static void SetObject(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }
}
