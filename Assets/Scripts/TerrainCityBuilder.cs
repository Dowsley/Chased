using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Enhanced city builder that auto-detects terrain size and generates roads with EasyRoads3D
/// </summary>
public class TerrainCityBuilder : MonoBehaviour
{
    [Header("Terrain Settings")]
    [SerializeField] private Terrain targetTerrain;
    [SerializeField] private bool autoDetectTerrain = true;
    [SerializeField][Range(0.5f, 1f)] private float terrainCoverage = 0.9f; // How much of terrain to fill

    [Header("City Layout")]
    [SerializeField] private float blockSize = 40f;
    [SerializeField] private float streetWidth = 12f;
    [SerializeField] private float buildingDensity = 0.7f; // 0-1, how many buildings per block

    [Header("Building Prefabs")]
    [SerializeField] private GameObject[] buildingPrefabs;
    [SerializeField] private GameObject[] skyscraperPrefabs;

    [Header("Street Props")]
    [SerializeField] private GameObject[] streetProps;
    [SerializeField] private float propSpacing = 20f;
    [SerializeField] private float propDensity = 0.3f; // Probability of placing a prop

    [Header("EasyRoads3D Integration")]
    [SerializeField] private bool generateRoads = true;
    [SerializeField] private Material roadMaterial;
    [SerializeField] private float roadWidth = 6f;

    private Transform cityParent;
    private int calculatedWidth;
    private int calculatedLength;
    private Vector3 cityOffset;

    public void BuildCity()
    {
        if (!DetectAndCalculate())
        {
            Debug.LogError("Cannot build city - terrain not detected or invalid!");
            return;
        }

        ClearCity();

        cityParent = new GameObject("ProceduralCity").transform;
        cityParent.position = cityOffset;

        if (generateRoads)
        {
            GenerateRoads();
        }

        PlaceBuildings();
        PlaceStreetProps();

        Debug.Log($"Procedural city built: {calculatedWidth}x{calculatedLength} blocks on terrain size {GetTerrainSize()}");
    }

    private bool DetectAndCalculate()
    {
        // Auto-detect terrain if needed
        if (autoDetectTerrain || targetTerrain == null)
        {
            targetTerrain = FindObjectOfType<Terrain>();
            if (targetTerrain == null)
            {
                Debug.LogWarning("No terrain found in scene! Will build city at origin.");
                calculatedWidth = 8;
                calculatedLength = 8;
                cityOffset = Vector3.zero;
                return true;
            }
        }

        // Get terrain size
        Vector3 terrainSize = targetTerrain.terrainData.size;
        Vector3 terrainPos = targetTerrain.transform.position;

        Debug.Log($"Detected terrain: {terrainSize.x}x{terrainSize.z} at {terrainPos}");

        // Calculate how many blocks fit in the terrain
        float usableWidth = terrainSize.x * terrainCoverage;
        float usableLength = terrainSize.z * terrainCoverage;

        float cellSize = blockSize + streetWidth;
        calculatedWidth = Mathf.Max(1, Mathf.FloorToInt(usableWidth / cellSize));
        calculatedLength = Mathf.Max(1, Mathf.FloorToInt(usableLength / cellSize));

        // Center the city on the terrain
        float cityWidth = calculatedWidth * cellSize;
        float cityLength = calculatedLength * cellSize;
        cityOffset = new Vector3(
            terrainPos.x + (terrainSize.x - cityWidth) / 2f,
            terrainPos.y,
            terrainPos.z + (terrainSize.z - cityLength) / 2f
        );

        return true;
    }

    private void ClearCity()
    {
        GameObject existing = GameObject.Find("ProceduralCity");
        if (existing != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(existing);
#else
            Destroy(existing);
#endif
        }

        // Also clear any EasyRoads3D road network we created
        ClearEasyRoads();
    }

