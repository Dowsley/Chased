using UnityEngine;

namespace City
{
	public class RoadGraphGizmos : MonoBehaviour
	{
		[SerializeField] private Color nodeColor = Color.yellow;
		[SerializeField] private Color laneColor = Color.cyan;
		[SerializeField] private float nodeSize = 0.8f;

		private void OnDrawGizmos()
		{
			if (RoadGraph.Instance == null) return;
			var graph = RoadGraph.Instance;

			Gizmos.color = nodeColor;
			foreach (var n in graph.Nodes)
			{
				Gizmos.DrawSphere(n.position + Vector3.up * 0.05f, nodeSize * 0.5f);
			}

			Gizmos.color = laneColor;
			foreach (var e in graph.Lanes)
			{
				var pts = e.path.points;
				for (int i = 1; i < pts.Count; i++)
				{
					Gizmos.DrawLine(pts[i - 1] + Vector3.up * 0.05f, pts[i] + Vector3.up * 0.05f);
				}
			}
		}
	}
}


