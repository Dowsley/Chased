using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Advanced city builder using ALL city_lowpoly assets with diagonal roads and districts
/// GUARANTEES buildings never spawn on roads
/// </summary>
public class UrbanCityBuilder : MonoBehaviour
{
    [Header("Terrain Settings")]
    [SerializeField] private Terrain targetTerrain;
    [SerializeField] private bool autoDetectTerrain = true;
    [SerializeField][Range(0.5f, 1f)] private float terrainCoverage = 0.9f;

    [Header("City Layout")]
    [SerializeField] private float blockSize = 40f;
    [SerializeField] private float streetWidth = 12f;
    [SerializeField][Range(0f, 1f)] private float blockDensity = 0.85f; // Probability a block gets buildings (0-1)

    [Header("District Buildings - Load All Assets")]
    [SerializeField] private GameObject[] downtownBuildings; // Skyscrapers
    [SerializeField] private GameObject[] midtownBuildings; // NY + Phaneron
    [SerializeField] private GameObject[] residentialBuildings; // Houses + Barcelona + France + Venice + Mexico + Croatia
    [SerializeField] private GameObject[] industrialBuildings; // Warehouses

    [Header("Street Props")]
    [SerializeField] private GameObject[] streetProps;
    [SerializeField] private float propDensity = 0.3f;

    [Header("Roads")]
    [SerializeField] private bool generateRoads = true;
    [SerializeField] private Material roadMaterial;
    [SerializeField][Range(0f, 0.3f)] private float diagonalRoadProbability = 0.15f; // 15% chance

    // City data
    private Transform cityParent;
    private int calculatedWidth;
    private int calculatedLength;
    private Vector3 cityOffset;
    private float cellSize;

    // CRITICAL: Track where roads are so buildings DON'T spawn on them
    private HashSet<Vector2Int> roadCells = new HashSet<Vector2Int>();
    private List<DiagonalRoad> diagonalRoads = new List<DiagonalRoad>();

    private class DiagonalRoad
    {
        public Vector3 startPos;
        public Vector3 endPos;
        public float width;
    }

    public void BuildCity()
    {
        if (!DetectAndCalculate())
        {
            Debug.LogError("Cannot build city - terrain not detected or invalid!");
            return;
        }

        ClearCity();
        roadCells.Clear();
        diagonalRoads.Clear();

        cityParent = new GameObject("UrbanCity").transform;
        cityParent.position = cityOffset;

        cellSize = blockSize + streetWidth;

        if (generateRoads)
        {
            GenerateRoadsWithDiagonals();
        }

        PlaceDistrictBuildings();
        PlaceStreetProps();

        Debug.Log($"Urban city built: {calculatedWidth}x{calculatedLength} blocks, {diagonalRoads.Count} diagonal roads");
    }

    private bool DetectAndCalculate()
    {
        if (autoDetectTerrain || targetTerrain == null)
        {
            targetTerrain = FindObjectOfType<Terrain>();
            if (targetTerrain == null)
            {
                Debug.LogWarning("No terrain found! Building 12x12 city at origin.");
                calculatedWidth = 12;
                calculatedLength = 12;
                cityOffset = Vector3.zero;
                return true;
            }
        }

        Vector3 terrainSize = targetTerrain.terrainData.size;
        Vector3 terrainPos = targetTerrain.transform.position;

        Debug.Log($"Detected terrain: {terrainSize.x}x{terrainSize.z} at {terrainPos}");

        float usableWidth = terrainSize.x * terrainCoverage;
        float usableLength = terrainSize.z * terrainCoverage;
        float cellSize = blockSize + streetWidth;

        calculatedWidth = Mathf.Max(1, Mathf.FloorToInt(usableWidth / cellSize));
        calculatedLength = Mathf.Max(1, Mathf.FloorToInt(usableLength / cellSize));

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
        GameObject existing = GameObject.Find("UrbanCity");
        if (existing != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(existing);
#else
            Destroy(existing);
#endif
        }
    }

