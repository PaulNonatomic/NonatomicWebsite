using Game.Intro.Camera;
using Nonatomic.ServiceLocator;
using UnityEngine;

namespace Game.Intro.Player
{
	public class LookAtCamera : MonoBehaviour
	{
		[SerializeField] private ServiceLocator _serviceLocator;
		[SerializeField] private float _slerpSpeed = 5f;
		private bool _initialized;

		private IMainCameraService _mainCamera;

		protected virtual async void Awake()
		{
			_mainCamera = await _serviceLocator.GetAsync<IMainCameraService>()
				.WithCancellation(destroyCancellationToken)
				.WithErrorHandling();

			_initialized = true;
			Debug.Log("LookAtCamera initialized");
		}

		protected virtual void Update()
		{
			if (!_initialized)
			{
				return;
			}

			if (_mainCamera == null || !_mainCamera.Transform)
			{
				return;
			}

			var targetDirection = _mainCamera.Transform.position - transform.position;
			var targetRotation = Quaternion.LookRotation(targetDirection);
			var newRotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * _slerpSpeed);
			transform.rotation = newRotation;
		}
	}
}