using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MainSceneArchitectureConfigurator
{
    private const string ScenePath = "Assets/Scenes/MainScene.unity";
    private const string GeneratedFolder = "Assets/Generated";
    private const string PrefabFolder = "Assets/Generated/Prefabs";
    private const string ConfigPath = "Assets/Generated/MainGameConfig.asset";

    public static void Configure()
    {
        EnsureFolder("Assets", "Generated");
        EnsureFolder(GeneratedFolder, "Prefabs");

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        CustomUpdateManager updateManager = Object.FindFirstObjectByType<CustomUpdateManager>();
        if (updateManager == null)
        {
            GameObject updateObject = new GameObject("CustomUpdateManager");
            updateManager = updateObject.AddComponent<CustomUpdateManager>();
        }

        GameObject bootstrapObject = GameObject.Find("GameManager") ?? GameObject.Find("GameBootstrap") ?? new GameObject("GameBootstrap");
        bootstrapObject.name = "GameBootstrap";
        RemoveOldMonoBehavioursExcept<GameBootstrap>(bootstrapObject);
        GameBootstrap bootstrap = bootstrapObject.GetComponent<GameBootstrap>();
        if (bootstrap == null)
        {
            bootstrap = bootstrapObject.AddComponent<GameBootstrap>();
        }

        GameObject player = GameObject.Find("Player") ?? GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 0.5f, 0f);

        GameObject spawner = GameObject.Find("Spawner") ?? new GameObject("Spawner");
        spawner.transform.position = new Vector3(0f, 0.5f, 18f);

        GameObject buffSpawner = GameObject.Find("BuffWallSpawner") ?? new GameObject("BuffWallSpawner");
        buffSpawner.transform.position = new Vector3(-3f, 0.5f, 18f);

        GameObject playerLine = GameObject.Find("PlayerLine") ?? new GameObject("PlayerLine");
        playerLine.transform.position = new Vector3(0f, 0f, -2f);

        GameObject poolRoot = GameObject.Find("PoolRoot") ?? new GameObject("PoolRoot");

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        GameUIView view = canvas.GetComponent<GameUIView>();
        if (view == null)
        {
            view = canvas.gameObject.AddComponent<GameUIView>();
        }

        ConfigureBasicUi(canvas.transform, view);

        GameObject enemyPrefab = CreatePrefab("Enemy_Normal", PrimitiveType.Capsule, Color.red, new Vector3(1f, 1f, 1f));
        GameObject elitePrefab = CreatePrefab("Enemy_Elite", PrimitiveType.Capsule, new Color(0.65f, 0f, 0.95f), new Vector3(1.25f, 1.25f, 1.25f));
        GameObject bossPrefab = CreatePrefab("Enemy_Boss", PrimitiveType.Capsule, Color.black, new Vector3(1.8f, 1.8f, 1.8f));
        GameObject megazordPrefab = CreatePrefab("Enemy_Megazord", PrimitiveType.Capsule, Color.green, new Vector3(10.0f, 10.0f, 10.0f));
        GameObject damageWallPrefab = CreatePrefab("BuffWall_Damage", PrimitiveType.Cube, Color.yellow, new Vector3(1.4f, 1.4f, 0.4f));
        GameObject projectileWallPrefab = CreatePrefab("BuffWall_Projectiles", PrimitiveType.Cube, Color.cyan, new Vector3(1.4f, 1.4f, 0.4f));
        GameObject projectilePrefab = CreatePrefab("Projectile", PrimitiveType.Sphere, Color.white, new Vector3(0.35f, 0.35f, 0.35f));
        GameObject vfxPrefab = CreatePrefab("ImpactVfx", PrimitiveType.Sphere, Color.magenta, new Vector3(0.5f, 0.5f, 0.5f));

        GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<GameConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
        }

        config.player = new PlayerConfig
        {
            moveSpeed = 8f,
            minHorizontal = -6f,
            maxHorizontal = 6f,
            baseDamage = 1,
            baseProjectileCount = 1,
            fireRate = 0.2f
        };
        config.projectile = new ProjectileConfig { speed = 16f, lifetime = 4f, hitRadius = 0.45f };
        config.lanes = new LaneConfig
        {
            movementAxis = LaneAxis.Z,
            enemyMoveDirection = -1f,
            buffWallMoveDirection = -1f,
            projectileMoveDirection = 1f,
            projectileSpread = 0.45f
        };
        config.pools = new PoolConfig
        {
            entries = new[]
            {
                new PoolEntry { id = PoolId.EnemyNormal, prefab = enemyPrefab, prewarmCount = 16 },
                new PoolEntry { id = PoolId.EnemyElite, prefab = elitePrefab, prewarmCount = 8 },
                new PoolEntry { id = PoolId.EnemyBoss, prefab = bossPrefab, prewarmCount = 2 },
                new PoolEntry { id = PoolId.MegazordBoss, prefab = megazordPrefab, prewarmCount = 1 },
                new PoolEntry { id = PoolId.BuffWallDamage, prefab = damageWallPrefab, prewarmCount = 6 },
                new PoolEntry { id = PoolId.BuffWallProjectileCount, prefab = projectileWallPrefab, prewarmCount = 6 },
                new PoolEntry { id = PoolId.Projectile, prefab = projectilePrefab, prewarmCount = 80 },
                new PoolEntry { id = PoolId.ImpactVfx, prefab = vfxPrefab, prewarmCount = 12 },
            }
        };
        config.waves = new[]
        {
            new WaveConfig
            {
                delayBeforeWave = 0.5f,
                enemies = new[] { new EnemySpawnData { type = EnemyType.Elite, count = 3, interval = 1.2f, health = 3, speed = 3f } },
                buffWalls = new[] { new BuffWallSpawnData { type = BuffType.Damage, count = 2, interval = 2f, health = 2, speed = 2.5f, value = 1 } },
                boss = new BossSpawnData { enabled = false },
                megazord = new MegazordSpawnData { enabled = false }
            },
            new WaveConfig
            {
                delayBeforeWave = 1f,
                enemies = new[]
                {
                    new EnemySpawnData { type = EnemyType.Normal, count = 6, interval = 0.8f, health = 2, speed = 3.5f },
                    new EnemySpawnData { type = EnemyType.Elite, count = 2, interval = 1.4f, health = 4, speed = 2.8f }
                },
                buffWalls = new[]
                {
                    new BuffWallSpawnData { type = BuffType.ProjectileCount, count = 2, interval = 2.5f, health = 2, speed = 2.5f, value = 1 },
                    new BuffWallSpawnData { type = BuffType.Damage, count = 1, interval = 3f, health = 3, speed = 2.5f, value = 1 }
                },
                boss = new BossSpawnData { enabled = true, delay = 5f, health = 10, speed = 2.2f },
                megazord = new MegazordSpawnData { enabled = true, delay = 5f, health = 1000, speed = 1.0f }
            }
        };
        EditorUtility.SetDirty(config);

        SerializedObject serializedBootstrap = new SerializedObject(bootstrap);
        SetObject(serializedBootstrap, "updateManager", updateManager);
        SetObject(serializedBootstrap, "gameUIView", view);
        SetObject(serializedBootstrap, "gameConfig", config);
        SetObject(serializedBootstrap, "playerTransform", player.transform);
        SetObject(serializedBootstrap, "enemySpawnPoint", spawner.transform);
        SetObject(serializedBootstrap, "buffWallSpawnPoint", buffSpawner.transform);
        SetObject(serializedBootstrap, "playerLine", playerLine.transform);
        SetObject(serializedBootstrap, "poolRoot", poolRoot.transform);
        serializedBootstrap.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(bootstrap);

        GameObject oldEnemy = GameObject.Find("Enemy");
        if (oldEnemy != null && oldEnemy.scene.IsValid())
        {
            oldEnemy.SetActive(false);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void ConfigureBasicUi(Transform canvasTransform, GameUIView view)
    {
        GameObject mainMenu = EnsurePanel(canvasTransform, "MainMenuPanel");
        GameObject hud = EnsurePanel(canvasTransform, "HUDPanel");
        GameObject pause = EnsurePanel(canvasTransform, "PausePanel");
        GameObject victory = EnsurePanel(canvasTransform, "VictoryPanel");
        GameObject defeat = EnsurePanel(canvasTransform, "DefeatPanel");

        TMP_Text wave = EnsureText(hud.transform, "WaveText", new Vector2(20f, -20f), "Wave 0/0");
        TMP_Text enemies = EnsureText(hud.transform, "EnemiesText", new Vector2(20f, -55f), "Enemies 0");
        TMP_Text buff = EnsureText(hud.transform, "BuffText", new Vector2(20f, -90f), "Damage +0");
        TMP_Text projectiles = EnsureText(hud.transform, "ProjectileText", new Vector2(20f, -125f), "Projectiles 1");
        EnsureText(mainMenu.transform, "TitleText", new Vector2(0f, 90f), "Optimization Shooter");
        EnsureText(pause.transform, "PauseText", new Vector2(0f, 40f), "Paused");
        EnsureText(victory.transform, "VictoryText", new Vector2(0f, 40f), "Victory");
        EnsureText(defeat.transform, "DefeatText", new Vector2(0f, 40f), "Defeat");

        SerializedObject serializedView = new SerializedObject(view);
        SetObject(serializedView, "mainMenuPanel", mainMenu);
        SetObject(serializedView, "hudPanel", hud);
        SetObject(serializedView, "pausePanel", pause);
        SetObject(serializedView, "victoryPanel", victory);
        SetObject(serializedView, "defeatPanel", defeat);
        SetObject(serializedView, "waveText", wave);
        SetObject(serializedView, "enemiesText", enemies);
        SetObject(serializedView, "buffText", buff);
        SetObject(serializedView, "projectileText", projectiles);
        serializedView.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(view);
    }

    private static GameObject EnsurePanel(Transform parent, string name)
    {
        Transform found = parent.Find(name);
        GameObject panel = found != null ? found.gameObject : new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return panel;
    }

    private static TMP_Text EnsureText(Transform parent, string name, Vector2 anchoredPosition, string text)
    {
        Transform found = parent.Find(name);
        GameObject textObject = found != null ? found.gameObject : new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TMP_Text tmp = textObject.GetComponent<TMP_Text>();
        tmp.text = text;
        tmp.fontSize = 28f;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(420f, 40f);
        return tmp;
    }

    private static GameObject CreatePrefab(string name, PrimitiveType primitiveType, Color color, Vector3 scale)
    {
        string path = $"{PrefabFolder}/{name}.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
        {
            return existing;
        }

        GameObject temp = GameObject.CreatePrimitive(primitiveType);
        temp.name = name;
        temp.transform.localScale = scale;
        Collider collider = temp.GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }

        Renderer renderer = temp.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = color;
            string materialPath = $"{GeneratedFolder}/{name}.mat";
            AssetDatabase.CreateAsset(material, materialPath);
            renderer.sharedMaterial = material;
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
        Object.DestroyImmediate(temp);
        return prefab;
    }

    private static void RemoveOldMonoBehavioursExcept<TKeep>(GameObject target) where TKeep : MonoBehaviour
    {
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(target);
        MonoBehaviour[] components = target.GetComponents<MonoBehaviour>();
        for (int i = components.Length - 1; i >= 0; i--)
        {
            if (components[i] != null && components[i] is not TKeep)
            {
                Object.DestroyImmediate(components[i], true);
            }
        }
    }

    private static void SetObject(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