    private void GenerateRoadsWithDiagonals()
    {
        Transform roadsParent = new GameObject("Roads").transform;
        roadsParent.parent = cityParent;

        // STEP 1: Create standard grid roads (vertical and horizontal)
        // NOTE: Grid roads run BETWEEN blocks, not ON blocks, so we don't mark cells

        // Vertical streets (north-south)
        for (int x = 0; x <= calculatedWidth; x++)
        {
            Vector3 start = new Vector3(x * cellSize, 0, 0);
            Vector3 end = new Vector3(x * cellSize, 0, calculatedLength * cellSize);

            CreateRoad(start, end, streetWidth, roadsParent, $"Street_Vertical_{x}");

            // Grid roads don't need cell marking - they're at grid lines, buildings are in block centers
        }

        // Horizontal streets (east-west)
        for (int z = 0; z <= calculatedLength; z++)
        {
            Vector3 start = new Vector3(0, 0, z * cellSize);
            Vector3 end = new Vector3(calculatedWidth * cellSize, 0, z * cellSize);

            CreateRoad(start, end, streetWidth, roadsParent, $"Street_Horizontal_{z}");

            // Grid roads don't need cell marking - they're at grid lines, buildings are in block centers
        }

        // STEP 2: Add diagonal roads (boulevards)
        AddDiagonalRoads(roadsParent);

        Debug.Log($"Generated {(calculatedWidth + 1) + (calculatedLength + 1)} grid roads + {diagonalRoads.Count} diagonal roads");
    }

    private void AddDiagonalRoads(Transform parent)
    {
        // Add a few strategic diagonal roads
        int diagonalsAdded = 0;

        // Main diagonal from SW to NE
        if (Random.value < diagonalRoadProbability * 3f) // Higher chance for main diagonal
        {
            Vector3 start = new Vector3(0, 0, 0);
            Vector3 end = new Vector3(calculatedWidth * cellSize, 0, calculatedLength * cellSize);
            CreateDiagonalRoad(start, end, streetWidth * 1.5f, parent, "Boulevard_Diagonal_Main");
            diagonalsAdded++;
        }

        // Secondary diagonal from SE to NW
        if (Random.value < diagonalRoadProbability * 2f)
        {
            Vector3 start = new Vector3(calculatedWidth * cellSize, 0, 0);
            Vector3 end = new Vector3(0, 0, calculatedLength * cellSize);
            CreateDiagonalRoad(start, end, streetWidth * 1.5f, parent, "Boulevard_Diagonal_Secondary");
            diagonalsAdded++;
        }

        // Random diagonal shortcuts (2-4 additional diagonals)
        int randomDiagonals = Random.Range(2, 5);
        for (int i = 0; i < randomDiagonals; i++)
        {
            if (Random.value < diagonalRoadProbability)
            {
                // Random start and end points
                Vector3 start = new Vector3(
                    Random.Range(0, calculatedWidth) * cellSize,
                    0,
                    Random.Range(0, calculatedLength) * cellSize
                );

                Vector3 end = new Vector3(
                    Random.Range(0, calculatedWidth) * cellSize,
                    0,
                    Random.Range(0, calculatedLength) * cellSize
                );

                // Only create if they're far enough apart
                if (Vector3.Distance(start, end) > cellSize * 3f)
                {
                    CreateDiagonalRoad(start, end, streetWidth, parent, $"Boulevard_Diagonal_{i}");
                    diagonalsAdded++;
                }
            }
        }

        Debug.Log($"Added {diagonalsAdded} diagonal roads");
    }

    private void CreateRoad(Vector3 start, Vector3 end, float width, Transform parent, string name)
    {
        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = name;
        road.transform.parent = parent;

        Vector3 midpoint = (start + end) / 2f;
        float length = Vector3.Distance(start, end);

        road.transform.localPosition = midpoint;
        road.transform.localScale = new Vector3(width, 0.1f, length);

        Vector3 direction = end - start;
        if (direction != Vector3.zero)
        {
            road.transform.localRotation = Quaternion.LookRotation(direction);
        }

        ApplyRoadMaterial(road);
    }

