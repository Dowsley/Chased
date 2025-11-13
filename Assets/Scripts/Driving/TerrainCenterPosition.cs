using UnityEngine;

namespace Driving
{
    public class TerrainCenterPosition : MonoBehaviour
    {
        [Header("Terrain Settings")]
        [SerializeField] private Terrain targetTerrain;
        [SerializeField] private bool autoFindTerrain = true;

        [Header("Position Settings")]
        [SerializeField] private float heightOffset = 2f;
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private float raycastHeight = 1000f;

        private void Start()
        {
            if (runOnStart)
            {
                CenterOnTerrain();
            }
        }

        public void CenterOnTerrain()
        {
            // Find terrain if not assigned
            if (targetTerrain == null && autoFindTerrain)
            {
                targetTerrain = Terrain.activeTerrain;

                if (targetTerrain == null)
                {
                    Debug.LogError("TerrainCenterPosition: No terrain found in scene!");
                    return;
                }
            }

            if (targetTerrain == null)
            {
                Debug.LogError("TerrainCenterPosition: No terrain assigned!");
                return;
            }

            // Get terrain data
            TerrainData terrainData = targetTerrain.terrainData;
            Vector3 terrainPosition = targetTerrain.transform.position;

            // Calculate center position (X and Z)
            float centerX = terrainPosition.x + (terrainData.size.x / 2f);
            float centerZ = terrainPosition.z + (terrainData.size.z / 2f);

            // Raycast down from above to find terrain height
            Vector3 raycastStart = new Vector3(centerX, terrainPosition.y + raycastHeight, centerZ);
            RaycastHit hit;

            if (Physics.Raycast(raycastStart, Vector3.down, out hit, raycastHeight * 2f))
            {
                // Position at hit point plus offset
                transform.position = hit.point + Vector3.up * heightOffset;
                Debug.Log($"TerrainCenterPosition: Positioned at center ({transform.position.x:F2}, {transform.position.y:F2}, {transform.position.z:F2})");
            }
            else
            {
                // Fallback if raycast fails
                Debug.LogWarning("TerrainCenterPosition: Raycast didn't hit terrain, using terrain base height");
                transform.position = new Vector3(centerX, terrainPosition.y + heightOffset, centerZ);
            }
        }

        // Call this from inspector or code to re-center
        [ContextMenu("Center on Terrain")]
        public void CenterOnTerrainMenu()
        {
            CenterOnTerrain();
        }
    }
}
