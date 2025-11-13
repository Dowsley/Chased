using System.Collections.Generic;
using City;
using Core;
using UnityEngine;

namespace Driving.AI
{
	public class PathFollowingAIDriverInput : BaseDriverInput
	{
		[SerializeField] private bool disabled = false;
		[SerializeField] private float replanInterval = 1.0f;
		[SerializeField] private float lookaheadDistance = 10f;
		[SerializeField] private float targetSpeed = 20f;
		[SerializeField] private float maxSteerAngle = 30f;
		[SerializeField] private bool patrolWhenNoTarget = true;
		[SerializeField] private bool debugLogs = false;
			[SerializeField] private bool reverseRecoveryEnabled = true;
			[SerializeField] private float reverseDuration = 1.25f;
			[SerializeField] private float reverseThrottle = -0.6f;
			[SerializeField] private float forwardRecoverDuration = 0.75f;
			[SerializeField] private float forwardRecoverThrottle = 0.35f;

		private readonly List<Vector3> _path = new List<Vector3>();
		private float _replanTimer;
		private int _pathSegmentIndex;
		private Rigidbody _rb;
		private Vector3? _patrolGoal;
		private float _stuckTimer;
		private const float StuckThresholdSeconds = 2f;
		private RoadGraph _graph;
			private enum RecoveryMode { None, Reversing, ForwardRecover }
			private RecoveryMode _recoveryMode;
			private float _recoveryTimer;

		private RoadGraph ResolveGraph()
		{
			var all = FindObjectsByType<RoadGraph>(FindObjectsSortMode.None);
			RoadGraph best = null;
			int bestLanes = -1;
			foreach (var g in all)
			{
				int count = g != null ? g.Lanes.Count : 0;
				if (count > bestLanes)
				{
					best = g;
					bestLanes = count;
				}
			}
			if (best != null && debugLogs)
			{
				Debug.Log($"{name}: Bound to RoadGraph with nodes={best.Nodes.Count} lanes={best.Lanes.Count}");
			}
			return best;
		}

		protected override void Start()
		{
			base.Start();
			_rb = GetComponent<Rigidbody>();
			_graph = ResolveGraph();
			_replanTimer = 0f; // force initial plan on first FixedUpdate
		}

		protected override void FixedUpdate()
		{
			if (disabled) return;
			if (_graph == null || _graph.Lanes.Count == 0) { _graph = ResolveGraph(); if (_graph == null) return; }

			_replanTimer -= Time.fixedDeltaTime;
			if (_replanTimer <= 0f)
			{
				_replanTimer = replanInterval;
				PlanPath();
			}

			base.FixedUpdate();
		}

		private void PlanPath()
		{
			if (_graph == null)
			{
				if (debugLogs) Debug.LogWarning($"{name}: RoadGraph.Instance is null. Did you click Generate City?");
				return;
			}
			if (_graph.Lanes.Count == 0)
			{
				if (debugLogs) Debug.LogWarning($"{name}: RoadGraph has 0 lanes. Click Generate City or check CityGenerator.");
				return;
			}

			Transform target = GameManager.Instance != null ? GameManager.Instance.targetCar : null;
			if (target == null)
			{
				if (!patrolWhenNoTarget || _graph.Nodes.Count == 0) return;
				// pick a random node as a patrol destination
				if (_patrolGoal == null || (transform.position - _patrolGoal.Value).sqrMagnitude < 4f)
				{
					int idx = Random.Range(0, _graph.Nodes.Count);
					_patrolGoal = _graph.Nodes[idx].position;
				}
				var patrolPath = _graph.FindPath(transform.position, _patrolGoal.Value);
				if (patrolPath != null && patrolPath.Count >= 2)
				{
					_path.Clear();
					_path.AddRange(patrolPath);
					_pathSegmentIndex = 0;
					if (debugLogs) Debug.Log($"{name}: Planned patrol path with {_path.Count} points.");
				}
				else
				{
					if (debugLogs) Debug.LogWarning($"{name}: Patrol path is null/too short.");
				}
				return;
			}

			var newPath = _graph.FindPath(transform.position, target.position);
			if (newPath != null && newPath.Count >= 2)
			{
				_path.Clear();
				_path.AddRange(newPath);
				_pathSegmentIndex = 0;
				if (debugLogs) Debug.Log($"{name}: Planned chase path with {_path.Count} points.");
			}
			else
			{
				if (debugLogs) Debug.LogWarning($"{name}: Chase path is null/too short.");
			}
		}

