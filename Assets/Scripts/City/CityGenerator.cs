using System.Collections.Generic;
using UnityEngine;

namespace City
{
	public class CityGenerator : MonoBehaviour
	{
		[Header("Layout")]
		[SerializeField] private int rows = 6;
		[SerializeField] private int cols = 6;
		[SerializeField] private float blockSize = 40f;
		[SerializeField] private float roadWidth = 10f;
		[SerializeField] private float groundSizeMultiplier = 4f;
		[SerializeField] private bool autoGenerateOnStart = false;

		[Header("Buildings")]
		[SerializeField] private float buildingMargin = 2f;
		[SerializeField] private float minBuildingHeight = 6f;
		[SerializeField] private float maxBuildingHeight = 40f;
		[SerializeField] private int randomSeed = 12345;
		[SerializeField] private float buildingSpacingMultiplier = 2f; // 2x default spacing

		[Header("Parents")]
		[SerializeField] private Transform groundRoot;
		[SerializeField] private Transform roadsRoot;
		[SerializeField] private Transform buildingsRoot;

		private RoadGraph _graph;

		private void Start()
		{
			if (autoGenerateOnStart)
			{
				Generate();
			}
		}

		[ContextMenu("Generate City")]
		public void Generate()
		{
			EnsureRoots();
			CenterRoots();
			ClearRoots();

			_graph = FindFirstObjectByType<RoadGraph>();
			if (!_graph)
			{
				var graphGo = new GameObject("RoadGraph");
				_graph = graphGo.AddComponent<RoadGraph>();
			}
			_graph.Clear();

			GenerateGround();
			var grid = GenerateNodesGrid();
			GenerateStraightRoads(grid);
			GenerateBuildings(grid);
		}

		public void ClearCity()
		{
			ClearRoots();
			if (!_graph) _graph = FindFirstObjectByType<RoadGraph>();
			if (_graph) _graph.Clear();
		}

		private void EnsureRoots()
		{
			if (!groundRoot)
			{
				var g = new GameObject("Ground");
				groundRoot = g.transform;
			}
			if (!roadsRoot)
			{
				var r = new GameObject("Roads");
				roadsRoot = r.transform;
			}
			if (!buildingsRoot)
			{
				var b = new GameObject("Buildings");
				buildingsRoot = b.transform;
			}
		}

		private void CenterRoots()
		{
			groundRoot.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
			roadsRoot.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
			buildingsRoot.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
			groundRoot.localScale = Vector3.one;
			roadsRoot.localScale = Vector3.one;
			buildingsRoot.localScale = Vector3.one;
		}

		private void ClearRoots()
		{
			ClearChildren(groundRoot);
			ClearChildren(roadsRoot);
			ClearChildren(buildingsRoot);
		}

		private static void ClearChildren(Transform t)
		{
			if (!t) return;
			for (int i = t.childCount - 1; i >= 0; i--)
			{
				var child = t.GetChild(i);
#if UNITY_EDITOR
				if (!Application.isPlaying)
					DestroyImmediate(child.gameObject);
				else
					Destroy(child.gameObject);
#else
				Destroy(child.gameObject);
#endif
			}
		}

		private void GenerateGround()
		{
			float spanX = cols * (blockSize + roadWidth);
			float spanZ = rows * (blockSize + roadWidth);
			float size = Mathf.Max(spanX, spanZ) * groundSizeMultiplier;

			var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
			plane.name = "GroundPlane";
			plane.transform.SetParent(groundRoot, false);
			plane.transform.localScale = new Vector3(size / 10f, 1f, size / 10f); // Unity plane is 10x10
			var mr = plane.GetComponent<MeshRenderer>();
			if (mr) mr.sharedMaterial = DefaultGray();
		}

		private Material DefaultGray()
		{
			var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
			mat.color = new Color(0.20f, 0.20f, 0.20f, 1f);
			return mat;
		}

		private Material RoadDark()
		{
			var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
			mat.color = new Color(0.10f, 0.10f, 0.10f, 1f);
			return mat;
		}

		private Material BuildingMat()
		{
			var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
			mat.color = new Color(0.35f, 0.35f, 0.38f, 1f);
			return mat;
		}

