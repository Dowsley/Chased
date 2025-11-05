using UnityEngine;

namespace Driving.Player
{
	[RequireComponent(typeof(Camera))]
	public class ChaseCamera : MonoBehaviour
	{
		[SerializeField] private Transform target;
		[SerializeField] private Vector3 followOffset = new(0f, 3f, -8f);
		[SerializeField] private float positionDamping = 0.15f;
		[SerializeField] private float rotationDamping = 10f;

		[SerializeField] private float minFOV = 65f;
		[SerializeField] private float maxFOV = 90f;
		[SerializeField] private float maxFovAtSpeed = 45f;
		[SerializeField] private float fovLerpSpeed = 8f;

		[SerializeField] private float extraDistancePerSpeed = 0.06f;
		[SerializeField] private float maxExtraDistance = 5f;

		[SerializeField] private float lookAheadPerSpeed = 0.35f;
		[SerializeField] private float maxLookAhead = 6f;
		[SerializeField] private float lateralLookAmount = 0.4f;

		[SerializeField] private float rollAngle = 6f;
		[SerializeField] private float rollLerpSpeed = 3.5f;

		// How much the camera orbits around the car on hard cornering (to reveal wheels)
		[SerializeField] private float maxCornerYaw = 18f;
		[SerializeField] private float cornerYawLerp = 3.5f;

		[SerializeField] private bool alignToVelocity = true;
		[SerializeField] private float velocityAlignWeight = 0.6f;

		[SerializeField] private bool collisionAvoidance = true;
		[SerializeField] private LayerMask collisionMask = ~0;
		[SerializeField] private float collisionRadius = 0.2f;
		[SerializeField] private float collisionBuffer = 0.2f;

		private Rigidbody _targetRb;
		private Camera _camera;
		private Vector3 _positionVelocity;
		private float _currentRoll;
		private float _cornerYaw;

		private void Awake()
		{
			_camera = GetComponent<Camera>();
		}

		private void Start()
		{
			_targetRb = target.GetComponent<Rigidbody>();
			if (_camera.orthographic)
			{
				_camera.orthographic = false;
			}
		}

		private void LateUpdate()
		{

			float speed = _targetRb.linearVelocity.magnitude;
			Vector3 localVelocity = target.InverseTransformDirection(_targetRb.linearVelocity);

			float fovT = Mathf.Clamp01(maxFovAtSpeed > 0f ? speed / maxFovAtSpeed : 0f);
			float targetFov = Mathf.Lerp(minFOV, maxFOV, fovT);
			_camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, targetFov, Time.deltaTime * fovLerpSpeed);

			float extraDistance = Mathf.Clamp(speed * extraDistancePerSpeed, 0f, maxExtraDistance);
			Vector3 dynamicOffset = followOffset + new Vector3(0f, 0f, -extraDistance);

			// Cornering orbit: swing camera outward on turns to expose wheels
			float targetCornerYaw = -Mathf.Clamp(localVelocity.x, -1f, 1f) * maxCornerYaw;
			_cornerYaw = Mathf.Lerp(_cornerYaw, targetCornerYaw, Time.deltaTime * cornerYawLerp);
			Vector3 corneredOffset = Quaternion.AngleAxis(_cornerYaw, Vector3.up) * dynamicOffset;

			Vector3 desiredPos = target.TransformPoint(corneredOffset);

			Vector3 lookAhead = target.forward * Mathf.Clamp(speed * lookAheadPerSpeed, 0f, maxLookAhead);
			lookAhead += target.right * (localVelocity.x * lateralLookAmount);
			Vector3 focusPoint = target.position + lookAhead;

			if (collisionAvoidance)
			{
				Vector3 origin = target.position + Vector3.up * 0.5f;
				Vector3 toCam = desiredPos - origin;
				float dist = toCam.magnitude;
				if (dist > 0.001f)
				{
					Vector3 dir = toCam / dist;
					if (Physics.SphereCast(origin, collisionRadius, dir, out RaycastHit hit, dist, collisionMask, QueryTriggerInteraction.Ignore))
					{
						desiredPos = hit.point + hit.normal * (collisionRadius + collisionBuffer);
					}
				}
			}

			transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref _positionVelocity, positionDamping);

			Vector3 forwardDir = (focusPoint - transform.position);
			if (forwardDir.sqrMagnitude < 0.0001f) forwardDir = target.forward;

			if (alignToVelocity && _targetRb.linearVelocity.sqrMagnitude > 0.01f)
			{
				Vector3 velDir = _targetRb.linearVelocity.normalized;
				forwardDir = Vector3.Slerp(forwardDir.normalized, velDir, velocityAlignWeight);
			}

			Quaternion lookRot = Quaternion.LookRotation(forwardDir.normalized, Vector3.up);
			float targetRoll = -Mathf.Clamp(localVelocity.x, -1f, 1f) * rollAngle;
			_currentRoll = Mathf.Lerp(_currentRoll, targetRoll, Time.deltaTime * rollLerpSpeed);
			lookRot *= Quaternion.AngleAxis(_currentRoll, Vector3.forward);

			transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 1f - Mathf.Exp(-rotationDamping * Time.deltaTime));
		}
	}
}


