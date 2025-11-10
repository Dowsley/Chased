using UnityEngine;
using Core;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Sets up the chase game scene with player and AI cop cars positioned in the city
/// </summary>
public class ChaseSceneSetup : MonoBehaviour
{
    [Header("Vehicle Prefabs")]
    [SerializeField] private GameObject playerCarPrefab;
    [SerializeField] private GameObject aiCarPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private int numberOfCopCars = 6;
    [SerializeField] private float spawnRadius = 80f;
    [SerializeField] private Vector3 playerSpawnPosition = new Vector3(200, 1, 200);

    [Header("References")]
    [SerializeField] private CityBuilder cityBuilder;
    [SerializeField] private TerrainCityBuilder terrainCityBuilder;
    [SerializeField] private UrbanCityBuilder urbanCityBuilder;

    private GameObject playerCar;
    private GameObject[] copCars;

    public void SetupScene()
    {
        ClearVehicles();

        // CRITICAL: Get a STREET position, not just center (which might be in a building)
        if (urbanCityBuilder != null)
        {
            playerSpawnPosition = urbanCityBuilder.GetRandomStreetPosition();
            Debug.Log($"Player spawn on street via UrbanCityBuilder: {playerSpawnPosition}");
        }
        else if (terrainCityBuilder != null)
        {
            playerSpawnPosition = terrainCityBuilder.GetRandomStreetPosition();
            Debug.Log($"Player spawn on street via TerrainCityBuilder: {playerSpawnPosition}");
        }
        else if (cityBuilder != null)
        {
            playerSpawnPosition = cityBuilder.GetRandomStreetPosition();
            playerSpawnPosition.y = 1f;
            Debug.Log($"Player spawn on street via CityBuilder: {playerSpawnPosition}");
        }

        SpawnPlayerCar();
        SpawnCopCars();
        SetupCamera();
        SetupGameManager();
        SetupVehicleHealth(); // Add health component
        // Note: Chase will be started from UI when player clicks "START CHASE"

        Debug.Log($"Chase scene setup complete! Player car + {numberOfCopCars} cop cars spawned.");
    }

    private void ClearVehicles()
    {
        // Clear existing vehicles
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in players)
        {
#if UNITY_EDITOR
            DestroyImmediate(p);
#else
            Destroy(p);
#endif
        }