    private void GenerateRoads()
    {
#if UNITY_EDITOR
        // Try to find or create EasyRoads3D Road Network
        GameObject roadNetwork = GameObject.Find("Road Network");

        if (roadNetwork == null)
        {
            // Create via GameObject menu path
            roadNetwork = CreateRoadNetwork();
            if (roadNetwork == null)
            {
                Debug.LogWarning("Could not create EasyRoads3D Road Network. Make sure EasyRoads3D is properly installed.");
                return;
            }
        }

        // Generate grid of roads
        Transform roadsParent = new GameObject("GeneratedRoads").transform;
        roadsParent.parent = cityParent;

        float cellSize = blockSize + streetWidth;

        // Create vertical streets (going north-south)
        for (int x = 0; x <= calculatedWidth; x++)
        {
            CreateStreetLine(
                new Vector3(x * cellSize, 0, 0),
                new Vector3(x * cellSize, 0, calculatedLength * cellSize),
                roadsParent,
                $"Street_V_{x}"
            );
        }

        // Create horizontal streets (going east-west)
        for (int z = 0; z <= calculatedLength; z++)
        {
            CreateStreetLine(
                new Vector3(0, 0, z * cellSize),
                new Vector3(calculatedWidth * cellSize, 0, z * cellSize),
                roadsParent,
                $"Street_H_{z}"
            );
        }

        Debug.Log($"Generated {calculatedWidth + calculatedLength + 2} roads");
#endif
    }

    private GameObject CreateRoadNetwork()
    {
        // EasyRoads3D free version - try to create road network via menu command
        // This uses UnityEditor menu items
#if UNITY_EDITOR
        try
        {
            EditorApplication.ExecuteMenuItem("GameObject/3D Object/EasyRoads3D/New Road Network");
            GameObject roadNetwork = GameObject.Find("Road Network");
            if (roadNetwork != null)
            {
                Debug.Log("Created EasyRoads3D Road Network");
                return roadNetwork;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Could not create Road Network via menu: {e.Message}");
        }

        // Alternative: Look for the prefab and instantiate it
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EasyRoads3D/Resources/ER Road Network.prefab");
        if (prefab != null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "Road Network";
            return instance;
        }
#endif
        return null;
    }

    private void CreateStreetLine(Vector3 start, Vector3 end, Transform parent, string name)
    {
        // Create simple plane for roads (fallback if EasyRoads3D doesn't work)
        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = name;
        road.transform.parent = parent;

        // Position and scale to create a road
        Vector3 midpoint = (start + end) / 2f;
        float length = Vector3.Distance(start, end);

        road.transform.localPosition = midpoint;
        road.transform.localScale = new Vector3(streetWidth, 0.1f, length);
        road.transform.localRotation = Quaternion.LookRotation(end - start);

        // Apply road material
        if (roadMaterial != null)
        {
            road.GetComponent<Renderer>().material = roadMaterial;
        }
        else
        {
            // Default dark gray material for roads
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.2f, 0.2f, 0.2f);
            road.GetComponent<Renderer>().material = mat;
        }

        // Ensure road has collider for driving
        if (road.GetComponent<Collider>() == null)
        {
            road.AddComponent<BoxCollider>();
        }
    }