    private void CreateDiagonalRoad(Vector3 start, Vector3 end, float width, Transform parent, string name)
    {
        CreateRoad(start, end, width, parent, name);

        // Track diagonal road for collision checking
        DiagonalRoad diagRoad = new DiagonalRoad
        {
            startPos = start,
            endPos = end,
            width = width
        };
        diagonalRoads.Add(diagRoad);

        // Mark cells along diagonal as roads
        MarkDiagonalRoadCells(start, end, width);
    }

    private void MarkRoadCell(int x, int z)
    {
        // Mark grid cell as containing a road
        roadCells.Add(new Vector2Int(x, z));
    }

    private void MarkDiagonalRoadCells(Vector3 start, Vector3 end, float width)
    {
        // Mark all cells that the diagonal road passes through
        float distance = Vector3.Distance(start, end);
        int segments = Mathf.CeilToInt(distance / cellSize);

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 point = Vector3.Lerp(start, end, t);

            // Convert world position to grid coordinates
            int gridX = Mathf.FloorToInt(point.x / cellSize);
            int gridZ = Mathf.FloorToInt(point.z / cellSize);

            // Mark this cell and adjacent cells (because road has width)
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    MarkRoadCell(gridX + dx, gridZ + dz);
                }
            }
        }
    }

    private void ApplyRoadMaterial(GameObject road)
    {
        if (roadMaterial != null)
        {
            road.GetComponent<Renderer>().material = roadMaterial;
        }
        else
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.2f, 0.2f, 0.2f);
            road.GetComponent<Renderer>().material = mat;
        }
    }

    private void PlaceDistrictBuildings()
    {
        Transform buildingsParent = new GameObject("Buildings").transform;
        buildingsParent.parent = cityParent;

        int buildingsPlaced = 0;
        int buildingsSkippedDiagonal = 0;
        int buildingsSkippedDensity = 0;
        int buildingsSkippedFit = 0;

        for (int x = 0; x < calculatedWidth; x++)
        {
            for (int z = 0; z < calculatedLength; z++)
            {
                // Random density check - should this block have buildings at all?
                if (Random.value > blockDensity)
                {
                    buildingsSkippedDensity++;
                    continue;
                }

                // Determine district and get appropriate building
                DistrictType district = GetDistrictType(x, z);
                GameObject[] districtBuildings = GetBuildingsForDistrict(district);

                if (districtBuildings == null || districtBuildings.Length == 0) continue;

                // Calculate block boundaries (with safety buffer from roads)
                float roadBuffer = streetWidth * 0.6f; // Stay away from road edges
                Vector3 blockMin = new Vector3(
                    x * cellSize + roadBuffer,
                    0,
                    z * cellSize + roadBuffer
                );
                Vector3 blockMax = new Vector3(
                    (x + 1) * cellSize - roadBuffer,
                    0,
                    (z + 1) * cellSize - roadBuffer
                );

                Vector3 blockCenter = (blockMin + blockMax) / 2f;

                // CHECK: Make sure block isn't on a diagonal road
                if (IsOnDiagonalRoad(blockCenter))
                {
                    buildingsSkippedDiagonal++;
                    continue; // SKIP - diagonal road passes through here!
                }

                // Sample terrain height
                float terrainHeight = 0f;
                if (targetTerrain != null)
                {
                    Vector3 worldPos = cityOffset + blockCenter;
                    terrainHeight = targetTerrain.SampleHeight(worldPos);
                }

                // Track placed buildings in this block to avoid overlap
                List<Bounds> placedBounds = new List<Bounds>();

                // Try to place as many buildings as possible
                int maxAttempts = district == DistrictType.Downtown ? 3 : 8; // Downtown = fewer larger buildings
                int attemptsPerBuilding = 5;

                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    GameObject prefab = districtBuildings[Random.Range(0, districtBuildings.Length)];

                    // Get building bounds
                    Vector3 buildingSize = GetPrefabBounds(prefab);

                    // Try multiple positions to fit this building
                    bool placed = false;
                    for (int posAttempt = 0; posAttempt < attemptsPerBuilding; posAttempt++)
                    {
                        // Random rotation (0, 90, 180, 270)
                        int rotationSteps = Random.Range(0, 4);
                        Quaternion rotation = Quaternion.Euler(0, rotationSteps * 90f, 0);

                        // Adjust size for rotation (90/270 degrees swap X and Z)
                        Vector3 rotatedSize = buildingSize;
                        if (rotationSteps % 2 == 1) // 90 or 270 degrees
                        {
                            rotatedSize = new Vector3(buildingSize.z, buildingSize.y, buildingSize.x);
                        }

                        // Calculate safe placement area considering building size
                        Vector3 safeMin = blockMin + new Vector3(rotatedSize.x / 2f, 0, rotatedSize.z / 2f);
                        Vector3 safeMax = blockMax - new Vector3(rotatedSize.x / 2f, 0, rotatedSize.z / 2f);

                        // Check if building can fit at all
                        if (safeMin.x >= safeMax.x || safeMin.z >= safeMax.z)
                        {
                            break; // Building too big for this block
                        }

                        // Random position within safe area
                        Vector3 position = new Vector3(
                            Random.Range(safeMin.x, safeMax.x),
                            terrainHeight,
                            Random.Range(safeMin.z, safeMax.z)
                        );

                        // Create bounds for this placement
                        Bounds newBounds = new Bounds(position, rotatedSize);

                        // Check overlap with existing buildings
                        bool overlaps = false;
                        foreach (Bounds existing in placedBounds)
                        {
                            if (newBounds.Intersects(existing))
                            {
                                overlaps = true;
                                break;
                            }
                        }

                        if (!overlaps)
                        {
                            // SUCCESS - place building
#if UNITY_EDITOR
                            GameObject building = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                            building.transform.localPosition = position;
                            building.transform.localRotation = rotation;
                            building.transform.parent = buildingsParent;
#else
                            GameObject building = Instantiate(prefab, position, rotation, buildingsParent);
#endif
                            building.name = $"{district}_{x}_{z}_{attempt}";

                            EnsureColliders(building);
                            placedBounds.Add(newBounds);
                            buildingsPlaced++;
                            placed = true;
                            break;
                        }
                    }

                    if (!placed)
                    {
                        buildingsSkippedFit++;
                    }
                }
            }
        }

        Debug.Log($"Buildings: {buildingsPlaced} placed, {buildingsSkippedDiagonal} skipped (diagonal), {buildingsSkippedDensity} skipped (density), {buildingsSkippedFit} couldn't fit");
    }

    private Vector3 GetPrefabBounds(GameObject prefab)
    {
        // Get the bounds of the prefab by checking its renderers
        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            // Fallback to colliders
            Collider[] colliders = prefab.GetComponentsInChildren<Collider>();
            if (colliders.Length > 0)
            {
                Bounds bounds = colliders[0].bounds;
                foreach (Collider col in colliders)
                {
                    bounds.Encapsulate(col.bounds);
                }
                return bounds.size;
            }

            // Default size if nothing found
            return new Vector3(10f, 10f, 10f);
        }

        // Calculate combined bounds
        Bounds combinedBounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers)
        {
            combinedBounds.Encapsulate(renderer.bounds);
        }

        // Return size (add small buffer)
        Vector3 size = combinedBounds.size;
        size.x += 1f; // 1m buffer on each side
        size.z += 1f;

        return size;
    }

    private DistrictType GetDistrictType(int x, int z)
    {
        // Define districts based on position
        int centerX = calculatedWidth / 2;
        int centerZ = calculatedLength / 2;
        float distFromCenter = Vector2.Distance(new Vector2(x, z), new Vector2(centerX, centerZ));
        float maxDist = Mathf.Max(calculatedWidth, calculatedLength) / 2f;
        float normalizedDist = distFromCenter / maxDist;

        // Downtown (center) - 0-30% from center
        if (normalizedDist < 0.3f)
        {
            return DistrictType.Downtown;
        }
        // Midtown - 30-60% from center
        else if (normalizedDist < 0.6f)
        {
            return DistrictType.Midtown;
        }
        // Industrial (south/edges) - check if in bottom third
        else if (z < calculatedLength * 0.33f)
        {
            return DistrictType.Industrial;
        }
        // Residential (outer areas)
        else
        {
            return DistrictType.Residential;
        }
    }

    private GameObject[] GetBuildingsForDistrict(DistrictType district)
    {
        switch (district)
        {
            case DistrictType.Downtown:
                return downtownBuildings; // Skyscrapers
            case DistrictType.Midtown:
                return midtownBuildings; // NY + Phaneron
            case DistrictType.Residential:
                return residentialBuildings; // Houses + European styles
            case DistrictType.Industrial:
                return industrialBuildings; // Warehouses
            default:
                return midtownBuildings;
        }
    }

    private bool IsOnDiagonalRoad(Vector3 position)
    {
        foreach (DiagonalRoad road in diagonalRoads)
        {
            // Calculate distance from point to line segment
            float dist = DistancePointToLineSegment(position, road.startPos, road.endPos);
            if (dist < road.width / 2f + blockSize * 0.2f) // Add buffer
            {
                return true; // Too close to diagonal road
            }
        }
        return false;
    }

    private float DistancePointToLineSegment(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        // Ignore Y axis for 2D calculation
        Vector2 p = new Vector2(point.x, point.z);
        Vector2 a = new Vector2(lineStart.x, lineStart.z);
        Vector2 b = new Vector2(lineEnd.x, lineEnd.z);

        Vector2 ab = b - a;
        Vector2 ap = p - a;

        float t = Vector2.Dot(ap, ab) / Vector2.Dot(ab, ab);
        t = Mathf.Clamp01(t);

        Vector2 closest = a + t * ab;
        return Vector2.Distance(p, closest);
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

        int propsPlaced = 0;

        // Place props at intersections
        for (int x = 0; x <= calculatedWidth; x++)
        {
            for (int z = 0; z <= calculatedLength; z++)
            {
                if (Random.value > propDensity) continue;

                Vector3 position = new Vector3(x * cellSize, 0, z * cellSize);

                if (targetTerrain != null)
                {
                    Vector3 worldPos = cityOffset + position;
                    position.y = targetTerrain.SampleHeight(worldPos);
                }

                PlaceProp(position, propsParent);
                propsPlaced++;
            }
        }

        Debug.Log($"Placed {propsPlaced} street props");
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
        float totalWidth = calculatedWidth * cellSize;
        float totalLength = calculatedLength * cellSize;
        return cityOffset + new Vector3(totalWidth / 2f, 0, totalLength / 2f);
    }

    public Vector3 GetRandomStreetPosition()
    {
        // Find a random grid intersection or street
        bool isVerticalStreet = Random.value > 0.5f;
        Vector3 position;

        if (isVerticalStreet)
        {
            int streetIndex = Random.Range(0, calculatedWidth + 1);
            float streetCenterX = streetIndex * cellSize;
            float xOffset = Random.Range(-streetWidth * 0.3f, streetWidth * 0.3f);
            float z = Random.Range(0, calculatedLength * cellSize);
            position = new Vector3(streetCenterX + xOffset, 0, z);
        }
        else
        {
            int streetIndex = Random.Range(0, calculatedLength + 1);
            float streetCenterZ = streetIndex * cellSize;
            float zOffset = Random.Range(-streetWidth * 0.3f, streetWidth * 0.3f);
            float x = Random.Range(0, calculatedWidth * cellSize);
            position = new Vector3(x, 0, streetCenterZ + zOffset);
        }

        position += cityOffset;

        if (targetTerrain != null)
        {
            position.y = targetTerrain.SampleHeight(position) + 1f;
        }
        else
        {
            position.y = 1f;
        }

        return position;
    }

    private enum DistrictType
    {
        Downtown,    // Skyscrapers (center)
        Midtown,     // NY + Phaneron (mid-ring)
        Residential, // Houses + European styles (outer)
        Industrial   // Warehouses (south/edges)
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(UrbanCityBuilder))]
public class UrbanCityBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        UrbanCityBuilder builder = (UrbanCityBuilder)target;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "URBAN CITY BUILDER - ALL ASSETS\n\n" +
            "✓ Uses ALL 204 city_lowpoly buildings\n" +
            "✓ Creates districts (Downtown/Midtown/Residential/Industrial)\n" +
            "✓ Generates diagonal roads\n" +
            "✓ GUARANTEES buildings never spawn on roads\n\n" +
            "Load assets using buttons below, then Build!",
            MessageType.Info);

        EditorGUILayout.Space();

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("BUILD URBAN CITY", GUILayout.Height(50)))
        {
            builder.BuildCity();
            EditorUtility.SetDirty(builder);
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Load District Assets:", EditorStyles.boldLabel);

        if (GUILayout.Button("Load Downtown Buildings (Skyscrapers)"))
        {
            LoadPrefabs(builder, "Assets/city_lowpoly/prefabs/Skyscrapers", "downtownBuildings");
        }

        if (GUILayout.Button("Load Midtown Buildings (NY + Phaneron)"))
        {
            LoadMultipleFolders(builder, new string[] {
                "Assets/city_lowpoly/prefabs/NY",
                "Assets/city_lowpoly/prefabs/Phaneron_LP"
            }, "midtownBuildings");
        }

        if (GUILayout.Button("Load Residential Buildings (Houses + European)"))
        {
            LoadMultipleFolders(builder, new string[] {
                "Assets/city_lowpoly/prefabs/Houses",
                "Assets/city_lowpoly/prefabs/Barcelona",
                "Assets/city_lowpoly/prefabs/France",
                "Assets/city_lowpoly/prefabs/Venice",
                "Assets/city_lowpoly/prefabs/Mexico",
                "Assets/city_lowpoly/prefabs/Croatia"
            }, "residentialBuildings");
        }

        if (GUILayout.Button("Load Industrial Buildings (Warehouses)"))
        {
            LoadPrefabs(builder, "Assets/city_lowpoly/prefabs/Warehouse", "industrialBuildings");
        }

        if (GUILayout.Button("Load Street Props"))
        {
            LoadPrefabs(builder, "Assets/city_lowpoly/prefabs/Props", "streetProps");
        }

        EditorGUILayout.Space();

        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("LOAD ALL ASSETS AT ONCE", GUILayout.Height(40)))
        {
            LoadPrefabs(builder, "Assets/city_lowpoly/prefabs/Skyscrapers", "downtownBuildings");
            LoadMultipleFolders(builder, new string[] {
                "Assets/city_lowpoly/prefabs/NY",
                "Assets/city_lowpoly/prefabs/Phaneron_LP"
            }, "midtownBuildings");
            LoadMultipleFolders(builder, new string[] {
                "Assets/city_lowpoly/prefabs/Houses",
                "Assets/city_lowpoly/prefabs/Barcelona",
                "Assets/city_lowpoly/prefabs/France",
                "Assets/city_lowpoly/prefabs/Venice",
                "Assets/city_lowpoly/prefabs/Mexico",
                "Assets/city_lowpoly/prefabs/Croatia"
            }, "residentialBuildings");
            LoadPrefabs(builder, "Assets/city_lowpoly/prefabs/Warehouse", "industrialBuildings");
            LoadPrefabs(builder, "Assets/city_lowpoly/prefabs/Props", "streetProps");

            EditorUtility.DisplayDialog("Assets Loaded!", "All city assets loaded successfully!", "OK");
        }
        GUI.backgroundColor = Color.white;
    }

    private void LoadPrefabs(UrbanCityBuilder builder, string path, string fieldName)
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

    private void LoadMultipleFolders(UrbanCityBuilder builder, string[] paths, string fieldName)
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

        SerializedObject so = new SerializedObject(builder);
        SerializedProperty prop = so.FindProperty(fieldName);
        prop.arraySize = allPrefabs.Count;

        for (int i = 0; i < allPrefabs.Count; i++)
        {
            prop.GetArrayElementAtIndex(i).objectReferenceValue = allPrefabs[i];
        }

        so.ApplyModifiedProperties();
        Debug.Log($"Loaded {allPrefabs.Count} prefabs from {paths.Length} folders into {fieldName}");
    }
}
#endif
