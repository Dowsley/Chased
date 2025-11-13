using System;
using System.Collections.Generic;
using UnityEngine;

namespace City
{
	[Serializable]
	public class WaypointPath
	{
		public List<Vector3> points = new();
	}

	[Serializable]
	public class IntersectionNode
	{
		public int id;
		public Vector3 position;
		public readonly List<LaneEdge> Outgoing = new();
		public readonly List<LaneEdge> Incoming = new();
	}

	[Serializable]
	public class LaneEdge
	{
		public int id;
		public IntersectionNode from;
		public IntersectionNode to;
		public WaypointPath path;
		public float speedLimit = 20f;

		public float Cost => ApproxLength + 2f; // small turn bias

		private float? _lengthCache;
		public float ApproxLength
		{
			get
			{
				if (_lengthCache.HasValue) return _lengthCache.Value;
				float len = 0f;
				var pts = path?.points;
				if (pts is { Count: > 1 })
				{
					for (int i = 1; i < pts.Count; i++)
					{
						len += Vector3.Distance(pts[i - 1], pts[i]);
					}
				}
				_lengthCache = len;
				return len;
			}
		}
	}

	public class RoadGraph : MonoBehaviour
	{
		public static RoadGraph Instance { get; private set; }

		[SerializeField] private List<IntersectionNode> nodes = new List<IntersectionNode>();
		[SerializeField] private List<LaneEdge> lanes = new List<LaneEdge>();

