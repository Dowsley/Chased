using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Quick setup menu for building a chase city in one click
/// </summary>
public class QuickCitySetup : EditorWindow
{
    private bool useTerrainDetection = true;
    private float blockSize = 40f;
    private float streetWidth = 12f;
    private float terrainCoverage = 0.9f;
    private int copCarCount = 6;

    [MenuItem("Chased/Quick City Setup")]
    static void ShowWindow()
    {
        QuickCitySetup window = (QuickCitySetup)EditorWindow.GetWindow(typeof(QuickCitySetup));
        window.titleContent = new GUIContent("Quick City Setup");
        window.minSize = new Vector2(400, 500);
        window.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.Space(10);

        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
        titleStyle.fontSize = 16;
        titleStyle.alignment = TextAnchor.MiddleCenter;

        EditorGUILayout.LabelField("CHASED - City Builder", titleStyle);
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Quick Setup for Chase Gameplay", EditorStyles.centeredGreyMiniLabel);

        EditorGUILayout.Space(20);

        // City Settings
        EditorGUILayout.LabelField("City Settings", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("City will auto-fit your terrain!", MessageType.Info);

        useTerrainDetection = EditorGUILayout.Toggle("Auto-Detect Terrain", useTerrainDetection);
        terrainCoverage = EditorGUILayout.Slider("Terrain Coverage", terrainCoverage, 0.5f, 1f);
        blockSize = EditorGUILayout.Slider("Block Size", blockSize, 20f, 80f);
        streetWidth = EditorGUILayout.Slider("Street Width", streetWidth, 8f, 20f);

        EditorGUILayout.Space(10);

        // Chase Settings
        EditorGUILayout.LabelField("Chase Settings", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Configure cop cars and pursuit", MessageType.Info);

        copCarCount = EditorGUILayout.IntSlider("Number of Cop Cars", copCarCount, 2, 12);

        EditorGUILayout.Space(20);

        // Build Button
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("BUILD COMPLETE CHASE CITY", GUILayout.Height(50)))
        {
            BuildCompleteCity();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(10);

        // Individual buttons
        EditorGUILayout.LabelField("Or build step-by-step:", EditorStyles.miniBoldLabel);

        if (GUILayout.Button("1. Build City Only", GUILayout.Height(30)))
        {
            BuildCityOnly();
        }

        if (GUILayout.Button("2. Add Vehicles Only", GUILayout.Height(30)))
        {
            AddVehiclesOnly();
        }

        EditorGUILayout.Space(20);

        // Info
        EditorGUILayout.HelpBox(
            "This will create:\n" +
            "• Procedural city fitted to your terrain\n" +
            $"• {blockSize}m building blocks with {streetWidth}m streets\n" +
            "• Automatic road network\n" +
            $"• 1 player car + {copCarCount} cop cars\n" +
            "• Proper camera setup\n" +
            "• Game manager with campaign/betting\n" +
            "• Complete UI (HUD, betting, win/lose screens)\n\n" +
            "Perfect for chase gameplay with permadeath!",
            MessageType.None);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Open Full Documentation"))
        {
            System.Diagnostics.Process.Start("CITY_BUILDER_GUIDE.txt");
        }
    }

    void BuildCompleteCity()
    {
        if (!EditorUtility.DisplayDialog("Build Complete Chase Game",
            "This will create a complete chase game with:\n" +
            "• Procedural city\n" +
            "• Player + cop cars\n" +
            "• Complete UI system\n" +
            "• Campaign/betting system\n\n" +
            "Any existing city/vehicles/UI will be replaced.\n\n" +
            "Continue?",
            "Yes, Build Everything!", "Cancel"))
        {
            return;
        }

        BuildCityOnly();
        SetupGameUI();
        AddVehiclesOnly();

        EditorUtility.DisplayDialog("Success!",
            "Complete chase game built successfully!\n\n" +
            "✓ City generated\n" +
            "✓ UI created\n" +
            "✓ Vehicles spawned\n" +
            "✓ Systems ready\n\n" +
            "Press Play and click 'Start Chase' to begin!",
            "OK");
    }

    void BuildCityOnly()
    {
        // Create or find UrbanCityBuilder
        UrbanCityBuilder builder = FindObjectOfType<UrbanCityBuilder>();
        if (builder == null)
        {
            GameObject builderObj = new GameObject("UrbanCityBuilder");
            builder = builderObj.AddComponent<UrbanCityBuilder>();
            Undo.RegisterCreatedObjectUndo(builderObj, "Create UrbanCityBuilder");
        }

        // Set properties
        SerializedObject so = new SerializedObject(builder);
        so.FindProperty("autoDetectTerrain").boolValue = useTerrainDetection;
        so.FindProperty("terrainCoverage").floatValue = terrainCoverage;
        so.FindProperty("blockSize").floatValue = blockSize;
        so.FindProperty("streetWidth").floatValue = streetWidth;
        so.FindProperty("generateRoads").boolValue = true;
        so.FindProperty("blockDensity").floatValue = 0.85f;
        so.FindProperty("diagonalRoadProbability").floatValue = 0.15f;

        // Load ALL assets into districts
        LoadPrefabsArray(so, "downtownBuildings", "Assets/city_lowpoly/prefabs/Skyscrapers");

        LoadMultipleFoldersArray(so, "midtownBuildings", new string[] {
            "Assets/city_lowpoly/prefabs/NY",
            "Assets/city_lowpoly/prefabs/Phaneron_LP"
        });

        LoadMultipleFoldersArray(so, "residentialBuildings", new string[] {
            "Assets/city_lowpoly/prefabs/Houses",
            "Assets/city_lowpoly/prefabs/Barcelona",
            "Assets/city_lowpoly/prefabs/France",
            "Assets/city_lowpoly/prefabs/Venice",
            "Assets/city_lowpoly/prefabs/Mexico",
            "Assets/city_lowpoly/prefabs/Croatia"
        });

        LoadPrefabsArray(so, "industrialBuildings", "Assets/city_lowpoly/prefabs/Warehouse");
        LoadPrefabsArray(so, "streetProps", "Assets/city_lowpoly/prefabs/Props");

        so.ApplyModifiedProperties();

        // Build the city
        builder.BuildCity();

        EditorUtility.SetDirty(builder);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("Urban city with all assets built!");
    }

    void AddVehiclesOnly()
    {
        // Create or find ChaseSceneSetup
        ChaseSceneSetup setup = FindObjectOfType<ChaseSceneSetup>();
        if (setup == null)
        {
            GameObject setupObj = new GameObject("ChaseSceneSetup");
            setup = setupObj.AddComponent<ChaseSceneSetup>();
            Undo.RegisterCreatedObjectUndo(setupObj, "Create ChaseSceneSetup");
        }

        // Set properties
        SerializedObject so = new SerializedObject(setup);
        so.FindProperty("numberOfCopCars").intValue = copCarCount;

        // Load prefabs
        GameObject playerCar = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlayerCar.prefab");
        GameObject aiCar = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/AICar.prefab");

        if (playerCar != null)
            so.FindProperty("playerCarPrefab").objectReferenceValue = playerCar;

        if (aiCar != null)
            so.FindProperty("aiCarPrefab").objectReferenceValue = aiCar;

        // Link city builder (try all types)
        UrbanCityBuilder urbanCityBuilder = FindObjectOfType<UrbanCityBuilder>();
        TerrainCityBuilder terrainCityBuilder = FindObjectOfType<TerrainCityBuilder>();
        CityBuilder cityBuilder = FindObjectOfType<CityBuilder>();

        // Prefer UrbanCityBuilder (newest)
        if (urbanCityBuilder != null)
        {
            Debug.Log("Linked UrbanCityBuilder to ChaseSceneSetup");
            // UrbanCityBuilder has same methods as TerrainCityBuilder
        }
        else if (terrainCityBuilder != null)
        {
            so.FindProperty("terrainCityBuilder").objectReferenceValue = terrainCityBuilder;
            Debug.Log("Linked TerrainCityBuilder to ChaseSceneSetup");
        }
        else if (cityBuilder != null)
        {
            so.FindProperty("cityBuilder").objectReferenceValue = cityBuilder;
            Debug.Log("Linked CityBuilder to ChaseSceneSetup");
        }

        so.ApplyModifiedProperties();

        // Setup the scene
        setup.SetupScene();

        EditorUtility.SetDirty(setup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"Vehicles spawned: 1 player + {copCarCount} cops!");
    }

    void SetupGameUI()
    {
        Debug.Log("Setting up complete game UI...");

        // Clean up existing UI
        GameObject existingUI = GameObject.Find("GameUI");
        if (existingUI != null)
        {
            Undo.DestroyObjectImmediate(existingUI);
        }

        // Create root UI GameObject
        GameObject uiRoot = new GameObject("GameUI");
        Undo.RegisterCreatedObjectUndo(uiRoot, "Create Game UI");

        // Create Event System if it doesn't exist
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
        }

        // Create all canvases
        Canvas hudCanvas = CreateCanvas(uiRoot.transform, "HUD Canvas", 10, false);
        Canvas bettingCanvas = CreateCanvas(uiRoot.transform, "Betting Canvas", 11, true);
        Canvas winCanvas = CreateCanvas(uiRoot.transform, "Win Canvas", 12, false);
        Canvas gameOverCanvas = CreateCanvas(uiRoot.transform, "GameOver Canvas", 13, false);

        // Create UI elements for each canvas
        var hudElements = CreateHUDElements(hudCanvas.transform);
        var bettingElements = CreateBettingElements(bettingCanvas.transform);
        var winElements = CreateWinElements(winCanvas.transform);
        var gameOverElements = CreateGameOverElements(gameOverCanvas.transform);

        // Create and configure GameUIManager
        var uiManager = uiRoot.AddComponent<UI.GameUIManager>();
        SerializedObject so = new SerializedObject(uiManager);

        // Assign canvas references
        so.FindProperty("hudCanvas").objectReferenceValue = hudCanvas;
        so.FindProperty("bettingCanvas").objectReferenceValue = bettingCanvas;
        so.FindProperty("gameOverCanvas").objectReferenceValue = gameOverCanvas;
        so.FindProperty("winCanvas").objectReferenceValue = winCanvas;

        // Assign HUD elements
        so.FindProperty("moneyText").objectReferenceValue = hudElements["moneyText"];
        so.FindProperty("betText").objectReferenceValue = hudElements["betText"];
        so.FindProperty("timerText").objectReferenceValue = hudElements["timerText"];
        so.FindProperty("healthBar").objectReferenceValue = hudElements["healthBar"];
        so.FindProperty("healthText").objectReferenceValue = hudElements["healthText"];
        so.FindProperty("copHitsText").objectReferenceValue = hudElements["copHitsText"];

        // Assign betting elements
        so.FindProperty("availableMoneyText").objectReferenceValue = bettingElements["availableMoneyText"];
        so.FindProperty("betSlider").objectReferenceValue = bettingElements["betSlider"];
        so.FindProperty("betAmountText").objectReferenceValue = bettingElements["betAmountText"];
        so.FindProperty("potentialWinText").objectReferenceValue = bettingElements["potentialWinText"];
        so.FindProperty("startChaseButton").objectReferenceValue = bettingElements["startChaseButton"];
        so.FindProperty("newCampaignButton").objectReferenceValue = bettingElements["newCampaignButton"];

        // Assign win elements
        so.FindProperty("winBetText").objectReferenceValue = winElements["winBetText"];
        so.FindProperty("winningsText").objectReferenceValue = winElements["winningsText"];
        so.FindProperty("totalMoneyText").objectReferenceValue = winElements["totalMoneyText"];
        so.FindProperty("nextChaseButton").objectReferenceValue = winElements["nextChaseButton"];

        // Assign game over elements
        so.FindProperty("gameOverMessageText").objectReferenceValue = gameOverElements["gameOverMessageText"];
        so.FindProperty("restartCampaignButton").objectReferenceValue = gameOverElements["restartCampaignButton"];

        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(uiManager);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("Complete game UI created successfully!");
    }

    Canvas CreateCanvas(Transform parent, string name, int sortOrder, bool active)
    {
        GameObject canvasObj = new GameObject(name);
        canvasObj.transform.SetParent(parent);
        Undo.RegisterCreatedObjectUndo(canvasObj, $"Create {name}");

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;

        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        canvasObj.SetActive(active);

        return canvas;
    }

    Dictionary<string, Object> CreateHUDElements(Transform parent)
    {
        var elements = new Dictionary<string, Object>();

        // Money Text (top-left)
        GameObject moneyText = CreateTextMeshPro(parent, "MoneyText", new Vector2(-850, 500), new Vector2(200, 50), "$100", 32);
        moneyText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
        elements["moneyText"] = moneyText.GetComponent<TextMeshProUGUI>();

        // Bet Text (top-left, below money)
        GameObject betText = CreateTextMeshPro(parent, "BetText", new Vector2(-850, 450), new Vector2(200, 40), "Bet: $0", 24);
        betText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
        betText.GetComponent<TextMeshProUGUI>().color = Color.yellow;
        elements["betText"] = betText.GetComponent<TextMeshProUGUI>();

        // Timer Text (top-center)
        GameObject timerText = CreateTextMeshPro(parent, "TimerText", new Vector2(0, 500), new Vector2(300, 60), "60.0s", 42);
        timerText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        timerText.GetComponent<TextMeshProUGUI>().color = Color.cyan;
        elements["timerText"] = timerText.GetComponent<TextMeshProUGUI>();

        // Health Bar (bottom-center)
        GameObject healthBarObj = CreateSlider(parent, "HealthBar", new Vector2(0, -450), new Vector2(400, 30));
        Slider healthSlider = healthBarObj.GetComponent<Slider>();
        healthSlider.value = 1f;
        healthSlider.interactable = false;
        // Set health bar fill color to green->red gradient (done via Image component)
        Image fillImage = healthBarObj.transform.Find("Fill Area/Fill").GetComponent<Image>();
        fillImage.color = Color.green;
        elements["healthBar"] = healthSlider;

        // Health Text (on health bar)
        GameObject healthText = CreateTextMeshPro(healthBarObj.transform, "HealthText", Vector2.zero, new Vector2(400, 30), "100%", 20);
        healthText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        elements["healthText"] = healthText.GetComponent<TextMeshProUGUI>();

        // Cop Hits Counter (top-right)
        GameObject copHitsText = CreateTextMeshPro(parent, "CopHitsText", new Vector2(850, 500), new Vector2(250, 50), "Cop Hits: 0/3", 32);
        copHitsText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Right;
        copHitsText.GetComponent<TextMeshProUGUI>().color = Color.white;
        elements["copHitsText"] = copHitsText.GetComponent<TextMeshProUGUI>();

        return elements;
    }

    Dictionary<string, Object> CreateBettingElements(Transform parent)
    {
        var elements = new Dictionary<string, Object>();

        // Title
        GameObject title = CreateTextMeshPro(parent, "Title", new Vector2(0, 350), new Vector2(800, 80), "PLACE YOUR BET", 56);
        title.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        title.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        // Available Money Text
        GameObject availableMoney = CreateTextMeshPro(parent, "AvailableMoneyText", new Vector2(0, 250), new Vector2(400, 50), "Available: $100", 32);
        availableMoney.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        elements["availableMoneyText"] = availableMoney.GetComponent<TextMeshProUGUI>();

        // Bet Slider
        GameObject betSlider = CreateSlider(parent, "BetSlider", new Vector2(0, 150), new Vector2(600, 40));
        Slider slider = betSlider.GetComponent<Slider>();
        slider.minValue = 10f;
        slider.maxValue = 100f;
        slider.value = 20f;
        slider.wholeNumbers = true;
        elements["betSlider"] = slider;

        // Bet Amount Text
        GameObject betAmount = CreateTextMeshPro(parent, "BetAmountText", new Vector2(0, 80), new Vector2(300, 60), "$20", 48);
        betAmount.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        betAmount.GetComponent<TextMeshProUGUI>().color = Color.yellow;
        betAmount.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        elements["betAmountText"] = betAmount.GetComponent<TextMeshProUGUI>();

        // Potential Win Text
        GameObject potentialWin = CreateTextMeshPro(parent, "PotentialWinText", new Vector2(0, 20), new Vector2(500, 40), "Potential Win: $30 - $45", 24);
        potentialWin.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        potentialWin.GetComponent<TextMeshProUGUI>().color = Color.green;
        elements["potentialWinText"] = potentialWin.GetComponent<TextMeshProUGUI>();

        // Start Chase Button
        GameObject startButton = CreateButton(parent, "StartChaseButton", new Vector2(0, -100), new Vector2(400, 80), "START CHASE");
        elements["startChaseButton"] = startButton.GetComponent<Button>();

        // New Campaign Button (hidden by default)
        GameObject newCampaignButton = CreateButton(parent, "NewCampaignButton", new Vector2(0, -200), new Vector2(350, 60), "NEW CAMPAIGN");
        newCampaignButton.GetComponent<Image>().color = new Color(0.3f, 0.7f, 0.3f);
        newCampaignButton.SetActive(false);
        elements["newCampaignButton"] = newCampaignButton.GetComponent<Button>();

        return elements;
    }

    Dictionary<string, Object> CreateWinElements(Transform parent)
    {
        var elements = new Dictionary<string, Object>();

        // Title
        GameObject title = CreateTextMeshPro(parent, "Title", new Vector2(0, 350), new Vector2(800, 100), "CHASE WON!", 64);
        title.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        title.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        title.GetComponent<TextMeshProUGUI>().color = Color.green;

        // Win Bet Text
        GameObject winBet = CreateTextMeshPro(parent, "WinBetText", new Vector2(0, 200), new Vector2(400, 50), "Bet: $20", 36);
        winBet.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        elements["winBetText"] = winBet.GetComponent<TextMeshProUGUI>();

        // Winnings Text
        GameObject winnings = CreateTextMeshPro(parent, "WinningsText", new Vector2(0, 130), new Vector2(500, 60), "Won: $45 (2.25x)", 42);
        winnings.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        winnings.GetComponent<TextMeshProUGUI>().color = Color.yellow;
        winnings.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        elements["winningsText"] = winnings.GetComponent<TextMeshProUGUI>();

        // Total Money Text
        GameObject totalMoney = CreateTextMeshPro(parent, "TotalMoneyText", new Vector2(0, 50), new Vector2(400, 50), "Total: $145", 36);
        totalMoney.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        elements["totalMoneyText"] = totalMoney.GetComponent<TextMeshProUGUI>();

        // Next Chase Button
        GameObject nextButton = CreateButton(parent, "NextChaseButton", new Vector2(0, -100), new Vector2(350, 70), "NEXT CHASE");
        nextButton.GetComponent<Image>().color = new Color(0.2f, 0.8f, 0.2f);
        elements["nextChaseButton"] = nextButton.GetComponent<Button>();

        return elements;
    }

    Dictionary<string, Object> CreateGameOverElements(Transform parent)
    {
        var elements = new Dictionary<string, Object>();

        // Title
        GameObject title = CreateTextMeshPro(parent, "Title", new Vector2(0, 350), new Vector2(900, 100), "CAMPAIGN OVER", 64);
        title.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        title.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        title.GetComponent<TextMeshProUGUI>().color = Color.red;

        // Game Over Message
        GameObject message = CreateTextMeshPro(parent, "GameOverMessageText", new Vector2(0, 100), new Vector2(700, 300),
            "CAMPAIGN OVER\n\nYou're out of money!\n\nStart a new campaign?", 32);
        message.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        elements["gameOverMessageText"] = message.GetComponent<TextMeshProUGUI>();

        // Restart Campaign Button
        GameObject restartButton = CreateButton(parent, "RestartCampaignButton", new Vector2(0, -150), new Vector2(400, 80), "NEW CAMPAIGN");
        restartButton.GetComponent<Image>().color = new Color(0.7f, 0.3f, 0.3f);
        elements["restartCampaignButton"] = restartButton.GetComponent<Button>();

        return elements;
    }

    GameObject CreateTextMeshPro(Transform parent, string name, Vector2 position, Vector2 size, string text, int fontSize)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        RectTransform rectTransform = textObj.AddComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        return textObj;
    }

