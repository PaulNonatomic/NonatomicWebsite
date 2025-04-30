using Nonatomic.ServiceLocator;
using UnityEngine;

namespace Game.Intro.Player
{
	/// <summary>
	///     Rotates a transform around another transform (pivot point) with smooth transitions
	/// </summary>
	public class OrbitController : MonoBehaviour
	{
		[SerializeField] private ServiceLocator _serviceLocator;
		[SerializeField] private float _rotationSpeed = 20f;
		[SerializeField] private float _radius = 5f;
		[SerializeField] private Vector3 _rotationAxis = Vector3.up;
		[SerializeField] private float _heightOffset;
		[SerializeField] private bool _autoRotate = true;
		[SerializeField] private bool _maintainOrientation;
		[SerializeField] private float _positionSmoothTime = 0.1f;
		[SerializeField] private float _rotationSmoothTime = 0.1f;

		private float _currentAngle;
		private Quaternion _currentRotation;
		private Vector3 _currentVelocity;
		private bool _initialized;
		private Quaternion _initialRotation;
		private Transform _pivotPoint;
		private IPlayerSpawnerService _playerSpawner;
		private Vector3 _targetPosition;
		private Quaternion _targetRotation;

		protected virtual async void Awake()
		{
			_playerSpawner = await _serviceLocator.GetAsync<IPlayerSpawnerService>()
				.WithCancellation(destroyCancellationToken)
				.WithErrorHandling();

			_pivotPoint = _playerSpawner.Transform;
			_currentRotation = transform.rotation;

			Initialize();
		}

		private void Update()
		{
			if (_autoRotate)
			{
				// Update angle based on time and speed
				_currentAngle += _rotationSpeed * Time.deltaTime;

				// Keep angle between 0-360 degrees
				if (_currentAngle >= 360f)
				{
					_currentAngle -= 360f;
				}
			}

			// Calculate the desired position and rotation
			CalculateTargetPositionAndRotation();

			// Smoothly move to the target position
			transform.position = Vector3.SmoothDamp(transform.position, _targetPosition, ref _currentVelocity,
				_positionSmoothTime);

			// Smoothly rotate to the target rotation
			if (!_maintainOrientation)
			{
				_currentRotation = Quaternion.Slerp(_currentRotation, _targetRotation,
					1f - Mathf.Exp(-_rotationSmoothTime * 10f * Time.deltaTime));
				transform.rotation = _currentRotation;
			}
			else
			{
				transform.rotation = _initialRotation;
			}
		}

		/// <summary>
		///     Draws gizmos to visualize the orbit in the editor
		/// </summary>
		private void OnDrawGizmosSelected()
		{
			if (!Application.isPlaying)
			{
				var pivotPos = _pivotPoint != null ? _pivotPoint.position : Vector3.zero;
				var heightVector = _rotationAxis.normalized * _heightOffset;

				// Draw orbit path
				Gizmos.color = Color.cyan;
				DrawOrbitPath(pivotPos, _radius, _rotationAxis, heightVector);

				// Draw rotation axis
				Gizmos.color = Color.green;
				Gizmos.DrawLine(pivotPos, pivotPos + _rotationAxis.normalized * 2f);
			}
		}

		private void Initialize()
		{
			if (!_initialized)
			{
				return;
			}

			if (_pivotPoint == null)
			{
				Debug.LogWarning("No pivot point assigned to OrbitController. Using world origin instead.");
			}

			// Store initial rotation if needed
			_initialRotation = transform.rotation;
			_currentRotation = _initialRotation;

			// Set initial position
			CalculateTargetPositionAndRotation();
			transform.position = _targetPosition;

			_initialized = true;
		}

		/// <summary>
		///     Sets the rotation axis
		/// </summary>
		/// <param name="axis">The new rotation axis</param>
		public void SetRotationAxis(Vector3 axis)
		{
			_rotationAxis = axis.normalized;
		}

		/// <summary>
		///     Sets the orbit radius
		/// </summary>
		/// <param name="newRadius">The new radius value</param>
		public void SetRadius(float newRadius)
		{
			_radius = newRadius;
		}

		/// <summary>
		///     Sets the smoothing time for position transitions
		/// </summary>
		/// <param name="smoothTime">The new smooth time value in seconds</param>
		public void SetPositionSmoothTime(float smoothTime)
		{
			_positionSmoothTime = Mathf.Max(0.001f, smoothTime);
		}

		/// <summary>
		///     Sets the smoothing time for rotation transitions
		/// </summary>
		/// <param name="smoothTime">The new smooth time value in seconds</param>
		public void SetRotationSmoothTime(float smoothTime)
		{
			_rotationSmoothTime = Mathf.Max(0.001f, smoothTime);
		}

		/// <summary>
		///     Sets the rotation speed in degrees per second
		/// </summary>
		/// <param name="newSpeed">The new rotation speed</param>
		public void SetRotationSpeed(float newSpeed)
		{
			_rotationSpeed = newSpeed;
		}

		/// <summary>
		///     Sets the current angle in degrees
		/// </summary>
		/// <param name="angle">The new angle</param>
		public void SetAngle(float angle)
		{
			_currentAngle = angle % 360f;
		}

		/// <summary>
		///     Toggles auto-rotation on/off
		/// </summary>
		public void ToggleAutoRotate()
		{
			_autoRotate = !_autoRotate;
		}

		/// <summary>
		///     Calculates the target position and rotation based on current angle and radius
		/// </summary>
		private void CalculateTargetPositionAndRotation()
		{
			// Get pivot position (or use origin if no pivot set)
			var pivotPosition = _pivotPoint != null ? _pivotPoint.position : Vector3.zero;

			// Calculate direction from pivot
			var rotation = Quaternion.AngleAxis(_currentAngle, _rotationAxis);
			var direction = rotation * Vector3.forward;

			// Calculate new position
			var offset = direction * _radius;
			var heightVector = _rotationAxis.normalized * _heightOffset;
			_targetPosition = pivotPosition + offset + heightVector;

			// Calculate desired look direction
			if (!_maintainOrientation)
			{
				var lookDirection = pivotPosition - _targetPosition + heightVector;
				if (lookDirection != Vector3.zero)
				{
					_targetRotation = Quaternion.LookRotation(lookDirection);
				}
			}
			else
			{
				_targetRotation = _initialRotation;
			}
		}

		/// <summary>
		///     Helper method to draw the orbit path as a gizmo
		/// </summary>
		/// <param name="center">The center point of the orbit</param>
		/// <param name="radius">The radius of the orbit</param>
		/// <param name="axis">The axis of rotation</param>
		/// <param name="heightOffset">The height offset from the center</param>
		private void DrawOrbitPath(Vector3 center, float radius, Vector3 axis, Vector3 heightOffset)
		{
			// Create a plane from the axis
			var planeNormal = axis.normalized;
			var tangent = Vector3.Cross(planeNormal, Vector3.up);
			if (tangent.magnitude < 0.001f)
			{
				tangent = Vector3.Cross(planeNormal, Vector3.right);
			}

			tangent.Normalize();
			var bitangent = Vector3.Cross(planeNormal, tangent);

			// Draw the orbit path
			const int segments = 32;
			var previousPoint = center + tangent * radius + heightOffset;

			for (var i = 1; i <= segments; i++)
			{
				var angle = i * 2 * Mathf.PI / segments;
				var nextPoint = center +
								(tangent * Mathf.Cos(angle) +
								 bitangent * Mathf.Sin(angle)) * radius +
								heightOffset;

				Gizmos.DrawLine(previousPoint, nextPoint);
				previousPoint = nextPoint;
			}
		}
	}
}