    private void ClearEasyRoads()
    {
#if UNITY_EDITOR
        GameObject[] roads = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject obj in roads)
        {
            if (obj.name.Contains("GeneratedRoads") || obj.name.StartsWith("Street_"))
            {
                DestroyImmediate(obj);
            }
        }
#endif
    }

    private void PlaceBuildings()
    {
        if (buildingPrefabs == null || buildingPrefabs.Length == 0)
        {
            Debug.LogWarning("No building prefabs assigned!");
            return;
        }

        Transform buildingsParent = new GameObject("Buildings").transform;
        buildingsParent.parent = cityParent;

        float cellSize = blockSize + streetWidth;

        for (int x = 0; x < calculatedWidth; x++)
        {
            for (int z = 0; z < calculatedLength; z++)
            {
                // Random chance to place building based on density
                if (Random.value > buildingDensity) continue;

                Vector3 blockCenter = new Vector3(
                    x * cellSize + blockSize / 2f + streetWidth / 2f,
                    0,
                    z * cellSize + blockSize / 2f + streetWidth / 2f
                );

                // Sample terrain height if terrain exists
                if (targetTerrain != null)
                {
                    Vector3 worldPos = cityOffset + blockCenter;
                    blockCenter.y = targetTerrain.SampleHeight(worldPos);
                }

                // Place 1-3 buildings per block
                int buildingCount = Random.Range(1, 4);
                for (int i = 0; i < buildingCount; i++)
                {
                    GameObject prefab = GetRandomBuilding(x, z);
                    if (prefab == null) continue;

                    Vector3 offset = new Vector3(
                        Random.Range(-blockSize * 0.35f, blockSize * 0.35f),
                        0,
                        Random.Range(-blockSize * 0.35f, blockSize * 0.35f)
                    );

                    Vector3 position = blockCenter + offset;
                    Quaternion rotation = Quaternion.Euler(0, Random.Range(0, 4) * 90f, 0);

#if UNITY_EDITOR
                    GameObject building = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    building.transform.localPosition = position;
                    building.transform.localRotation = rotation;
                    building.transform.parent = buildingsParent;
#else
                    GameObject building = Instantiate(prefab, position, rotation, buildingsParent);
#endif
                    building.name = $"Building_{x}_{z}_{i}";

                    // Ensure colliders for buildings
                    EnsureColliders(building);
                }
            }
        }

        Debug.Log($"Placed buildings in {calculatedWidth * calculatedLength} blocks");
    }

    private GameObject GetRandomBuilding(int x, int z)
    {
        // Use skyscrapers in downtown area (center)
        int centerX = calculatedWidth / 2;
        int centerZ = calculatedLength / 2;
        float distFromCenter = Vector2.Distance(new Vector2(x, z), new Vector2(centerX, centerZ));
        float maxDist = Mathf.Max(calculatedWidth, calculatedLength) / 2f;

        bool useSkyscraper = (distFromCenter / maxDist) < 0.3f &&
                            skyscraperPrefabs != null &&
                            skyscraperPrefabs.Length > 0;

        if (useSkyscraper && Random.value > 0.4f)
        {
            return skyscraperPrefabs[Random.Range(0, skyscraperPrefabs.Length)];
        }
        else if (buildingPrefabs != null && buildingPrefabs.Length > 0)
        {
            return buildingPrefabs[Random.Range(0, buildingPrefabs.Length)];
        }

        return null;
    }

    private void EnsureColliders(GameObject building)
    {
        if (building.GetComponentInChildren<Collider>() == null)
        {
            MeshFilter[] meshes = building.GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter mf in meshes)
            {
                if (mf.gameObject.GetComponent<Collider>() == null)
                {
                    MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.convex = false;
                }
            }
        }
    }

    private void PlaceStreetProps()
    {
        if (streetProps == null || streetProps.Length == 0) return;

        Transform propsParent = new GameObject("StreetProps").transform;
        propsParent.parent = cityParent;

        float cellSize = blockSize + streetWidth;

        // Place props along streets
        for (int x = 0; x <= calculatedWidth; x++)
        {
            for (float z = 0; z < calculatedLength * cellSize; z += propSpacing)
            {
                if (Random.value > propDensity) continue;

                Vector3 position = new Vector3(
                    x * cellSize + Random.Range(-streetWidth * 0.4f, streetWidth * 0.4f),
                    0,
                    z
                );

                // Sample terrain height
                if (targetTerrain != null)
                {
                    Vector3 worldPos = cityOffset + position;
                    position.y = targetTerrain.SampleHeight(worldPos);
                }

                PlaceProp(position, propsParent);
            }
        }

        // Place props along horizontal streets
        for (int z = 0; z <= calculatedLength; z++)
        {
            for (float x = 0; x < calculatedWidth * cellSize; x += propSpacing)
            {
                if (Random.value > propDensity) continue;

                Vector3 position = new Vector3(
                    x,
                    0,
                    z * cellSize + Random.Range(-streetWidth * 0.4f, streetWidth * 0.4f)
                );

                if (targetTerrain != null)
                {
                    Vector3 worldPos = cityOffset + position;
                    position.y = targetTerrain.SampleHeight(worldPos);
                }

                PlaceProp(position, propsParent);
            }
        }
    }

    private void PlaceProp(Vector3 position, Transform parent)
    {
        GameObject prefab = streetProps[Random.Range(0, streetProps.Length)];
        Quaternion rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

#if UNITY_EDITOR
        GameObject prop = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        prop.transform.localPosition = position;
        prop.transform.localRotation = rotation;
        prop.transform.parent = parent;
#else
        GameObject prop = Instantiate(prefab, position, rotation, parent);
#endif
    }

    public Vector3 GetCityCenter()
    {
        float cellSize = blockSize + streetWidth;
        float totalWidth = calculatedWidth * cellSize;
        float totalLength = calculatedLength * cellSize;
        return cityOffset + new Vector3(totalWidth / 2f, 0, totalLength / 2f);
    }

    public Vector3 GetRandomStreetPosition()
    {
        float cellSize = blockSize + streetWidth;
        bool isVerticalStreet = Random.value > 0.5f;

        Vector3 position;

        if (isVerticalStreet)
        {
            // Vertical street: X is on a street line, Z can be anywhere along it
            int streetIndex = Random.Range(0, calculatedWidth + 1);

            // Street is at streetIndex * cellSize - ensure we're IN the street, not building
            // Streets are streetWidth wide, so stay within that
            float streetCenterX = streetIndex * cellSize;
            float xOffset = Random.Range(-streetWidth * 0.3f, streetWidth * 0.3f);

            float z = Random.Range(0, calculatedLength * cellSize);

            position = new Vector3(streetCenterX + xOffset, 0, z);
        }
        else
        {
            // Horizontal street: Z is on a street line, X can be anywhere along it
            int streetIndex = Random.Range(0, calculatedLength + 1);

            // Street is at streetIndex * cellSize
            float streetCenterZ = streetIndex * cellSize;
            float zOffset = Random.Range(-streetWidth * 0.3f, streetWidth * 0.3f);

            float x = Random.Range(0, calculatedWidth * cellSize);

            position = new Vector3(x, 0, streetCenterZ + zOffset);
        }

        position += cityOffset;

        // Sample terrain height
        if (targetTerrain != null)
        {
            position.y = targetTerrain.SampleHeight(position) + 1f; // +1 to spawn slightly above ground
        }
        else
        {
            position.y = 1f; // Default height if no terrain
        }

        return position;
    }

    private Vector3 GetTerrainSize()
    {
        if (targetTerrain != null)
        {
            return targetTerrain.terrainData.size;
        }
        return Vector3.zero;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TerrainCityBuilder))]
