using Game.Common;
using Nonatomic.ServiceLocator;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using CameraTarget = Game.Intro.Camera.CameraTarget;

namespace Game.Intro.Player
{
	public interface IPlayerSpawnerService
	{
		public Transform Transform { get; }
	}

	public class PlayerSpawnerService : MonoService<IPlayerSpawnerService>, IPlayerSpawnerService
	{
		[SerializeField] private AssetReferenceGameObject _playerPrefabRef;
		[SerializeField] private AssetReferenceGameObject _trackingCameraPrefabRef;
		[SerializeField] private Transform _parent;

		private CameraTarget _cameraTarget;
		private GameObject _player;
		private AsyncOperationHandle<GameObject> _playerPrefabHandle;
		private CinemachineCamera _trackingCamera;
		private AsyncOperationHandle<GameObject> _trackingCameraPrefabHandle;

		protected override void Awake()
		{
			base.Awake();
			ServiceReady();
		}

		protected virtual async void Start()
		{
			_playerPrefabHandle = await AddressableLoader
				.LoadAssetAsync<GameObject>(_playerPrefabRef, this)
				.WithErrorHandling();

			_trackingCameraPrefabHandle = await AddressableLoader
				.LoadAssetAsync<GameObject>(_trackingCameraPrefabRef, this)
				.WithErrorHandling();

			SpawnPlayer(_playerPrefabHandle.Result);
			SpawnTrackingCamera(_trackingCameraPrefabHandle.Result);

			_trackingCamera.Follow = _cameraTarget.transform;
		}

		protected virtual void OnDestroy()
		{
			Addressables.Release(_playerPrefabHandle);
			Addressables.Release(_trackingCameraPrefabHandle);
		}

		public Transform Transform => transform;

		private void SpawnTrackingCamera(GameObject prefab)
		{
			if (_trackingCamera != null)
			{
				return;
			}

			var instance = Instantiate(prefab, _parent);
			_trackingCamera = instance.GetComponentInChildren<CinemachineCamera>();
		}

		private void SpawnPlayer(GameObject prefab)
		{
			if (_player != null)
			{
				return;
			}

			_player = Instantiate(prefab, _parent);
			_cameraTarget = _player.GetComponentInChildren<CameraTarget>();
		}
	}
}