using Nonatomic.ServiceLocator;
using UnityEngine;

namespace Game.Intro.Camera
{
	public interface IMainCameraService
	{
		Transform Transform { get; }
	}

	public class MainCameraService : MonoService<IMainCameraService>, IMainCameraService
	{
		protected override void Awake()
		{
			base.Awake();
			ServiceReady();
		}

		public Transform Transform => transform;
	}
}