		private void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
				DontDestroyOnLoad(gameObject);
				RebuildAdjacency();
			}
			else
			{
				Destroy(gameObject);
			}
		}

		private void OnEnable()
		{
			// Ensure adjacency is valid when deserializing from scene/prefab
			RebuildAdjacency();
		}

		private void RebuildAdjacency()
		{
			if (nodes == null || lanes == null) return;
			// Map node ids to canonical instances in the nodes list
			var idToNode = new Dictionary<int, IntersectionNode>(nodes.Count);
			for (int i = 0; i < nodes.Count; i++)
			{
				var n = nodes[i];
				if (n == null) continue;
				idToNode[n.id] = n;
			}

			// Rebind lane endpoints to canonical node instances (Unity deep-serializes, breaking reference identity)
			for (int i = 0; i < lanes.Count; i++)
			{
				var e = lanes[i];
				if (e == null) continue;
				if (e.from != null && idToNode.TryGetValue(e.from.id, out var fromNode))
				{
					e.from = fromNode;
				}
				if (e.to != null && idToNode.TryGetValue(e.to.id, out var toNode))
				{
					e.to = toNode;
				}
			}

			// Clear existing adjacency
			for (int i = 0; i < nodes.Count; i++)
			{
				if (nodes[i] == null) continue;
				nodes[i].Outgoing.Clear();
				nodes[i].Incoming.Clear();
			}
			// Re-link edges to node adjacency lists
			for (int i = 0; i < lanes.Count; i++)
			{
				var e = lanes[i];
				if (e == null || e.from == null || e.to == null) continue;
				e.from.Outgoing.Add(e);
				e.to.Incoming.Add(e);
			}
		}

		public void Clear()
		{
			nodes.Clear();
			lanes.Clear();
		}

		public IReadOnlyList<IntersectionNode> Nodes => nodes;
		public IReadOnlyList<LaneEdge> Lanes => lanes;

		public IntersectionNode CreateNode(Vector3 position)
		{
			var n = new IntersectionNode { id = nodes.Count, position = position };
			nodes.Add(n);
			return n;
		}

		public LaneEdge CreateLane(IntersectionNode from, IntersectionNode to, List<Vector3> points, float speedLimit = 20f)
		{
			var e = new LaneEdge
			{
				id = lanes.Count,
				from = from,
				to = to,
				path = new WaypointPath { points = points },
				speedLimit = speedLimit
			};
			lanes.Add(e);
			from.Outgoing.Add(e);
			to.Incoming.Add(e);
			return e;
		}

		public List<Vector3> FindPath(Vector3 worldStart, Vector3 worldEnd)
		{
			IntersectionNode startNode = FindNearestNode(worldStart);
			IntersectionNode endNode = FindNearestNode(worldEnd);
			if (startNode == null || endNode == null) return null;
			if (startNode.id == endNode.id)
			{
				// Fallback: trivial segment to avoid null paths when start=end node
				return new List<Vector3> { startNode.position, endNode.position };
			}

			var cameFrom = new Dictionary<int, int>();
			var gScore = new Dictionary<int, float>();
			var fScore = new Dictionary<int, float>();
			var open = new PriorityQueue<int>();

			for (int i = 0; i < nodes.Count; i++)
			{
				gScore[i] = float.PositiveInfinity;
				fScore[i] = float.PositiveInfinity;
			}

			gScore[startNode.id] = 0f;
			fScore[startNode.id] = Heuristic(startNode.position, endNode.position);
			open.Enqueue(startNode.id, fScore[startNode.id]);

			while (open.Count > 0)
			{
				int currentId = open.Dequeue();
				if (currentId == endNode.id)
				{
					return ReconstructPath(cameFrom, startNode.id, endNode.id);
				}

				var current = nodes[currentId];
				foreach (var edge in current.Outgoing)
				{
					int neighborId = edge.to.id;
					float tentative = gScore[currentId] + edge.Cost;
					if (tentative < gScore[neighborId])
					{
						cameFrom[neighborId] = currentId;
						gScore[neighborId] = tentative;
						fScore[neighborId] = tentative + Heuristic(nodes[neighborId].position, endNode.position);
						open.Enqueue(neighborId, fScore[neighborId]);
					}
				}
			}

			// Fallback: BFS connectivity test (unweighted)
			var q = new Queue<int>();
			var visited = new HashSet<int>();
			var parent = new Dictionary<int, int>();
			q.Enqueue(startNode.id);
			visited.Add(startNode.id);
			bool found = false;
			while (q.Count > 0)
			{
				int cur = q.Dequeue();
				if (cur == endNode.id) { found = true; break; }
				foreach (var e in nodes[cur].Outgoing)
				{
					int nid = e.to.id;
					if (!visited.Add(nid)) continue;
					parent[nid] = cur;
					q.Enqueue(nid);
				}
			}
			if (found)
			{
				Debug.LogWarning($"RoadGraph: A* failed but BFS succeeded. start=({startNode.id}) end=({endNode.id})");
				return ReconstructPath(parent, startNode.id, endNode.id);
			}

			Debug.LogWarning($"RoadGraph: A* and BFS failed between nodes. start=({startNode.id}) end=({endNode.id})");
			return null;
		}

		private float Heuristic(Vector3 a, Vector3 b) => Vector3.Distance(a, b);

		private List<Vector3> ReconstructPath(Dictionary<int, int> cameFrom, int startId, int endId)
		{
			var nodePath = new List<int>();
			int current = endId;
			nodePath.Add(current);
			while (current != startId)
			{
				if (!cameFrom.TryGetValue(current, out int prev)) break;
				current = prev;
				nodePath.Add(current);
			}
			nodePath.Reverse();

			var result = new List<Vector3>();
			for (int i = 0; i < nodePath.Count - 1; i++)
			{
				var from = nodes[nodePath[i]];
				var to = nodes[nodePath[i + 1]];
				LaneEdge edge = null;
				foreach (var e in from.Outgoing)
				{
					if (e.to == to) { edge = e; break; }
				}
				if (edge is { path: not null })
				{
					if (result.Count > 0 && edge.path.points.Count > 0 && result[^1] == edge.path.points[0])
					{
						for (int p = 1; p < edge.path.points.Count; p++) result.Add(edge.path.points[p]);
					}
					else
					{
						result.AddRange(edge.path.points);
					}
				}
			}
			return result;
		}

		private IntersectionNode FindNearestNode(Vector3 worldPos)
		{
			float best = float.PositiveInfinity;
			IntersectionNode bestNode = null;
			foreach (var n in nodes)
			{
				float d = Vector3.SqrMagnitude(worldPos - n.position);
				if (d < best) { best = d; bestNode = n; }
			}
			return bestNode;
		}
	}

	internal class PriorityQueue<T>
	{
		private readonly List<(T item, float pri)> _data = new();
		public int Count => _data.Count;
		public void Enqueue(T item, float priority)
		{
			_data.Add((item, priority));
			int ci = _data.Count - 1;
			while (ci > 0)
			{
				int pi = (ci - 1) / 2;
				if (_data[ci].pri >= _data[pi].pri) break;
				(_data[ci], _data[pi]) = (_data[pi], _data[ci]);
				ci = pi;
			}
		}
		public T Dequeue()
		{
			int li = _data.Count - 1;
			var frontItem = _data[0].item;
			_data[0] = _data[li];
			_data.RemoveAt(li);
			--li;
			int pi = 0;
			while (true)
			{
				int ci = pi * 2 + 1;
				if (ci > li) break;
				int rc = ci + 1;
				if (rc <= li && _data[rc].pri < _data[ci].pri) ci = rc;
				if (_data[pi].pri <= _data[ci].pri) break;
				(_data[pi], _data[ci]) = (_data[ci], _data[pi]);
				pi = ci;
			}
			return frontItem;
		}
	}
}