		protected override void UpdateVehicle()
		{
			if (_path.Count < 2)
			{
				VehicleController.SetThrottle(0f);
				VehicleController.SetSteering(0f);
				VehicleController.SetBrake(false);
			if (debugLogs) Debug.LogWarning($"{name}: No path. Waiting for planner.");
				return;
			}

			AdvanceSegmentIndex();
			Vector3 lookahead = ComputeLookaheadPoint(lookaheadDistance);

			Vector3 localTarget = transform.InverseTransformPoint(lookahead);
			float angleToTarget = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;
			float steerInput = Mathf.Clamp(angleToTarget / maxSteerAngle, -1f, 1f);

			float speed = _rb != null ? _rb.linearVelocity.magnitude : 0f;
			float throttle = Mathf.Clamp01((targetSpeed - speed) / Mathf.Max(1f, targetSpeed));

			VehicleController.SetThrottle(throttle);
			VehicleController.SetSteering(steerInput);
			VehicleController.SetBrake(false);

			// Stuck detector: throttle high but no speed gain over time
			if (throttle > 0.5f && speed < 0.5f)
			{
				_stuckTimer += Time.fixedDeltaTime;
				if (_stuckTimer > StuckThresholdSeconds)
				{
					if (reverseRecoveryEnabled && _recoveryMode == RecoveryMode.None)
					{
						_recoveryMode = RecoveryMode.Reversing;
						_recoveryTimer = reverseDuration;
						if (debugLogs) Debug.LogWarning($"{name}: Stuck detected. Entering reverse recovery for {reverseDuration:0.00}s");
					}
					else
					{
						if (debugLogs) Debug.LogWarning($"{name}: Stuck (no movement). Check WheelColliders grounded and road colliders exist.");
					}
					_stuckTimer = 0f;
				}
			}
			else
			{
				_stuckTimer = 0f;
			}

			// Recovery override
			if (reverseRecoveryEnabled && _recoveryMode != RecoveryMode.None)
			{
				_recoveryTimer -= Time.fixedDeltaTime;
				if (_recoveryMode == RecoveryMode.Reversing)
				{
					// Reverse with opposite steer to pivot away from obstacle
					float recoverSteer = Mathf.Clamp(-steerInput, -1f, 1f);
					VehicleController.SetThrottle(reverseThrottle);
					VehicleController.SetSteering(recoverSteer);
					VehicleController.SetBrake(false);
					if (_recoveryTimer <= 0f)
					{
						_recoveryMode = RecoveryMode.ForwardRecover;
						_recoveryTimer = forwardRecoverDuration;
						if (debugLogs) Debug.Log($"{name}: Reverse complete. Short forward recover {forwardRecoverDuration:0.00}s.");
					}
					return;
				}
				if (_recoveryMode == RecoveryMode.ForwardRecover)
				{
					VehicleController.SetThrottle(forwardRecoverThrottle);
					VehicleController.SetSteering(steerInput);
					VehicleController.SetBrake(false);
					if (_recoveryTimer <= 0f)
					{
						_recoveryMode = RecoveryMode.None;
						if (debugLogs) Debug.Log($"{name}: Recovery finished.");
					}
					return;
				}
			}
		}

		private void AdvanceSegmentIndex()
		{
			while (_pathSegmentIndex < _path.Count - 1)
			{
				Vector3 a = _path[_pathSegmentIndex];
				Vector3 b = _path[_pathSegmentIndex + 1];
				Vector3 ab = b - a;
				Vector3 ap = transform.position - a;
				float t = Vector3.Dot(ap, ab.normalized);
				if (t > ab.magnitude) _pathSegmentIndex++;
				else break;
			}
		}

		private Vector3 ComputeLookaheadPoint(float distance)
		{
			float remaining = distance;
			int i = _pathSegmentIndex;
			Vector3 pos = transform.position;

			// Start from current projection on the active segment
			if (i < _path.Count - 1)
			{
				Vector3 a = _path[i];
				Vector3 b = _path[i + 1];
				Vector3 ab = b - a;
				float len = ab.magnitude;
				if (len > 0.0001f)
				{
					Vector3 dir = ab / len;
					float t = Mathf.Clamp(Vector3.Dot(pos - a, dir), 0f, len);
					pos = a + dir * t;
				}
				else
				{
					pos = a;
				}
			}

			while (remaining > 0.001f && i < _path.Count - 1)
			{
				Vector3 a = pos;
				Vector3 b = _path[i + 1];
				float seg = Vector3.Distance(a, b);
				if (remaining <= seg)
				{
					Vector3 dir = (b - a).normalized;
					return a + dir * remaining;
				}
				else
				{
					remaining -= seg;
					i++;
					pos = _path[i];
				}
			}

			return _path[_path.Count - 1];
		}

		private void OnDrawGizmos()
		{
			if (_path.Count >= 2)
			{
				Gizmos.color = Color.green;
				for (int i = 1; i < _path.Count; i++)
				{
					Gizmos.DrawLine(_path[i - 1] + Vector3.up * 0.05f, _path[i] + Vector3.up * 0.05f);
				}
				Gizmos.color = Color.magenta;
				var la = ComputeLookaheadPoint(lookaheadDistance);
				Gizmos.DrawSphere(la + Vector3.up * 0.1f, 0.3f);
			}
		}
	}
}