        GameObject[] cops = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject cop in cops)
        {
            if (cop.name.Contains("AICar") || cop.name.Contains("PlayerCar"))
            {
#if UNITY_EDITOR
                DestroyImmediate(cop);
#else
                Destroy(cop);
#endif
            }
        }
    }

    private void SpawnPlayerCar()
    {
        if (playerCarPrefab == null)
        {
            Debug.LogError("Player car prefab not assigned!");
            return;
        }

#if UNITY_EDITOR
        playerCar = (GameObject)PrefabUtility.InstantiatePrefab(playerCarPrefab);
#else
        playerCar = Instantiate(playerCarPrefab);
#endif

        playerCar.name = "PlayerCar";
        playerCar.transform.position = playerSpawnPosition;
        playerCar.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

        Debug.Log($"Player car spawned at {playerSpawnPosition}");
    }

    private void SpawnCopCars()
    {
        if (aiCarPrefab == null)
        {
            Debug.LogError("AI car prefab not assigned!");
            return;
        }

        copCars = new GameObject[numberOfCopCars];

        for (int i = 0; i < numberOfCopCars; i++)
        {
            Vector3 spawnPos = GetCopSpawnPosition(i);

#if UNITY_EDITOR
            copCars[i] = (GameObject)PrefabUtility.InstantiatePrefab(aiCarPrefab);
#else
            copCars[i] = Instantiate(aiCarPrefab);
#endif

            copCars[i].name = $"CopCar_{i + 1}";
            copCars[i].transform.position = spawnPos;

            // Face toward player
            Vector3 directionToPlayer = playerSpawnPosition - spawnPos;
            copCars[i].transform.rotation = Quaternion.LookRotation(directionToPlayer);

            Debug.Log($"Cop car {i + 1} spawned at {spawnPos}");
        }
    }

    private Vector3 GetCopSpawnPosition(int index)
    {
        // Spawn cops in a circle around the player for chase gameplay
        float angle = (360f / numberOfCopCars) * index;
        float rad = angle * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            Mathf.Cos(rad) * spawnRadius,
            1f,
            Mathf.Sin(rad) * spawnRadius
        );

        Vector3 position = playerSpawnPosition + offset;

        // If city builder exists, try to snap to street
        if (urbanCityBuilder != null)
        {
            position = urbanCityBuilder.GetRandomStreetPosition();
        }
        else if (terrainCityBuilder != null)
        {
            position = terrainCityBuilder.GetRandomStreetPosition();
        }
        else if (cityBuilder != null)
        {
            position = cityBuilder.GetRandomStreetPosition();
            position.y = 1f;
        }

        return position;
    }

    private void SetupCamera()
    {
        if (playerCar == null)
        {
            Debug.LogError("Cannot setup camera - player car is null!");
            return;
        }

        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            mainCam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            camObj.AddComponent<AudioListener>();
        }

        // Add chase camera component if it doesn't exist
        var chaseCamera = mainCam.GetComponent<Driving.Player.ChaseCamera>();
        if (chaseCamera == null)
        {
            chaseCamera = mainCam.gameObject.AddComponent<Driving.Player.ChaseCamera>();
        }

        // CRITICAL: Link the camera to the player car!
        if (chaseCamera != null)
        {
            SerializedObject so = new SerializedObject(chaseCamera);
            so.FindProperty("target").objectReferenceValue = playerCar.transform;
            so.ApplyModifiedProperties();
            Debug.Log($"Camera linked to player car: {playerCar.name}");
        }

        Debug.Log("Camera setup complete");
    }

    private void SetupGameManager()
    {
        if (playerCar == null)
        {
            Debug.LogError("Cannot setup GameManager - player car is null!");
            return;
        }

        // Find or create game manager
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm == null)
        {
            GameObject gmObj = new GameObject("GameManager");
            gm = gmObj.AddComponent<GameManager>();
        }

        // CRITICAL: Set the target car so cops can chase!
        gm.targetCar = playerCar.transform;
        Debug.Log($"GameManager.targetCar set to: {playerCar.name}");

        Debug.Log("Game manager setup complete");
    }

    private void SetupVehicleHealth()
    {
        if (playerCar == null)
        {
            Debug.LogError("Cannot setup VehicleHealth - player car is null!");
            return;
        }

        // Add VehicleHealth component if it doesn't exist
        var vehicleHealth = playerCar.GetComponent<Driving.VehicleHealth>();
        if (vehicleHealth == null)
        {
            vehicleHealth = playerCar.AddComponent<Driving.VehicleHealth>();
            Debug.Log("VehicleHealth component added to player car");
        }

        Debug.Log("Vehicle health setup complete");
    }

    public void StartChase()
    {
        // Find player car if reference is lost (happens when entering play mode)
        if (playerCar == null)
        {
            playerCar = GameObject.Find("PlayerCar");

            if (playerCar == null)
            {
                Debug.LogError("Cannot start chase - PlayerCar GameObject not found in scene!");
                return;
            }

            Debug.Log("Found PlayerCar in scene at runtime");
        }

        // Get VehicleHealth component
        var vehicleHealth = playerCar.GetComponent<Driving.VehicleHealth>();
        if (vehicleHealth == null)
        {
            Debug.LogError("VehicleHealth component not found on player car!");
            return;
        }

        // Get GameManager
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("GameManager instance not found!");
            return;
        }

        // Start the chase (this will fail if no bet has been placed)
        gm.StartChase(vehicleHealth);

        // Show HUD
        var uiManager = FindObjectOfType<UI.GameUIManager>();
        if (uiManager != null)
        {
            uiManager.ShowHUD(vehicleHealth);
        }

        Debug.Log("Chase started!");
    }

    public GameObject GetPlayerCar()
    {
        return playerCar;
    }

    public GameObject[] GetCopCars()
    {
        return copCars;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ChaseSceneSetup))]
public class ChaseSceneSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ChaseSceneSetup setup = (ChaseSceneSetup)target;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Chase Scene Setup\n\n" +
            "This script sets up the player car and cop cars in the scene.\n\n" +
            "1. Assign Player Car Prefab (Assets/Prefabs/PlayerCar)\n" +
            "2. Assign AI Car Prefab (Assets/Prefabs/AICar)\n" +
            "3. Set number of cop cars\n" +
            "4. Click 'Setup Scene' to spawn vehicles",
            MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("Setup Chase Scene", GUILayout.Height(40)))
        {
            setup.SetupScene();
            EditorUtility.SetDirty(setup);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Auto-Load Prefabs"))
        {
            LoadVehiclePrefabs(setup);
        }
    }

    private void LoadVehiclePrefabs(ChaseSceneSetup setup)
    {
        SerializedObject so = new SerializedObject(setup);

        // Load player car
        GameObject playerCar = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlayerCar.prefab");
        if (playerCar != null)
        {
            so.FindProperty("playerCarPrefab").objectReferenceValue = playerCar;
            Debug.Log("Loaded PlayerCar prefab");
        }

        // Load AI car
        GameObject aiCar = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/AICar.prefab");
        if (aiCar != null)
        {
            so.FindProperty("aiCarPrefab").objectReferenceValue = aiCar;
            Debug.Log("Loaded AICar prefab");
        }

        // Try to find CityBuilder in scene
        CityBuilder cityBuilder = FindObjectOfType<CityBuilder>();
        if (cityBuilder != null)
        {
            so.FindProperty("cityBuilder").objectReferenceValue = cityBuilder;
            Debug.Log("Found and linked CityBuilder");
        }

        so.ApplyModifiedProperties();
    }
}
#endif
