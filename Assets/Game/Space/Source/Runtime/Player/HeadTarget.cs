using Game.Intro.Camera;
using Nonatomic.ServiceLocator;
using UnityEngine;

namespace Game.Intro.Player
{
	public class HeadTarget : MonoBehaviour
	{
		[SerializeField] private ServiceLocator _serviceLocator;
		[SerializeField] private Vector3 _offset;

		private IMainCameraService _playerTrackingCamera;

		protected virtual async void Awake()
		{
			_playerTrackingCamera = await _serviceLocator.GetAsync<IMainCameraService>()
				.WithCancellation(destroyCancellationToken)
				.WithErrorHandling();
		}

		protected virtual void Update()
		{
			if (_playerTrackingCamera == null)
			{
				return;
			}

			var pos = _playerTrackingCamera.Transform.position += _offset;
			transform.position = pos;
		}
	}
}