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

		private readonly List<Vector3> _path = new List<Vector3>();
		private float _replanTimer;
		private int _pathSegmentIndex;
		private Rigidbody _rb;

		protected override void Start()
		{
			base.Start();
			_rb = GetComponent<Rigidbody>();
		}

		protected override void FixedUpdate()
		{
			if (disabled) return;
			if (RoadGraph.Instance == null) return;

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
			Transform target = GameManager.Instance != null ? GameManager.Instance.targetCar : null;
			if (target == null) return;

			var newPath = RoadGraph.Instance.FindPath(transform.position, target.position);
			if (newPath != null && newPath.Count >= 2)
			{
				_path.Clear();
				_path.AddRange(newPath);
				_pathSegmentIndex = 0;
			}
		}

		protected override void UpdateVehicle()
		{
			if (_path.Count < 2)
			{
				VehicleController.SetThrottle(0f);
				VehicleController.SetSteering(0f);
				VehicleController.SetBrake(false);
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
	}
}


