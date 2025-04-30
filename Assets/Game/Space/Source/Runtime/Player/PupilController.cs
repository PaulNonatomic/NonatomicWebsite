using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Intro.Player
{
	/// <summary>
	///     Controls the movement of character pupils, making them drift naturally within a specified range.
	///     Both pupils move in tandem with natural, organic motion patterns.
	/// </summary>
	public class PupilController : MonoBehaviour
	{
		[FormerlySerializedAs("leftPupil")]
		[Header("Pupil References")]
		[Tooltip("Transform for the left pupil")]
		[SerializeField]
		private Transform _leftPupil;

		[FormerlySerializedAs("rightPupil")] [Tooltip("Transform for the right pupil")] [SerializeField]
		private Transform _rightPupil;

		[FormerlySerializedAs("xRange")]
		[Header("Movement Settings")]
		[Tooltip("Maximum distance pupils can move from their original position on X axis")]
		[SerializeField]
		private float _xRange = 0.05f;

		[FormerlySerializedAs("yRange")]
		[Tooltip("Maximum distance pupils can move from their original position on Y axis")]
		[SerializeField]
		private float _yRange = 0.05f;

		[FormerlySerializedAs("moveSpeed")] [Tooltip("Base speed of pupil movement")] [Range(0.1f, 5f)] [SerializeField]
		private float _moveSpeed = 1f;

		[FormerlySerializedAs("averageFocusDuration")]
		[Tooltip("How long pupils typically stay focused in one direction")]
		[Range(1f, 10f)]
		[SerializeField]
		private float _averageFocusDuration = 3f;

		[FormerlySerializedAs("focusVariation")]
		[Tooltip("Random variation in focus duration")]
		[Range(0f, 5f)]
		[SerializeField]
		private float _focusVariation = 1.5f;

		[FormerlySerializedAs("quickMoveProbability")]
		[Tooltip("Chance of rapid eye movement (like a blink or quick glance)")]
		[Range(0f, 0.1f)]
		[SerializeField]
		private float _quickMoveProbability = 0.02f;

		[FormerlySerializedAs("quickMoveSpeed")]
		[Tooltip("How quickly the quick movements occur")]
		[Range(1f, 10f)]
		[SerializeField]
		private float _quickMoveSpeed = 5f;

		private float _currentFocusDuration;

		// Current and target positions
		private Vector3 _currentOffset = Vector3.zero;
		private float _currentSpeed;

		// Timers and state
		private float _focusTimer;
		private bool _isInQuickMove;

		// Original positions of the pupils
		private Vector3 _leftPupilOrigin;

		// Perlin noise offsets for organic movement
		private float _noiseOffsetX;
		private float _noiseOffsetY;
		private Vector3 _rightPupilOrigin;
		private Vector3 _targetOffset;

		private void Start()
		{
			// Store the original positions
			if (_leftPupil != null)
			{
				_leftPupilOrigin = _leftPupil.localPosition;
			}
			else
			{
				Debug.LogError("Left pupil reference is missing on PupilController!");
			}

			if (_rightPupil != null)
			{
				_rightPupilOrigin = _rightPupil.localPosition;
			}
			else
			{
				Debug.LogError("Right pupil reference is missing on PupilController!");
			}

			// Initialize random values for organic movement
			_noiseOffsetX = Random.Range(0f, 1000f);
			_noiseOffsetY = Random.Range(0f, 1000f);

			// Set initial targets and durations
			SetNewTargetPosition();
			_currentSpeed = _moveSpeed;
		}

		private void Update()
		{
			if (_leftPupil == null || _rightPupil == null)
			{
				return;
			}

			// Update timers
			_focusTimer += Time.deltaTime;

			// Check if it's time for a new focus point
			if (_focusTimer >= _currentFocusDuration)
			{
				// Small chance of a quick movement
				_isInQuickMove = Random.value < _quickMoveProbability;

				SetNewTargetPosition();
				_focusTimer = 0f;

				// Set movement speed based on normal or quick movement
				_currentSpeed = _isInQuickMove ? _quickMoveSpeed : _moveSpeed;
			}

			// Move pupils toward target with smooth interpolation
			MovePupilsNaturally();
		}

		// Editor-only: visualize the movement range
		private void OnDrawGizmosSelected()
		{
			if (_leftPupil != null)
			{
				Gizmos.color = new(0, 1, 0, 0.3f);
				var leftWorldOrigin = _leftPupil.parent != null
					? _leftPupil.parent.TransformPoint(_leftPupilOrigin)
					: _leftPupilOrigin;
				Gizmos.DrawWireCube(leftWorldOrigin, new(_xRange * 2, _yRange * 2, 0.001f));
			}

			if (_rightPupil != null)
			{
				Gizmos.color = new(0, 1, 0, 0.3f);
				var rightWorldOrigin = _rightPupil.parent != null
					? _rightPupil.parent.TransformPoint(_rightPupilOrigin)
					: _rightPupilOrigin;
				Gizmos.DrawWireCube(rightWorldOrigin, new(_xRange * 2, _yRange * 2, 0.001f));
			}
		}

		private void MovePupilsNaturally()
		{
			// Calculate smooth interpolation factor
			var t = Time.deltaTime * _currentSpeed;

			// Add subtle micro-movements using Perlin noise for more natural effect
			var microMovementX =
				Mathf.PerlinNoise(_noiseOffsetX + Time.time * 0.5f, 0) * 0.02f * _xRange - 0.01f * _xRange;
			var microMovementY =
				Mathf.PerlinNoise(0, _noiseOffsetY + Time.time * 0.5f) * 0.02f * _yRange - 0.01f * _yRange;
			var microMovement = new Vector3(microMovementX, microMovementY, 0);

			// Smoothly interpolate position
			_currentOffset = Vector3.Lerp(_currentOffset, _targetOffset, t);

			// Apply position with micro-movements to both pupils
			var finalOffset = _currentOffset + microMovement;

			// Apply the offset to both pupils
			_leftPupil.localPosition = _leftPupilOrigin + finalOffset;
			_rightPupil.localPosition = _rightPupilOrigin + finalOffset;

			// Add occasional micro-jitters/blinks that happen to both eyes
			if (Random.value < 0.003f)
			{
				// Quick minor adjustment both pupils will make
				StartCoroutine(MicroBlink());
			}
		}

		private IEnumerator MicroBlink()
		{
			// Save current positions
			var leftOriginal = _leftPupil.localPosition;
			var rightOriginal = _rightPupil.localPosition;

			// Slightly adjust position (like a tiny blink/adjustment)
			var blinkAmount = Random.Range(0.2f, 0.4f);
			var blinkOffset = new Vector3(0, -blinkAmount * _yRange, 0);

			// Apply quick adjustment
			_leftPupil.localPosition = leftOriginal + blinkOffset;
			_rightPupil.localPosition = rightOriginal + blinkOffset;

			// Hold for a very brief moment
			yield return new WaitForSeconds(Random.Range(0.05f, 0.1f));

			// Return to original positions
			_leftPupil.localPosition = leftOriginal;
			_rightPupil.localPosition = rightOriginal;
		}

		private void SetNewTargetPosition()
		{
			// Calculate a new natural focus duration with variation
			_currentFocusDuration = _averageFocusDuration + Random.Range(-_focusVariation, _focusVariation);

			// Natural eye movements tend to favor certain positions (center, slightly up/down)
			// and have correlated x/y movement rather than purely random positioning

			// Center focus bias - eyes tend to return to center
			var centerBias = Random.value < 0.3f ? 0.3f : 0;

			// Reading pattern bias - horizontal movement with small vertical adjustment
			var readingPatternBias = Random.value < 0.2f ? 0.7f : 0;

			// Generate new target with these natural biases
			var xOffset = Random.Range(-_xRange, _xRange);
			var yOffset = Random.Range(-_yRange, _yRange);

			// Apply center bias
			xOffset = Mathf.Lerp(xOffset, 0, centerBias);
			yOffset = Mathf.Lerp(yOffset, 0, centerBias);

			// Apply reading pattern
			if (readingPatternBias > 0)
			{
				// More horizontal than vertical movement
				xOffset = Random.Range(-_xRange, _xRange);
				yOffset = Random.Range(-_yRange * 0.3f, _yRange * 0.3f);
			}

			_targetOffset = new(xOffset, yOffset, 0);
		}

		// Useful for animation events or external control
		public void ResetPupilPositions()
		{
			if (_leftPupil != null)
			{
				_leftPupil.localPosition = _leftPupilOrigin;
			}

			if (_rightPupil != null)
			{
				_rightPupil.localPosition = _rightPupilOrigin;
			}

			_currentOffset = Vector3.zero;
			_targetOffset = Vector3.zero;
		}

		// Allows other systems to influence where the character is looking
		public void LookInDirection(Vector2 direction, float intensity = 1.0f)
		{
			// Normalize and scale by range and intensity
			direction = Vector2.ClampMagnitude(direction, 1f);
			var lookOffset = new Vector3(
				direction.x * _xRange * intensity,
				direction.y * _yRange * intensity,
				0
			);

			_targetOffset = lookOffset;
			_currentSpeed = _moveSpeed * 2; // Look at directed targets more quickly
			_focusTimer = 0f;
			_currentFocusDuration = _averageFocusDuration;
		}
	}
}