    GameObject CreateSlider(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject sliderObj = new GameObject(name);
        sliderObj.transform.SetParent(parent, false);

        RectTransform rectTransform = sliderObj.AddComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;

        Slider slider = sliderObj.AddComponent<Slider>();

        // Create Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f);

        // Create Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(5, 5);
        fillAreaRect.offsetMax = new Vector2(-5, -5);

        // Create Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.3f, 0.7f, 1f);

        slider.fillRect = fillRect;
        slider.targetGraphic = fillImage;

        return sliderObj;
    }

    GameObject CreateButton(Transform parent, string name, Vector2 position, Vector2 size, string text)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;

        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.6f, 0.9f);

        Button button = buttonObj.AddComponent<Button>();

        // Create button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 28;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;

        return buttonObj;
    }

    void LoadPrefabsArray(SerializedObject so, string propertyName, string path)
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { path });
        SerializedProperty prop = so.FindProperty(propertyName);

        if (prop == null) return;

        prop.arraySize = guids.Length;
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            prop.GetArrayElementAtIndex(i).objectReferenceValue = prefab;
        }

        Debug.Log($"Loaded {guids.Length} prefabs for {propertyName}");
    }

    void LoadMultipleFoldersArray(SerializedObject so, string propertyName, string[] paths)
    {
        List<GameObject> allPrefabs = new List<GameObject>();

        foreach (string path in paths)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { path });
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab != null)
                {
                    allPrefabs.Add(prefab);
                }
            }
        }

        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop == null) return;

        prop.arraySize = allPrefabs.Count;
        for (int i = 0; i < allPrefabs.Count; i++)
        {
            prop.GetArrayElementAtIndex(i).objectReferenceValue = allPrefabs[i];
        }

        Debug.Log($"Loaded {allPrefabs.Count} prefabs from {paths.Length} folders for {propertyName}");
    }
}