public class TerrainCityBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TerrainCityBuilder builder = (TerrainCityBuilder)target;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "TERRAIN-ADAPTIVE CITY BUILDER\n\n" +
            "✓ Auto-detects terrain size\n" +
            "✓ Fills terrain with procedural city\n" +
            "✓ Generates roads automatically\n" +
            "✓ Perfect for chase gameplay!\n\n" +
            "Just click 'Build City' - it handles the rest!",
            MessageType.Info);

        EditorGUILayout.Space();

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("BUILD TERRAIN CITY", GUILayout.Height(50)))
        {
            builder.BuildCity();
            EditorUtility.SetDirty(builder);
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space();

        if (GUILayout.Button("Load Building Prefabs"))
        {
            LoadPrefabs(builder, "Assets/city_lowpoly/prefabs/NY", "buildingPrefabs");
        }

        if (GUILayout.Button("Load Skyscraper Prefabs"))
        {
            LoadPrefabs(builder, "Assets/city_lowpoly/prefabs/Skyscrapers", "skyscraperPrefabs");
        }

        if (GUILayout.Button("Load Street Props"))
        {
            LoadPrefabs(builder, "Assets/city_lowpoly/prefabs/Props", "streetProps");
        }
    }

    private void LoadPrefabs(TerrainCityBuilder builder, string path, string fieldName)
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { path });
        GameObject[] prefabs = new GameObject[guids.Length];

        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        }

        SerializedObject so = new SerializedObject(builder);
        SerializedProperty prop = so.FindProperty(fieldName);

        prop.arraySize = prefabs.Length;
        for (int i = 0; i < prefabs.Length; i++)
        {
            prop.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];
        }

        so.ApplyModifiedProperties();

        Debug.Log($"Loaded {prefabs.Length} prefabs into {fieldName}");
    }
}
#endif
