using Nonatomic.ServiceLocator;
using UnityEngine;

namespace Game.Intro.Camera
{
	public interface IPlayerTrackingCameraService
	{
		Transform Transform { get; }
	}

	public class PlayerTrackingCameraService : MonoService<IPlayerTrackingCameraService>, IPlayerTrackingCameraService
	{
		protected override void Awake()
		{
			base.Awake();
			ServiceReady();
		}

		public Transform Transform => transform;
	}
}