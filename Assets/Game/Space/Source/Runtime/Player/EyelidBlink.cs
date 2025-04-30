using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Game.Intro.Player
{
	public class EyelidBlink : MonoBehaviour
	{
		[SerializeField] private MeshRenderer[] _meshRenderers;
		[SerializeField] private float _minBlinkInterval = 3f;
		[SerializeField] private float _maxBlinkInterval = 5f;
		[SerializeField] private float _blinkDuration = 0.12f;

		private void Awake()
		{
			_ = BlinkLoopAsync(destroyCancellationToken);
		}

		private async Task BlinkLoopAsync(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				var waitTime = Random.Range(_minBlinkInterval, _maxBlinkInterval);
				await Task.Delay((int)(waitTime * 1000), cancellationToken);

				foreach (var meshRenderer in _meshRenderers)
				{
					meshRenderer.enabled = true;
				}

				await Task.Delay((int)(_blinkDuration * 1000), cancellationToken);

				foreach (var meshRenderer in _meshRenderers)
				{
					meshRenderer.enabled = false;
				}
			}
		}
	}
}