using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CityBuilder : MonoBehaviour
{
    [Header("City Layout")]
    [SerializeField] private int cityWidth = 8;
    [SerializeField] private int cityLength = 8;
    [SerializeField] private float blockSize = 40f;
    [SerializeField] private float streetWidth = 12f;

    [Header("Building Prefabs")]
    [SerializeField] private GameObject[] buildingPrefabs;
    [SerializeField] private GameObject[] skyscraperPrefabs;

    [Header("Street Props")]
    [SerializeField] private GameObject[] streetProps;
    [SerializeField] private float propSpacing = 20f;

    [Header("Ground")]
    [SerializeField] private Material streetMaterial;

    private Transform cityParent;

    public void BuildCity()
    {
        ClearCity();

        cityParent = new GameObject("City").transform;
        cityParent.position = Vector3.zero;

        CreateGround();
        PlaceBuildings();
        PlaceStreetProps();

        Debug.Log($"City built: {cityWidth}x{cityLength} blocks");
    }

    private void ClearCity()
    {
        GameObject existing = GameObject.Find("City");
        if (existing != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(existing);
#else
            Destroy(existing);
#endif
        }
    }

    private void CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "CityGround";
        ground.transform.parent = cityParent;

        float totalSize = Mathf.Max(cityWidth, cityLength) * (blockSize + streetWidth);
        ground.transform.localScale = new Vector3(totalSize / 10f, 1, totalSize / 10f);
        ground.transform.position = new Vector3(totalSize / 2f, -0.1f, totalSize / 2f);

        if (streetMaterial != null)
        {
            ground.GetComponent<Renderer>().material = streetMaterial;
        }
        else
        {
            // Default gray material for streets
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.3f, 0.3f, 0.3f);
            ground.GetComponent<Renderer>().material = mat;
        }

        // Add collider for driving
        if (ground.GetComponent<Collider>() != null)
        {
            ground.GetComponent<Collider>().enabled = true;
        }
    }

    private void PlaceBuildings()
    {
        if (buildingPrefabs == null || buildingPrefabs.Length == 0)
        {
            Debug.LogWarning("No building prefabs assigned! Assign prefabs from Assets/city_lowpoly/prefabs/NY");
            return;
        }

        Transform buildingsParent = new GameObject("Buildings").transform;
        buildingsParent.parent = cityParent;

        for (int x = 0; x < cityWidth; x++)
        {
            for (int z = 0; z < cityLength; z++)
            {
                Vector3 blockCenter = new Vector3(
                    x * (blockSize + streetWidth) + blockSize / 2f,
                    0,
                    z * (blockSize + streetWidth) + blockSize / 2f
                );

                // Place 1-3 buildings per block with some variety
                int buildingCount = Random.Range(1, 4);

                for (int i = 0; i < buildingCount; i++)
                {
                    GameObject prefab = GetRandomBuilding(x, z);
                    if (prefab == null) continue;

                    Vector3 offset = new Vector3(
                        Random.Range(-blockSize * 0.3f, blockSize * 0.3f),
                        0,
                        Random.Range(-blockSize * 0.3f, blockSize * 0.3f)
                    );

                    Vector3 position = blockCenter + offset;
                    Quaternion rotation = Quaternion.Euler(0, Random.Range(0, 4) * 90f, 0);

#if UNITY_EDITOR
                    GameObject building = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    building.transform.position = position;
                    building.transform.rotation = rotation;
                    building.transform.parent = buildingsParent;
#else
                    GameObject building = Instantiate(prefab, position, rotation, buildingsParent);
#endif
                    building.name = $"Building_{x}_{z}_{i}";

                    // Add colliders if missing
                    if (building.GetComponentInChildren<Collider>() == null)
                    {
                        MeshFilter[] meshes = building.GetComponentsInChildren<MeshFilter>();
                        foreach (MeshFilter mf in meshes)
                        {
                            MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                            mc.convex = false;
                        }
                    }
                }
            }
        }
    }

    private GameObject GetRandomBuilding(int x, int z)
    {
        // Use skyscrapers in downtown area (center)
        int centerX = cityWidth / 2;
        int centerZ = cityLength / 2;
        float distFromCenter = Vector2.Distance(new Vector2(x, z), new Vector2(centerX, centerZ));

        bool useSkyscraper = distFromCenter < 2f && skyscraperPrefabs != null && skyscraperPrefabs.Length > 0;

        if (useSkyscraper && Random.value > 0.3f)
        {
            return skyscraperPrefabs[Random.Range(0, skyscraperPrefabs.Length)];
        }
        else if (buildingPrefabs != null && buildingPrefabs.Length > 0)
        {
            return buildingPrefabs[Random.Range(0, buildingPrefabs.Length)];
        }

        return null;
    }

    private void PlaceStreetProps()
    {
        if (streetProps == null || streetProps.Length == 0) return;

        Transform propsParent = new GameObject("StreetProps").transform;
        propsParent.parent = cityParent;

        // Place props along streets (barriers, signals, etc.)
        for (int i = 0; i < cityWidth; i++)
        {
            for (int p = 0; p < cityLength * (blockSize + streetWidth) / propSpacing; p++)
            {
                Vector3 position = new Vector3(
                    i * (blockSize + streetWidth) - streetWidth / 2f,
                    0,
                    p * propSpacing
                );

                if (Random.value > 0.7f) // 30% chance to place a prop
                {
                    GameObject prefab = streetProps[Random.Range(0, streetProps.Length)];
#if UNITY_EDITOR
                    GameObject prop = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    prop.transform.position = position;
                    prop.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                    prop.transform.parent = propsParent;
#else
                    GameObject prop = Instantiate(prefab, position, Quaternion.Euler(0, Random.Range(0, 360), 0), propsParent);
#endif
                }
            }
        }
    }

    public Vector3 GetCityCenter()
    {
        float totalSize = Mathf.Max(cityWidth, cityLength) * (blockSize + streetWidth);
        return new Vector3(totalSize / 2f, 0, totalSize / 2f);
    }

    public Vector3 GetRandomStreetPosition()
    {
        // Return a position on a street (not in a building block)
        float cellSize = blockSize + streetWidth;
        bool isVerticalStreet = Random.value > 0.5f;

        Vector3 position;

        if (isVerticalStreet)
        {
            // Vertical street: X is on a street line, Z can be anywhere along it
            int streetIndex = Random.Range(0, cityWidth + 1);

            // Street is at streetIndex * cellSize - ensure we're IN the street, not building
            float streetCenterX = streetIndex * cellSize;
            float xOffset = Random.Range(-streetWidth * 0.3f, streetWidth * 0.3f);

            float z = Random.Range(0, cityLength * cellSize);

            position = new Vector3(streetCenterX + xOffset, 1f, z);
        }
        else
        {
            // Horizontal street: Z is on a street line, X can be anywhere along it
            int streetIndex = Random.Range(0, cityLength + 1);

            float streetCenterZ = streetIndex * cellSize;
            float zOffset = Random.Range(-streetWidth * 0.3f, streetWidth * 0.3f);

            float x = Random.Range(0, cityWidth * cellSize);

            position = new Vector3(x, 1f, streetCenterZ + zOffset);
        }

        return position;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(CityBuilder))]
public class CityBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CityBuilder builder = (CityBuilder)target;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "City Builder for Chase Game\n\n" +
            "1. Assign building prefabs from Assets/city_lowpoly/prefabs/NY\n" +
            "2. Assign skyscraper prefabs from Assets/city_lowpoly/prefabs/Skyscrapers\n" +
            "3. Optionally assign street props (barriers, signals)\n" +
            "4. Click 'Build City' to generate the city layout\n\n" +
            "The city will have a grid layout perfect for chase gameplay!",
            MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("Build City", GUILayout.Height(40)))
        {
            builder.BuildCity();
            EditorUtility.SetDirty(builder);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Load Building Prefabs from NY Folder"))
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

    private void LoadPrefabs(CityBuilder builder, string path, string fieldName)
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