		private IntersectionNode[,] GenerateNodesGrid()
		{
			var grid = new IntersectionNode[rows + 1, cols + 1];
			float step = blockSize + roadWidth;
			Vector3 offset = GetCityOffset(step);
			for (int r = 0; r <= rows; r++)
			{
				for (int c = 0; c <= cols; c++)
				{
					Vector3 pos = new Vector3(c * step, 0f, r * step) + offset;
					grid[r, c] = _graph.CreateNode(pos);
				}
			}
			return grid;
		}

		private void GenerateStraightRoads(IntersectionNode[,] grid)
		{
			float step = blockSize + roadWidth;
			Vector3 offset = GetCityOffset(step);

			// Horizontal roads (rows)
			for (int r = 0; r <= rows; r++)
			{
				Vector3 a = new Vector3(0f, 0f, r * step) + offset;
				Vector3 b = new Vector3(cols * step, 0f, r * step) + offset;
				CreateRoadStrip(a, b);
			}

			// Vertical roads (cols)
			for (int c = 0; c <= cols; c++)
			{
				Vector3 a = new Vector3(c * step, 0f, 0f) + offset;
				Vector3 b = new Vector3(c * step, 0f, rows * step) + offset;
				CreateRoadStrip(a, b, true);
			}

			// Lanes between adjacent nodes (both directions)
			for (int r = 0; r <= rows; r++)
			{
				for (int c = 0; c < cols; c++)
				{
					var n0 = grid[r, c];
					var n1 = grid[r, c + 1];
					_graph.CreateLane(n0, n1, new List<Vector3> { n0.position, n1.position });
					_graph.CreateLane(n1, n0, new List<Vector3> { n1.position, n0.position });
				}
			}
			for (int c = 0; c <= cols; c++)
			{
				for (int r = 0; r < rows; r++)
				{
					var n0 = grid[r, c];
					var n1 = grid[r + 1, c];
					_graph.CreateLane(n0, n1, new List<Vector3> { n0.position, n1.position });
					_graph.CreateLane(n1, n0, new List<Vector3> { n1.position, n0.position });
				}
			}
		}

		private void CreateRoadStrip(Vector3 a, Vector3 b, bool vertical = false)
		{
			Vector3 mid = (a + b) * 0.5f;
			float length = Vector3.Distance(a, b);
			var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
			go.name = vertical ? "Road_V" : "Road_H";
			go.transform.SetParent(roadsRoot, false);
			go.transform.position = mid + Vector3.up * -0.49f; // slightly below 0 to avoid z-fight
			go.transform.localScale = vertical
				? new Vector3(roadWidth, 1f, length + roadWidth)
				: new Vector3(length + roadWidth, 1f, roadWidth);
			var mr = go.GetComponent<MeshRenderer>();
			if (mr) mr.sharedMaterial = RoadDark();
		}

		private void GenerateBuildings(IntersectionNode[,] grid)
		{
			var rnd = new System.Random(randomSeed);
			float step = blockSize + roadWidth;
			Vector3 offset = GetCityOffset(step);
			for (int r = 0; r < rows; r++)
			{
				for (int c = 0; c < cols; c++)
				{
					float margin = buildingMargin * Mathf.Max(1f, buildingSpacingMultiplier);
					float sizeX = Mathf.Max(1f, blockSize - margin * 2f);
					float sizeZ = Mathf.Max(1f, blockSize - margin * 2f);
					float height = Mathf.Lerp(minBuildingHeight, maxBuildingHeight, (float)rnd.NextDouble());

					// Center building in the road-bounded block (half a step from the lower grid line)
					float halfStep = step * 0.5f;
					Vector3 center = new Vector3(c * step + halfStep, height * 0.5f, r * step + halfStep) + offset;

					var building = GameObject.CreatePrimitive(PrimitiveType.Cube);
					building.name = "Building";
					building.transform.SetParent(buildingsRoot, false);
					building.transform.position = center;
					building.transform.localScale = new Vector3(sizeX, height, sizeZ);
					var mr = building.GetComponent<MeshRenderer>();
					if (mr) mr.sharedMaterial = BuildingMat();
				}
			}
		}

		private Vector3 GetCityOffset(float step)
		{
			float width = cols * step;
			float depth = rows * step;
			// Center city around world origin (also centers on the ground plane)
			return new Vector3(-width * 0.5f, 0f, -depth * 0.5f);
		}
	}
}


